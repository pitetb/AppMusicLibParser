using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using MusicParser;
using MusicParser.App.Commands;
using MusicParser.App.Infrastructure;
using Serilog;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Cli;

// Charger les variables d'environnement depuis .env
DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { ".env" }));

// Configuration de Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Spectre", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

try
{
    AnsiConsole.Write(
        new FigletText("MusicParser")
            .Centered()
            .Color(Color.Blue));
    
    AnsiConsole.MarkupLine("[dim]Apple Music Library Parser[/]");
    AnsiConsole.WriteLine();

    // Configuration DI
    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.AddSerilog(Log.Logger);
    });
    services.AddMusicParser();

    // Configuration Spectre.Console.Cli
    var registrar = new TypeRegistrar(services);
    var app = new CommandApp(registrar);

    app.Configure(config =>
    {
        config.SetApplicationName("musicparser");
        
        // Commande info
        config.AddCommand<InfoCommand>("info")
            .WithDescription("Affiche les informations générales de la bibliothèque")
            .WithExample(new[] { "info" })
            .WithExample(new[] { "info", "/path/to/Library.musicdb" });
        
        // Commande stats
        config.AddCommand<StatsCommand>("stats")
            .WithDescription("Affiche les statistiques de la bibliothèque")
            .WithExample(new[] { "stats" })
            .WithExample(new[] { "stats", "/path/to/Library.musicdb" })
            .WithExample(new[] { "stats", "--top", "10" });
        
        // Commande ratings
        config.AddCommand<RatingsCommand>("ratings")
            .WithDescription("Affiche la distribution des ratings et des exemples de pistes")
            .WithExample(new[] { "ratings" })
            .WithExample(new[] { "ratings", "/path/to/Library.musicdb" })
            .WithExample(new[] { "ratings", "--count", "20" });
        
        // Commande compare
        config.AddCommand<CompareCommand>("compare")
            .WithDescription("Compare deux fichiers MusicDB déchiffrés")
            .WithExample(new[] { "compare", "file1.musicdb", "file2.musicdb" });
        
        // Commande likes
        config.AddCommand<LikesCommand>("likes")
            .WithDescription("Affiche les statistiques des j'aime/j'aime pas")
            .WithExample(new[] { "likes" })
            .WithExample(new[] { "likes", "/path/to/Library.musicdb" })
            .WithExample(new[] { "likes", "--examples", "20" });
        
        // Commande dump-offset (debug)
        config.AddCommand<DumpOffsetCommand>("dump-offset")
            .WithDescription("Dump le contenu déchiffré à un offset spécifique")
            .WithExample(new[] { "dump-offset", "file.musicdb", "0x2214" });
        
        // Commande search
        config.AddCommand<SearchCommand>("search")
            .WithDescription("Recherche une piste par titre")
            .WithExample(new[] { "search", "libraries-music-samples/Library.musicdb", "Bohemian" });

        // Autres commandes à venir...
        // config.AddCommand<ExportCommand>("export");
        // config.AddCommand<SearchCommand>("search");
    });

    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Une erreur inattendue s'est produite");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
