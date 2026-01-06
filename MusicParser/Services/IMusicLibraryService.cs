using MusicParser.Models;

namespace MusicParser.Services;

/// <summary>
/// Service pour parser une bibliothèque Apple Music
/// </summary>
public interface IMusicLibraryService
{
    /// <summary>
    /// Parse un fichier Library.musicdb et retourne la bibliothèque musicale
    /// </summary>
    /// <param name="filePath">Chemin vers le fichier Library.musicdb</param>
    /// <returns>La bibliothèque musicale parsée</returns>
    MusicLibrary ParseLibrary(string filePath);
}
