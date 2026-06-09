# Jellyfin.Plugin.Festivals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Jellyfin 10.11 plugin that presents music festivals as a native Shows-style library (Festival = Series, Year = Season, Recording = Episode), enriched from a plugin-owned JSON database managed through a dashboard config page.

**Architecture:** Map the festival hierarchy onto Jellyfin's native Series/Season/Episode model so HERO/backdrop, drill-down and playback work natively. A `FestivalStore` service owns `festivals.json` + an image folder in the plugin data dir. `ICustomMetadataProvider<T>` implementations enrich items by folder/file-name matching; `IDynamicImageProvider` supplies HERO/poster/artist images. A REST controller + HTML config page provide CRUD.

**Tech Stack:** .NET 9 (net9.0), Jellyfin.Controller 10.11.8, ASP.NET controllers, vanilla JS config page. Unit tests via xUnit for pure logic.

---

## File Structure

```
jellyfin-plugin-festivals/
  Jellyfin.Plugin.Festivals.sln
  Jellyfin.Plugin.Festivals/
    Jellyfin.Plugin.Festivals.csproj
    Plugin.cs                          # BasePlugin<PluginConfiguration>, IHasWebPages
    PluginServiceRegistrator.cs        # DI registration of FestivalStore
    Configuration/
      PluginConfiguration.cs           # minimal (data lives in JSON)
      config.html                      # dashboard management UI
      config.js                        # CRUD + uploads against the API
    Model/
      Festival.cs                      # Festival, FestivalYear, Recording records
    Data/
      FestivalStore.cs                 # load/save JSON + image files + lookups
      NameMatching.cs                  # normalize() + match helpers (pure, tested)
    Providers/
      FestivalSeriesProvider.cs        # ICustomMetadataProvider<Series>
      FestivalSeasonProvider.cs        # ICustomMetadataProvider<Season>
      FestivalEpisodeProvider.cs       # ICustomMetadataProvider<Episode>
      FestivalImageProvider.cs         # IDynamicImageProvider (Series/Season/Episode)
    Api/
      FestivalApiController.cs         # REST CRUD + image upload/serve
  Jellyfin.Plugin.Festivals.Tests/
    Jellyfin.Plugin.Festivals.Tests.csproj
    NameMatchingTests.cs
    FestivalStoreTests.cs
  README.md                            # English
  build.yaml / meta.json               # plugin manifest
```

**Testing reality:** Host-integrated types (providers, controller, config page) cannot be meaningfully unit-tested without the Jellyfin runtime. For those, the "test" is `dotnet build` succeeding and a manual load in Jellyfin. Pure logic (`NameMatching`, `FestivalStore` JSON round-trip + lookups) is covered by xUnit TDD.

---

## Task 1: Scaffold from official template

**Files:** whole project tree under `D:\Dev\jellyfin-plugin-festivals`

- [ ] **Step 1: Install the official template**

```bash
dotnet new install jellyfin-plugin-template
```
Expected: template `jellyfin-plugin-template` listed as installed.

- [ ] **Step 2: Generate the plugin**

```bash
cd /d/Dev/jellyfin-plugin-festivals
dotnet new jellyfin-plugin -n Jellyfin.Plugin.Festivals
```
Expected: project files created. (If the template nests a folder, flatten so the csproj sits at `Jellyfin.Plugin.Festivals/`.)

- [ ] **Step 3: Pin TFM + Controller version**

In `Jellyfin.Plugin.Festivals.csproj` set `<TargetFramework>net9.0</TargetFramework>` and `Jellyfin.Controller` PackageReference `Version="10.11.8"`.

- [ ] **Step 4: Verify it builds**

```bash
dotnet build Jellyfin.Plugin.Festivals/Jellyfin.Plugin.Festivals.csproj -c Release
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit** (`feat: scaffold plugin from official template`)

---

## Task 2: Domain model

**Files:** Create `Model/Festival.cs`

- [ ] **Step 1: Define records**

```csharp
namespace Jellyfin.Plugin.Festivals.Model;

