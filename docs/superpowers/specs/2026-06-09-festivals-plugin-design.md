# Jellyfin.Plugin.Festivals — Design

**Datum:** 2026-06-09
**Status:** Approved (Brainstorming)
**Ziel-Plattform:** Jellyfin 10.11.8 · .NET 9 (net9.0) · SDK 9.0.306

## Zweck

Ein Jellyfin-Plugin, das Musik-Festivals als durchsuchbare Bibliothek darstellt.
Eine Bibliothek „Festivals" enthält Festivals (z.B. Rock am Ring) mit HERO-Bild und
Infos; darin liegen Jahre (2026, 2025, …); in den Jahren liegen die Recordings
(Auftritte) mit Infos zum gezeigten Artist.

## Kern-Entscheidung: Mapping auf Jellyfins Serien-Modell

Ein Jellyfin-Plugin kann **keine eigene Browse-UI** bauen — die Web-Oberfläche ist
fix. Die gewünschte Hierarchie wird daher auf das native Serien-Modell abgebildet,
wodurch HERO/Backdrop, Drill-Down und Playback nativ funktionieren:

| Fachlich | Jellyfin-Typ | Native UI-Features |
|---|---|---|
| Festival (Rock am Ring) | `Series` | HERO/Backdrop, Poster, Overview, Genres |
| Jahr (2026) | `Season` | eigene Ordner-Ebene |
| Recording (Auftritt) | `Episode` | Titel = Artist, Overview = Artist-Infos, Thumbnail |

Die Bibliothek wird als **Shows-Bibliothek** namens „Festivals" angelegt.

### Ordner-Konvention auf der Platte

```
Festivals/
  Rock am Ring/
    2026/
      Sabaton.mkv
      Bring Me The Horizon.mkv
    2025/
      ...
```

- Festival = Top-Ordnername
- Jahr = Staffel-Nummer (numerischer Ordner)
- Recording = Dateiname

Dateien sind bereits vorhanden; Verknüpfung erfolgt per **Ordner-/Dateinamen-Matching**
(case-insensitive, normalisiert).

## Datenquelle: Eigene Plugin-DB + Config-UI

Keine Online-API, kein Auto-Download, kein SQLite. Stattdessen:

- **`festivals.json`** im Plugin-Datenordner als Datenspeicher
- **Bild-Ordner** im Plugin-Datenordner für HEROs / Poster / Artist-Fotos
- **Config-/Management-Seite** im Jellyfin-Dashboard zum Pflegen aller Daten

### Datenmodell

```
Festival {
  Id, Name, Beschreibung, Ort, Genres[],
  HeroBild (Datei-Ref), Poster (Datei-Ref),
  Jahre: Jahr[]
}
Jahr {
  Wert: 2026, Beschreibung?, Poster? (Datei-Ref),
  Recordings: Recording[]
}
Recording {
  DateinameMatch,          // Schlüssel fürs Episode-Matching
  Artist,                  // -> Episode-Titel
  Buehne?, Datum?,         // -> Overview
  Genres[],
  Bio,                     // -> Overview
  ArtistFoto (Datei-Ref),  // -> Episode-Bild
  Setlist?                 // -> Overview
}
```

## Architektur: 5 Bausteine

### 1. `FestivalStore` (Daten-Service)
Lädt/speichert `festivals.json`, verwaltet Bild-Dateien im Plugin-Datenordner.
Thread-sicher (Lock beim Schreiben). Stellt Lookup-Methoden für die Provider bereit
(Festival per Ordnername, Recording per Dateiname).

### 2. Config-/Management-UI (`Configuration/config.html` + JS)
Im Dashboard unter „Plugins" eingebunden via `IHasWebPages`. Funktionen:
- Festivals anlegen/bearbeiten/löschen → Jahre → Recordings
- HERO-/Poster-/Artist-Bilder hochladen
- Texte (Beschreibung, Bio, Setlist) pflegen
- Anzeige des Match-Status (gematcht vs. verwaist)

CRUD läuft gegen die Plugin-REST-API.

### 3. `FestivalApiController` (REST-API)
ASP.NET-Controller (Jellyfin-Plugin-API). Endpunkte für:
- CRUD auf Festivals/Jahre/Recordings (JSON)
- Bild-Upload (multipart) und Bild-Auslieferung
- Match-Status-Abfrage

### 4. Metadaten-Provider
`ICustomMetadataProvider<T>` für `Series`, `Season`, `Episode` — laufen nach dem
Bibliotheks-Scan, matchen das Item per Ordner-/Dateiname gegen den Store und setzen:
- **Series:** Overview = Festival-Beschreibung + Ort; Genres
- **Season:** Name/Overview = Jahr-Infos
- **Episode:** Titel = Artist; Overview = Bio + Bühne/Datum/Setlist; Genres

Fehlt ein DB-Eintrag, bleibt das Item unverändert (kein Fehler).

### 5. Bild-Provider
`IDynamicImageProvider` (bzw. passende Image-Provider-Schnittstelle) liefert:
- Festival (Series): HERO/Backdrop + Poster
- Jahr (Season): Poster
- Recording (Episode): Artist-Foto (Primary/Thumb)

Quelle: Bild-Ordner im Plugin-Datenordner.

## Felder pro Auftritt (final)

Artist · Bühne · Datum/Uhrzeit · Genres · Bio/Beschreibung · Artist-Foto · Setlist (optional)

## Technik / Build

- Template: offizielles `jellyfin-plugin-template` (`dotnet new`)
- TFM: `net9.0`
- NuGet: `Jellyfin.Controller` 10.11.8
- Build: `dotnet build` → Plugin-DLL → Kopie in Jellyfins Plugin-Ordner
- Build-Fehler-Risiko minimiert: Provider isoliert, Null-/Match-sicher

## Bewusst weggelassen (YAGNI)

- Keine Online-API / kein Auto-Metadata-Download
- Kein SQLite (JSON genügt, ist versionierbar)
- Keine eigene Browse-UI (nutzt native Jellyfin-Serien-UI)

## Offene Risiken / während Implementierung zu verifizieren

- Exakte Image-Provider-Schnittstelle in 10.11 (`IDynamicImageProvider` vs.
  `IRemoteImageProvider`) — gegen die echte `MediaBrowser.Controller` 10.11.8 prüfen.
- Reihenfolge/Trigger von `ICustomMetadataProvider` ggü. den eingebauten Providern.
- Routing-/Auth-Attribute des Plugin-API-Controllers in 10.11.
