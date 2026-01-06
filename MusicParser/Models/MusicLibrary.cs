namespace MusicParser.Models;

/// <summary>
/// Représente une bibliothèque musicale complète
/// </summary>
public class MusicLibrary
{
    public List<MusicTrack> Tracks { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
    public List<Artist> Artists { get; set; } = new();
    public List<Playlist> Playlists { get; set; } = new();
    public int TotalTracks => Tracks.Count;
    public string? LibraryPath { get; set; }
    public DateTime? ParsedAt { get; set; }
    
    // Header information
    public ulong LibraryId { get; set; }
    public string? VersionString { get; set; }
    public ushort MajorVersion { get; set; }
    public ushort MinorVersion { get; set; }
    public uint FileType { get; set; }
    public uint EnvelopeLength { get; set; }
    public uint FileSize { get; set; }
    public uint MaxCryptSize { get; set; }
    public uint HeaderTrackCount { get; set; }
    public uint HeaderAlbumCount { get; set; }
    public uint HeaderArtistCount { get; set; }
    public uint HeaderPlaylistCount { get; set; }

    public void AddTrack(MusicTrack track)
    {
        Tracks.Add(track);
    }

    public void DisplaySummary()
    {
        Console.WriteLine($"\n=== Bibliothèque Musicale ===");
        Console.WriteLine($"Nombre total de pistes: {TotalTracks}");
        Console.WriteLine($"Fichier: {LibraryPath}");
        Console.WriteLine($"Analysé le: {ParsedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"=============================\n");
    }

    public void DisplayTracks(int limit = 20)
    {
        var tracksToDisplay = limit > 0 ? Tracks.Take(limit) : Tracks;
        
        foreach (var track in tracksToDisplay)
        {
            Console.WriteLine(track);
            if (!string.IsNullOrEmpty(track.Album))
                Console.WriteLine($"  Album: {track.Album}");
            if (track.Duration.HasValue)
                Console.WriteLine($"  Durée: {TimeSpan.FromMilliseconds(track.Duration.Value):mm\\:ss}");
            if (!string.IsNullOrEmpty(track.Genre))
                Console.WriteLine($"  Genre: {track.Genre}");
            Console.WriteLine();
        }

        if (limit > 0 && Tracks.Count > limit)
        {
            Console.WriteLine($"... et {Tracks.Count - limit} autres pistes");
        }
    }
}
