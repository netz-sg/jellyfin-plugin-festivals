using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Festivals.Model;

namespace Jellyfin.Plugin.Festivals.Data;

/// <summary>
/// Owns the festival JSON database and the associated image directory.
/// </summary>
public class FestivalStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly object _lock = new();
    private readonly string _jsonPath;
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalStore"/> class.
    /// </summary>
    /// <param name="dataDir">The plugin data directory.</param>
    public FestivalStore(string dataDir)
    {
        DataDir = dataDir;
        Directory.CreateDirectory(dataDir);
        _jsonPath = Path.Combine(dataDir, "festivals.json");
        _settingsPath = Path.Combine(dataDir, "settings.json");
        ImageDir = Path.Combine(dataDir, "images");
        Directory.CreateDirectory(ImageDir);
    }

    /// <summary>Gets the plugin data directory.</summary>
    public string DataDir { get; }

    /// <summary>Gets the directory holding uploaded images.</summary>
    public string ImageDir { get; }

    /// <summary>
    /// Returns the full database (empty when none exists yet).
    /// </summary>
    /// <returns>The database.</returns>
    public FestivalDatabase GetAll()
    {
        lock (_lock)
        {
            if (!File.Exists(_jsonPath))
            {
                return new FestivalDatabase();
            }

            var json = File.ReadAllText(_jsonPath);
            return JsonSerializer.Deserialize<FestivalDatabase>(json, _jsonOptions) ?? new FestivalDatabase();
        }
    }

    /// <summary>
    /// Replaces the entire database.
    /// </summary>
    /// <param name="db">The new database.</param>
    public void Replace(FestivalDatabase db)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(db, _jsonOptions);
            File.WriteAllText(_jsonPath, json);
        }
    }

    /// <summary>
    /// Gets the plugin settings.
    /// </summary>
    /// <returns>The settings.</returns>
    public FestivalSettings GetSettings()
    {
        lock (_lock)
        {
            if (!File.Exists(_settingsPath))
            {
                return new FestivalSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<FestivalSettings>(json, _jsonOptions) ?? new FestivalSettings();
        }
    }

    /// <summary>
    /// Saves the plugin settings.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public void SaveSettings(FestivalSettings settings)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
    }

    /// <summary>
    /// Finds a festival by (folder) name.
    /// </summary>
    /// <param name="folderName">The series folder name.</param>
    /// <returns>The matching festival or null.</returns>
    public Festival? FindFestival(string? folderName)
        => GetAll().Festivals.FirstOrDefault(f => NameMatching.Matches(f.Name, folderName));

    /// <summary>
    /// Finds a festival edition by name and year.
    /// </summary>
    /// <param name="folderName">The series folder name.</param>
    /// <param name="year">The year.</param>
    /// <returns>The matching edition or null.</returns>
    public FestivalYear? FindYear(string? folderName, int year)
        => FindFestival(folderName)?.Years.FirstOrDefault(y => y.Year == year);

    /// <summary>
    /// Finds a recording by festival name, year and file name.
    /// </summary>
    /// <param name="folderName">The series folder name.</param>
    /// <param name="year">The year.</param>
    /// <param name="fileName">The episode file name without extension.</param>
    /// <returns>The matching recording or null.</returns>
    public Recording? FindRecording(string? folderName, int year, string? fileName)
        => FindYear(folderName, year)?.Recordings.FirstOrDefault(r => NameMatching.Matches(r.FileMatch, fileName));
}
