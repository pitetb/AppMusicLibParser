namespace MusicParser.Models;

/// <summary>
/// Type de playlist
/// </summary>
public enum PlaylistType
{
    Manual,      // Playlist manuelle créée par l'utilisateur
    Smart,       // Playlist intelligente avec critères automatiques
    System,      // Playlist système (Bibliothèque, Téléchargés, etc.)
    Folder       // Dossier contenant d'autres playlists
}

/// <summary>
/// Représente une playlist dans la bibliothèque Apple Music
/// </summary>
public class Playlist
{
    public ulong PlaylistId { get; set; }
    public string? Name { get; set; }
    public int TrackCount { get; set; }
    public PlaylistType Type { get; set; }
    public ulong ParentId { get; set; }
    public int DistinguishedKind { get; set; }
    public bool HasSmartCriteria { get; set; }
    public bool IsMaster { get; set; }  // True si c'est la playlist Master (ID = 5)
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public List<ulong> TrackIds { get; set; } = new();

    public override string ToString()
    {
        return $"[{PlaylistId}] {Name} ({TrackCount} pistes)";
    }
}
