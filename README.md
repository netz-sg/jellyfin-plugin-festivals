# Jellyfin Festivals Plugin

Browse music festivals in Jellyfin as a native, navigable library.

A **Festivals** library shows each festival (e.g. *Rock am Ring*) with a hero/backdrop
image and details. Drilling into a festival reveals its **editions by year**
(2026, 2025, …), and inside each year the **recorded performances**, each enriched
with information about the artist that played.

It works by mapping the festival hierarchy onto Jellyfin's native Series model, so
hero art, drill‑down navigation and playback all behave like any other library —
no custom UI required.

| Concept | Jellyfin type | What you get |
| --- | --- | --- |
| Festival (Rock am Ring) | Series | Hero/backdrop, poster, overview, genres |
| Year (2026) | Season | Its own folder level |
| Recording (a performance) | Episode | Title = artist, overview = artist info, thumbnail |

All metadata — descriptions, hero images, posters, artist photos, stages, dates and
setlists — is managed through the plugin's **dashboard configuration page**. The data
is stored in a plugin‑owned JSON file plus an image folder; no external API is used.

## Requirements

- Jellyfin **10.11** (built against `Jellyfin.Controller` 10.11.8, targeting .NET 9)

## Media folder layout

Organize the recordings on disk like a TV show, using the festival name as the top
folder, the year as the season folder, and one file per performance:

```
Festivals/
  Rock am Ring/
    2026/
      Sabaton.mkv
      Bring Me The Horizon.mkv
    2025/
      Metallica.mkv
```

- **Festival** = top folder name → matched against the festival *Name* in the config page
- **Year** = numeric season folder → matched against the *Year*
- **Recording** = file name (without extension) → matched against the *File match* value

Matching is case‑insensitive and ignores punctuation and extra whitespace.

## Installation

1. Build the plugin (see below) or download `Jellyfin.Plugin.Festivals.dll`.
2. Copy the DLL into your Jellyfin `plugins/Festivals/` directory:
   - Linux: `/var/lib/jellyfin/plugins/Festivals/`
   - Windows: `%ProgramData%\Jellyfin\Server\plugins\Festivals\`
3. Restart Jellyfin.
4. Create a library of type **Shows** named *Festivals* pointing at your festivals folder.

## Usage

1. Open **Dashboard → Plugins → Festivals**.
2. Add a festival, fill in its name (matching the folder), location, description,
   genres, and upload a hero/backdrop and poster image.
3. Add one or more **years**, then add **performances** to each year — set the
   *File match*, artist, stage, date, genres, bio, setlist and an artist image.
4. Click **Save**.
5. In your Festivals library, run **Refresh metadata** so the providers apply the data.

## Build from source

Requires the .NET 9 SDK.

```bash
dotnet build Jellyfin.Plugin.Festivals/Jellyfin.Plugin.Festivals.csproj -c Release
```

The resulting plugin DLL is written to
`Jellyfin.Plugin.Festivals/bin/Release/net9.0/Jellyfin.Plugin.Festivals.dll`.

Run the unit tests with:

```bash
dotnet test
```

## How it works

- **`FestivalStore`** owns `festivals.json` and an `images/` folder in the plugin data directory.
- **`FestivalApiController`** (`/Festivals`) provides CRUD and image upload/serving for the config page.
- **`FestivalSeriesProvider` / `FestivalSeasonProvider` / `FestivalEpisodeProvider`**
  (`ICustomMetadataProvider`) enrich items after the library scan by matching folder/file names.
- **`FestivalImageProvider`** (`IDynamicImageProvider`) supplies hero, poster and artist images.

## License

MIT