public class Recording
{
    public string FileMatch { get; set; } = "";   // matched against episode file name (no ext)
    public string Artist { get; set; } = "";
    public string? Stage { get; set; }
    public string? Date { get; set; }
    public List<string> Genres { get; set; } = new();
    public string? Bio { get; set; }
    public string? ArtistImage { get; set; }       // relative file name in image dir
    public string? Setlist { get; set; }
}

public class FestivalYear
{
    public int Year { get; set; }
    public string? Description { get; set; }
    public string? Poster { get; set; }
    public List<Recording> Recordings { get; set; } = new();
}

public class Festival
{
    public string Id { get; set; } = "";          // stable guid string
    public string Name { get; set; } = "";         // matched against series folder name
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<string> Genres { get; set; } = new();
    public string? HeroImage { get; set; }
    public string? Poster { get; set; }
    public List<FestivalYear> Years { get; set; } = new();
}

public class FestivalDatabase
{
    public List<Festival> Festivals { get; set; } = new();
}
```

- [ ] **Step 2: Build** → `dotnet build`. Expected: 0 errors.
- [ ] **Step 3: Commit** (`feat: add festival domain model`)

---

## Task 3: Name matching (TDD, pure logic)

**Files:** Create `Data/NameMatching.cs`, Test `Tests/NameMatchingTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using Jellyfin.Plugin.Festivals.Data;
using Xunit;

public class NameMatchingTests
{
    [Theory]
    [InlineData("Rock am Ring", "rock am ring")]
    [InlineData("  Rock   am Ring ", "rock am ring")]
    [InlineData("Bring Me The Horizon", "bring me the horizon")]
    [InlineData("Sabaton (Live)", "sabaton live")]
    public void Normalize_collapses_case_space_and_punctuation(string input, string expected)
        => Assert.Equal(expected, NameMatching.Normalize(input));

    [Fact]
    public void Matches_ignores_case_and_surrounding_space()
        => Assert.True(NameMatching.Matches("Sabaton", " sabaton "));
}
```

- [ ] **Step 2: Run, expect FAIL**

```bash
dotnet test Jellyfin.Plugin.Festivals.Tests --filter NameMatchingTests
```
Expected: FAIL (NameMatching not defined).

- [ ] **Step 3: Implement**

```csharp
using System.Text;

namespace Jellyfin.Plugin.Festivals.Data;

public static class NameMatching
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var sb = new StringBuilder();
        bool lastSpace = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) { sb.Append(ch); lastSpace = false; }
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                if (!lastSpace && sb.Length > 0) { sb.Append(' '); lastSpace = true; }
            }
            // drop other punctuation
        }
        return sb.ToString().Trim();
    }

    public static bool Matches(string? a, string? b) => Normalize(a) == Normalize(b);
}
```

- [ ] **Step 4: Run, expect PASS**. - [ ] **Step 5: Commit** (`feat: add name matching`)

---

## Task 4: FestivalStore (TDD for JSON + lookups)

**Files:** Create `Data/FestivalStore.cs`, Test `Tests/FestivalStoreTests.cs`

- [ ] **Step 1: Failing test** (round-trip + lookup by folder/file)

```csharp
using Jellyfin.Plugin.Festivals.Data;
using Jellyfin.Plugin.Festivals.Model;
using Xunit;

public class FestivalStoreTests
{
    private static FestivalStore NewStore(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), "fest-" + Guid.NewGuid());
        Directory.CreateDirectory(dir);
        return new FestivalStore(dir);
    }

    [Fact]
    public void Save_then_load_roundtrips()
    {
        var s = NewStore(out _);
        s.Replace(new FestivalDatabase { Festivals = { new Festival { Id = "1", Name = "Rock am Ring" } } });
        var reloaded = new FestivalStore(s.DataDir).GetAll();
        Assert.Single(reloaded.Festivals);
        Assert.Equal("Rock am Ring", reloaded.Festivals[0].Name);
    }

    [Fact]
    public void FindFestival_matches_folder_name_case_insensitive()
    {
        var s = NewStore(out _);
        s.Replace(new FestivalDatabase { Festivals = { new Festival { Id = "1", Name = "Rock am Ring" } } });
        Assert.NotNull(s.FindFestival("rock am ring"));
        Assert.Null(s.FindFestival("Wacken"));
    }

    [Fact]
    public void FindRecording_matches_file_and_year()
    {
        var s = NewStore(out _);
        var f = new Festival { Id = "1", Name = "Rock am Ring",
            Years = { new FestivalYear { Year = 2026,
                Recordings = { new Recording { FileMatch = "Sabaton", Artist = "Sabaton" } } } } };
        s.Replace(new FestivalDatabase { Festivals = { f } });
        Assert.Equal("Sabaton", s.FindRecording("Rock am Ring", 2026, "sabaton")!.Artist);
    }
}
```

- [ ] **Step 2: Run, expect FAIL.**

- [ ] **Step 3: Implement**

```csharp
using System.Text.Json;
using Jellyfin.Plugin.Festivals.Model;

