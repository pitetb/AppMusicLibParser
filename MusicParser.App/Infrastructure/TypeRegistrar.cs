using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MusicParser.App.Infrastructure;

/// <summary>
/// Intégration de l'injection de dépendances avec Spectre.Console.Cli
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services);
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddTransient(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddTransient(service, _ => factory());
    }
}
