using System.ComponentModel;
using MusicParser.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

/// <summary>
/// Commande pour afficher les statistiques de la bibliothèque
/// </summary>
public class StatsCommand : Command<StatsCommand.Settings>
{
    private readonly IMusicLibraryService _musicService;

    public StatsCommand(IMusicLibraryService musicService)
    {
        _musicService = musicService;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[FILE]")]
        [Description("Chemin vers le fichier Library.musicdb")]
        public string? FilePath { get; set; }

        [CommandOption("--top <COUNT>")]
        [Description("Nombre de pistes les plus écoutées à afficher")]
        [DefaultValue(5)]
        public int TopCount { get; set; } = 5;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Déterminer le chemin du fichier
            var libraryPath = settings.FilePath 
                ?? "libraries-music-samples/Monterey/BigLibrary.musicdb";

            // Vérifier que le fichier existe
            if (!File.Exists(libraryPath))
            {
                AnsiConsole.MarkupLine("[red]❌ Erreur: Le fichier n'existe pas:[/]");
                AnsiConsole.WriteLine($"  {libraryPath}");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Usage: musicparser stats <FILE> --top <COUNT>[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[cyan]📁 Fichier:[/] {libraryPath}");
            AnsiConsole.MarkupLine($"[cyan]📊 Taille:[/] {new FileInfo(libraryPath).Length:N0} octets");
            AnsiConsole.WriteLine();

            // Parser avec progress
            var library = AnsiConsole.Status()
                .Start("Parsing de la bibliothèque...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    return _musicService.ParseLibrary(libraryPath);
                });

            // Calculer les statistiques
            var tracksWithPlayCount = library.Tracks.Count(t => t.PlayCount.HasValue && t.PlayCount > 0);
            var totalPlays = library.Tracks.Where(t => t.PlayCount.HasValue).Sum(t => t.PlayCount!.Value);
            var averagePlays = tracksWithPlayCount > 0 ? (double)totalPlays / tracksWithPlayCount : 0;

            // Afficher les résultats dans un tableau
            AnsiConsole.WriteLine();
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .AddColumn(new TableColumn("[bold]Catégorie[/]").Centered())
                .AddColumn(new TableColumn("[bold]Valeur[/]").RightAligned());

            table.AddRow("Pistes totales", $"[green]{library.Tracks.Count:N0}[/]");
            table.AddRow("Albums", $"[green]{library.Albums.Count:N0}[/]");
            table.AddRow("Artistes", $"[green]{library.Artists.Count:N0}[/]");
            table.AddRow("Playlists", $"[green]{library.Playlists.Count:N0}[/]");
            table.AddEmptyRow();
            table.AddRow("Pistes avec lecture", $"[yellow]{tracksWithPlayCount:N0}[/]");
            table.AddRow("Lectures totales", $"[yellow]{totalPlays:N0}[/]");
            
            if (tracksWithPlayCount > 0)
            {
                table.AddRow("Moyenne lectures/piste", $"[yellow]{averagePlays:F1}[/]");
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Top pistes les plus écoutées
            var mostPlayedTracks = library.Tracks
                .Where(t => t.PlayCount.HasValue && t.PlayCount > 0)
                .OrderByDescending(t => t.PlayCount)
                .Take(settings.TopCount)
                .ToList();

            if (mostPlayedTracks.Any())
            {
                var topTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Magenta1)
                    .Title($"[bold magenta]🎵 Top {settings.TopCount} pistes les plus écoutées[/]")
                    .AddColumn("#")
                    .AddColumn("Titre")
                    .AddColumn("Artiste")
                    .AddColumn("Album")
                    .AddColumn(new TableColumn("Lectures").RightAligned());

                for (int i = 0; i < mostPlayedTracks.Count; i++)
                {
                    var track = mostPlayedTracks[i];
                    topTable.AddRow(
                        $"{i + 1}",
                        track.Title ?? "[dim]Sans titre[/]",
                        track.Artist ?? "[dim]Inconnu[/]",
                        track.Album ?? "[dim]Inconnu[/]",
                        $"[green]{track.PlayCount:N0}[/]"
                    );
                }

                AnsiConsole.Write(topTable);
                AnsiConsole.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Erreur: {ex.Message}[/]");
            return 1;
        }
    }
}
