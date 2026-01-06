namespace MusicParser.Models;

/// <summary>
/// Statut j'aime/j'aime pas d'une piste
/// </summary>
public enum LikeStatus
{
    /// <summary>Neutre (pas de préférence)</summary>
    Neutral = 0,
    /// <summary>Je n'aime plus (état transitoire après retrait d'un like)</summary>
    Disliked = 1,
    /// <summary>J'aime</summary>
    Liked = 2,
    /// <summary>Je n'aime pas (explicitement défini par l'utilisateur)</summary>
    DislikedExplicit = 3
}

/// <summary>
/// Représente une piste musicale dans la bibliothèque Apple Music
/// </summary>
public class MusicTrack
{
    public ulong TrackId { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? AlbumArtist { get; set; }
    public string? Composer { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public int? TrackNumber { get; set; }
    public int? DiscNumber { get; set; }
    public int? Duration { get; set; } // en millisecondes
    public int? BitRate { get; set; }
    public int? SampleRate { get; set; }
    public int? PlayCount { get; set; }
    public DateTime? DateAdded { get; set; }
    public DateTime? DateModified { get; set; }
    public DateTime? LastPlayed { get; set; }
    public string? FilePath { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSize { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public LikeStatus? LikeStatus { get; set; }
    
    // Références vers album et artiste (itma offsets 172 et 180)
    public ulong? AlbumRef { get; set; }
    public ulong? ArtistRef { get; set; }
    
    // Champs supplémentaires de la doc
    public string? Kind { get; set; } // 0x0006
    public string? Grouping { get; set; } // 0x000E (Classical)
    public string? SortTitle { get; set; } // 0x001E
    public string? SortAlbum { get; set; } // 0x001F
    public string? SortArtist { get; set; } // 0x0020
    public string? SortAlbumArtist { get; set; } // 0x0021
    public string? SortComposer { get; set; } // 0x0022
    public string? WorkName { get; set; } // 0x003F (Classical)
    public string? MovementName { get; set; } // 0x0040 (Classical)
    public int? MovementNumber { get; set; } // Offset 88 in itma
    public int? MovementCount { get; set; } // Offset 86 in itma

    public override string ToString()
    {
        return $"[{TrackId}] {Artist} - {Title} ({Album})";
    }
}
