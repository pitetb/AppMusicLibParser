namespace MusicParser.Models;

/// <summary>
/// Représente un album dans la bibliothèque Apple Music
/// </summary>
public class Album
{
    public ulong AlbumId { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? AlbumArtist { get; set; }

    public override string ToString()
    {
        return $"[{AlbumId}] {Title} - {AlbumArtist ?? Artist ?? "Unknown"}";
    }
}
