using System.ComponentModel;
using MusicParser.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

/// <summary>
/// Commande pour afficher la distribution des ratings et des exemples de pistes
/// </summary>
public class RatingsCommand : Command<RatingsCommand.Settings>
{
    private readonly IMusicLibraryService _musicService;

    public RatingsCommand(IMusicLibraryService musicService)
    {
        _musicService = musicService;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[FILE]")]
        [Description("Chemin vers le fichier Library.musicdb")]
        public string? FilePath { get; set; }

        [CommandOption("--count <COUNT>")]
        [Description("Nombre de pistes à afficher par niveau d'étoiles")]
        [DefaultValue(10)]
        public int SampleCount { get; set; } = 10;
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
                AnsiConsole.MarkupLine("[yellow]Usage: musicparser ratings <FILE> --count <COUNT>[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[cyan]📁 Fichier:[/] {libraryPath}");
            AnsiConsole.WriteLine();

            // Parser avec progress
            var library = AnsiConsole.Status()
                .Start("Parsing de la bibliothèque...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    return _musicService.ParseLibrary(libraryPath);
                });

            // Grouper les pistes par rating
            var tracksByRating = library.Tracks
                .Where(t => t.Rating.HasValue && t.Rating.Value > 0)
                .GroupBy(t => t.Rating!.Value)
                .OrderByDescending(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            var totalTracksWithRating = tracksByRating.Values.Sum(list => list.Count);
            var totalTracks = library.Tracks.Count;

            // Afficher la distribution
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold yellow]⭐ Distribution des ratings[/]"));
            AnsiConsole.WriteLine();

            var distTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Yellow)
                .AddColumn(new TableColumn("[bold]Rating[/]").Centered())
                .AddColumn(new TableColumn("[bold]Pistes[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Pourcentage[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Barre[/]"));

            // Calculer le max pour la barre de progression
            var maxCount = tracksByRating.Any() ? tracksByRating.Values.Max(list => list.Count) : 0;

            for (int stars = 5; stars >= 1; stars--)
            {
                var count = tracksByRating.ContainsKey(stars * 20) ? tracksByRating[stars * 20].Count : 0;
                var percentage = totalTracksWithRating > 0 ? (count * 100.0 / totalTracksWithRating) : 0;
                var barLength = maxCount > 0 ? (int)Math.Round(count * 40.0 / maxCount) : 0;
                var bar = new string('█', barLength);
                var color = stars switch
                {
                    5 => "green",
                    4 => "cyan",
                    3 => "yellow",
                    2 => "orange1",
                    _ => "red"
                };

                distTable.AddRow(
                    $"[{color}]{new string('⭐', stars)}[/]",
                    $"[{color}]{count:N0}[/]",
                    $"[{color}]{percentage:F1}%[/]",
                    $"[{color}]{bar}[/]"
                );
            }

            distTable.AddEmptyRow();
            distTable.AddRow(
                "[bold]Total avec rating[/]",
                $"[bold]{totalTracksWithRating:N0}[/]",
                $"[bold]{(totalTracksWithRating * 100.0 / totalTracks):F1}%[/]",
                ""
            );

            AnsiConsole.Write(distTable);
            AnsiConsole.WriteLine();

            // Afficher les exemples pour chaque niveau
            foreach (var stars in new[] { 5, 4, 3, 2, 1 })
            {
                var ratingValue = stars * 20;
                if (!tracksByRating.ContainsKey(ratingValue) || !tracksByRating[ratingValue].Any())
                    continue;

                var tracks = tracksByRating[ratingValue]
                    .OrderBy(t => t.Artist)
                    .ThenBy(t => t.Album)
                    .ThenBy(t => t.Title)
                    .Take(settings.SampleCount)
                    .ToList();

                var color = stars switch
                {
                    5 => Color.Green,
                    4 => Color.Cyan1,
                    3 => Color.Yellow,
                    2 => Color.Orange1,
                    _ => Color.Red
                };
                
                var colorName = stars switch
                {
                    5 => "green",
                    4 => "cyan",
                    3 => "yellow",
                    2 => "orange1",
                    _ => "red"
                };

                var starDisplay = new string('⭐', stars);
                AnsiConsole.Write(new Rule($"[bold {colorName}]{starDisplay} Exemples ({tracks.Count}/{tracksByRating[ratingValue].Count})[/]"));
                AnsiConsole.WriteLine();

                var trackTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(color)
                    .AddColumn("#")
                    .AddColumn("Titre")
                    .AddColumn("Artiste")
                    .AddColumn("Album");

                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    trackTable.AddRow(
                        $"{i + 1}",
                        track.Title ?? "[dim]Sans titre[/]",
                        track.Artist ?? "[dim]Inconnu[/]",
                        track.Album ?? "[dim]Inconnu[/]"
                    );
                }

                AnsiConsole.Write(trackTable);
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
