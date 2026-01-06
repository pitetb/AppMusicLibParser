using Microsoft.Extensions.DependencyInjection;
using MusicParser.Services;

namespace MusicParser;

/// <summary>
/// Extensions pour configurer les services MusicParser
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les services MusicParser à la collection de services
    /// </summary>
    public static IServiceCollection AddMusicParser(this IServiceCollection services)
    {
        services.AddScoped<IMusicLibraryService, MusicLibraryService>();
        return services;
    }
}
