using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MusicParser.App.Infrastructure;

/// <summary>
/// Résolveur de types pour Spectre.Console.Cli avec support de l'injection de dépendances
/// </summary>
public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceCollection _services;
    private IServiceProvider? _provider;

    public TypeResolver(IServiceCollection services)
    {
        _services = services;
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        // Build le provider seulement au premier appel (après que toutes les commandes soient enregistrées)
        _provider ??= _services.BuildServiceProvider();

        return _provider.GetService(type);
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