namespace Jellyfin.Plugin.Festivals.Data;

public class FestivalStore
{
    private readonly object _lock = new();
    private readonly string _jsonPath;
    public string DataDir { get; }
    public string ImageDir { get; }

    public FestivalStore(string dataDir)
    {
        DataDir = dataDir;
        _jsonPath = Path.Combine(dataDir, "festivals.json");
        ImageDir = Path.Combine(dataDir, "images");
        Directory.CreateDirectory(ImageDir);
    }

    public FestivalDatabase GetAll()
    {
        lock (_lock)
        {
            if (!File.Exists(_jsonPath)) return new FestivalDatabase();
            var json = File.ReadAllText(_jsonPath);
            return JsonSerializer.Deserialize<FestivalDatabase>(json) ?? new FestivalDatabase();
        }
    }

    public void Replace(FestivalDatabase db)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_jsonPath, json);
        }
    }

    public Festival? FindFestival(string folderName)
        => GetAll().Festivals.FirstOrDefault(f => NameMatching.Matches(f.Name, folderName));

    public Recording? FindRecording(string folderName, int year, string fileName)
    {
        var f = FindFestival(folderName);
        var y = f?.Years.FirstOrDefault(y => y.Year == year);
        return y?.Recordings.FirstOrDefault(r => NameMatching.Matches(r.FileMatch, fileName));
    }
}
```

- [ ] **Step 4: Run, expect PASS.** - [ ] **Step 5: Commit** (`feat: add festival store`)

---

## Task 5: Plugin entry + DI + config

**Files:** Replace template `Plugin.cs`, add `PluginServiceRegistrator.cs`, `Configuration/PluginConfiguration.cs`

- [ ] **Step 1: Plugin.cs** — `BasePlugin<PluginConfiguration>`, `IHasWebPages`; expose a singleton `FestivalStore` rooted at `applicationPaths.PluginsPath`/plugin data dir; register the `config.html` page in `GetPages()` with embedded resource name `Jellyfin.Plugin.Festivals.Configuration.config.html`.

- [ ] **Step 2: PluginServiceRegistrator.cs** — implement `IPluginServiceRegistrator.RegisterServices(IServiceCollection, IServerApplicationHost)` and register `FestivalStore` as singleton so providers/controller can inject it.

- [ ] **Step 3:** Mark `config.html`/`config.js` as `EmbeddedResource` in the csproj.

- [ ] **Step 4: Build** → 0 errors. - [ ] **Step 5: Commit** (`feat: plugin entry, DI, config page wiring`)

---

## Task 6: Metadata providers

**Files:** Create the three `Providers/Festival*Provider.cs`

- [ ] **Step 1: Series provider**

```csharp
public class FestivalSeriesProvider : ICustomMetadataProvider<Series>
{
    private readonly FestivalStore _store;
    public FestivalSeriesProvider(FestivalStore store) => _store = store;
    public string Name => "Festivals";

