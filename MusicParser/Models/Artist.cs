namespace MusicParser.Models;

/// <summary>
/// Représente un artiste dans la bibliothèque Apple Music
/// </summary>
public class Artist
{
    public ulong ArtistId { get; set; }
    public string? Name { get; set; }

    public override string ToString()
    {
        return $"[{ArtistId}] {Name}";
    }
}
