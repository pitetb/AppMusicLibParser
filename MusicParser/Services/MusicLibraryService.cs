using Microsoft.Extensions.Logging;
using MusicParser.Models;
using MusicParser.Parsers;

namespace MusicParser.Services;

/// <summary>
/// Implémentation du service de bibliothèque musicale
/// </summary>
public class MusicLibraryService : IMusicLibraryService
{
    private readonly ILogger<MusicLibraryService> _logger;

    public MusicLibraryService(ILogger<MusicLibraryService> logger)
    {
        _logger = logger;
    }

    public MusicLibrary ParseLibrary(string filePath)
    {
        _logger.LogDebug("Début du parsing de {FilePath}", filePath);
        
        if (!File.Exists(filePath))
        {
            _logger.LogError("Le fichier {FilePath} n'existe pas", filePath);
            throw new FileNotFoundException($"Le fichier '{filePath}' n'existe pas.", filePath);
        }

        var fileInfo = new FileInfo(filePath);
        _logger.LogDebug("Fichier: {FilePath}", filePath);
        _logger.LogDebug("Taille: {Size:N0} octets", fileInfo.Length);

        var parser = new MusicDbParser(filePath, _logger);
        var library = parser.Parse();
        
        _logger.LogDebug("Parsing terminé: {TrackCount} pistes, {AlbumCount} albums, {ArtistCount} artistes, {PlaylistCount} playlists",
            library.Tracks.Count, library.Albums.Count, library.Artists.Count, library.Playlists.Count);
        
        return library;
    }
}