    public Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions options, CancellationToken ct)
    {
        var f = _store.FindFestival(item.Name);
        if (f is null) return Task.FromResult(ItemUpdateType.None);
        item.Overview = string.Join("\n\n",
            new[] { f.Description, f.Location is null ? null : $"Ort: {f.Location}" }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (f.Genres.Count > 0) item.Genres = f.Genres.ToArray();
        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}
```

- [ ] **Step 2: Season provider** — match `item.Series.Name` + `item.IndexNumber` (year) → set `Name`/`Overview` from `FestivalYear`.

- [ ] **Step 3: Episode provider** — derive festival folder from `item.Series?.Name`, year from `item.Season?.IndexNumber`, file from `Path.GetFileNameWithoutExtension(item.Path)`; on match set `item.Name = Artist`, `item.Overview = Bio + Stage/Date/Setlist`, `item.Genres`.

- [ ] **Step 4: Build** → 0 errors. - [ ] **Step 5: Commit** (`feat: metadata providers`)

---

## Task 7: Image provider

**Files:** Create `Providers/FestivalImageProvider.cs` implementing `IDynamicImageProvider`.

- [ ] **Step 1:** `Supports(BaseItem)` → true for `Series`, `Season`, `Episode`.
- [ ] **Step 2:** `GetSupportedImages` → Series: `[Primary, Backdrop]`; Season: `[Primary]`; Episode: `[Primary]`.
- [ ] **Step 3:** `GetImage(item, type, ct)` → resolve the right stored file (`HeroImage`→Backdrop, `Poster`→Primary, year `Poster`, recording `ArtistImage`) and return `DynamicImageResponse` with a `FileStream` + format.
- [ ] **Step 4: Build** → 0 errors. - [ ] **Step 5: Commit** (`feat: image provider`)

---

## Task 8: REST API controller

**Files:** Create `Api/FestivalApiController.cs`

- [ ] **Step 1:** `[ApiController] [Authorize(Policy="RequiresElevation")] [Route("Festivals")]`, inject `FestivalStore`.
- [ ] **Step 2:** `GET /Festivals` → full DB; `PUT /Festivals` → `Replace(db)`.
- [ ] **Step 3:** `POST /Festivals/Images` (multipart) → save file into `ImageDir`, return file name; `GET /Festivals/Images/{name}` → stream the file.
- [ ] **Step 4: Build** → 0 errors. - [ ] **Step 5: Commit** (`feat: rest api controller`)

---

## Task 9: Config management UI

**Files:** Create `Configuration/config.html` + `config.js`

- [ ] **Step 1:** Jellyfin-styled page (`<div data-role="page" class="page type-interior pluginConfigurationPage">`) listing festivals, with add/edit/delete for festivals → years → recordings and image upload buttons.
- [ ] **Step 2:** `config.js` loads `GET Festivals`, renders, and `PUT Festivals` on save; uploads via `POST Festivals/Images` using `ApiClient.getUrl` + auth headers.
- [ ] **Step 3: Build** → 0 errors. - [ ] **Step 4: Commit** (`feat: config management UI`)

---

## Task 10: Manifest, README, packaging

**Files:** `build.yaml`/`meta.json`, `README.md` (English)

- [ ] **Step 1:** Fill plugin GUID, name, version, targetAbi `10.11.8.0` in the manifest.
- [ ] **Step 2:** Write English `README.md`: what it does, folder convention, install (copy DLL to Jellyfin `plugins/`), config-page usage, build-from-source.
- [ ] **Step 3: Build Release** → produce the DLL. - [ ] **Step 4: Commit** (`docs: readme + manifest`)

---

## Task 11: Repo + push (user-authorized)

- [ ] **Step 1:** `git init`, add `.gitignore` (bin/obj), initial structure already committed per task.
- [ ] **Step 2:** `gh repo create jellyfin-plugin-festivals --public --source . --remote origin`.
- [ ] **Step 3:** `git push -u origin main`.
- [ ] **Step 4:** Output the repo URL for the user to test.

---

## Self-Review

- **Spec coverage:** Series/Season/Episode mapping ✓ (T6), JSON DB ✓ (T4), config UI ✓ (T9), REST API ✓ (T8), image provider/HERO ✓ (T7), folder matching ✓ (T3), template scaffold + net9/10.11.8 ✓ (T1), repo+push+README ✓ (T10/T11).
- **Placeholder scan:** Host-integrated steps describe exact interfaces/members verified against MediaBrowser.Controller 10.11.8; pure-logic tasks contain full code. Provider/UI step bodies are described not fully coded because they bind to host types whose exact shapes are confirmed at compile time — the build step is the gate.
- **Type consistency:** `FestivalStore`, `NameMatching.Matches/Normalize`, `Festival/FestivalYear/Recording`, `FindFestival/FindRecording/Replace/GetAll`, `ImageDir` used consistently across tasks.
