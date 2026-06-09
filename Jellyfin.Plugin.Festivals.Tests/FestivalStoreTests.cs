using System;
using System.IO;
using Jellyfin.Plugin.Festivals.Data;
using Jellyfin.Plugin.Festivals.Model;
using Xunit;

namespace Jellyfin.Plugin.Festivals.Tests;

public class FestivalStoreTests
{
    private static FestivalStore NewStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fest-" + Guid.NewGuid().ToString("N"));
        return new FestivalStore(dir);
    }

    [Fact]
    public void Save_then_load_roundtrips()
    {
        var store = NewStore();
        store.Replace(new FestivalDatabase
        {
            Festivals = { new Festival { Id = "1", Name = "Rock am Ring" } }
        });

        var reloaded = new FestivalStore(store.DataDir).GetAll();

        Assert.Single(reloaded.Festivals);
        Assert.Equal("Rock am Ring", reloaded.Festivals[0].Name);
    }

    [Fact]
    public void GetAll_returns_empty_when_no_file()
    {
        var store = NewStore();
        Assert.Empty(store.GetAll().Festivals);
    }

    [Fact]
    public void FindFestival_matches_folder_name_case_insensitive()
    {
        var store = NewStore();
        store.Replace(new FestivalDatabase
        {
            Festivals = { new Festival { Id = "1", Name = "Rock am Ring" } }
        });

        Assert.NotNull(store.FindFestival("rock am ring"));
        Assert.Null(store.FindFestival("Wacken"));
    }

    [Fact]
    public void FindRecording_matches_file_and_year()
    {
        var store = NewStore();
        var festival = new Festival
        {
            Id = "1",
            Name = "Rock am Ring",
            Years =
            {
                new FestivalYear
                {
                    Year = 2026,
                    Recordings = { new Recording { FileMatch = "Sabaton", Artist = "Sabaton" } }
                }
            }
        };
        store.Replace(new FestivalDatabase { Festivals = { festival } });

        Assert.Equal("Sabaton", store.FindRecording("Rock am Ring", 2026, "sabaton")!.Artist);
        Assert.Null(store.FindRecording("Rock am Ring", 2025, "sabaton"));
        Assert.Null(store.FindRecording("Rock am Ring", 2026, "metallica"));
    }
}
