using System.ComponentModel;
using MusicParser.Models;
using MusicParser.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

/// <summary>
/// Commande pour afficher les statistiques des j'aime/j'aime pas
/// </summary>
public class LikesCommand : Command<LikesCommand.Settings>
{
    private readonly IMusicLibraryService _musicService;

    public LikesCommand(IMusicLibraryService musicService)
    {
        _musicService = musicService;
    }

    public class Settings : CommandSettings
    {
        [Description("Chemin vers le fichier Library.musicdb")]
        [CommandArgument(0, "[libraryPath]")]
        public string? LibraryPath { get; set; }

        [Description("Nombre d'exemples de pistes à afficher par catégorie")]
        [CommandOption("-e|--examples")]
        [DefaultValue(10)]
        public int ExampleCount { get; set; } = 10;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var libraryPath = settings.LibraryPath ?? "libraries-music-samples/Library.musicdb";

            if (!File.Exists(libraryPath))
            {
                AnsiConsole.MarkupLine($"[red]❌ Fichier introuvable: {libraryPath}[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[cyan]📚 Bibliothèque:[/] {libraryPath}");
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

            // Grouper par statut
            var tracksByStatus = library.Tracks
                .GroupBy(t => t.LikeStatus ?? LikeStatus.Neutral)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Afficher les statistiques globales
            AnsiConsole.WriteLine();
            var statsTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Title("[bold blue]📊 Statistiques J'aime/J'aime pas[/]")
                .AddColumn(new TableColumn("[bold]Statut[/]").Centered())
                .AddColumn(new TableColumn("[bold]Nombre[/]").RightAligned())
                .AddColumn(new TableColumn("[bold]Pourcentage[/]").RightAligned());

            var total = library.Tracks.Count;
            
            var likedCount = tracksByStatus.GetValueOrDefault(LikeStatus.Liked)?.Count ?? 0;
            var dislikedCount = tracksByStatus.GetValueOrDefault(LikeStatus.Disliked)?.Count ?? 0;
            var dislikedExplicitCount = tracksByStatus.GetValueOrDefault(LikeStatus.DislikedExplicit)?.Count ?? 0;
            var neutralCount = tracksByStatus.GetValueOrDefault(LikeStatus.Neutral)?.Count ?? 0;

            statsTable.AddRow(
                "[green]❤️  J'aime[/]",
                $"[green]{likedCount:N0}[/]",
                $"[green]{(total > 0 ? likedCount * 100.0 / total : 0):F1}%[/]"
            );
            
            statsTable.AddRow(
                "[orange1]💔 Je n'aime plus[/]",
                $"[orange1]{dislikedCount:N0}[/]",
                $"[orange1]{(total > 0 ? dislikedCount * 100.0 / total : 0):F1}%[/]"
            );
            
            statsTable.AddRow(
                "[red]👎 J'aime pas[/]",
                $"[red]{dislikedExplicitCount:N0}[/]",
                $"[red]{(total > 0 ? dislikedExplicitCount * 100.0 / total : 0):F1}%[/]"
            );
            
            statsTable.AddRow(
                "[dim]⚪ Neutre[/]",
                $"[dim]{neutralCount:N0}[/]",
                $"[dim]{(total > 0 ? neutralCount * 100.0 / total : 0):F1}%[/]"
            );

            statsTable.AddEmptyRow();
            statsTable.AddRow(
                "[bold]Total[/]",
                $"[bold]{total:N0}[/]",
                "[bold]100.0%[/]"
            );

            AnsiConsole.Write(statsTable);
            AnsiConsole.WriteLine();

            // Afficher des exemples pour chaque statut
            DisplayExamples(LikeStatus.Liked, tracksByStatus, settings.ExampleCount, "❤️  J'aime", Color.Green);
            DisplayExamples(LikeStatus.Disliked, tracksByStatus, settings.ExampleCount, "💔 Je n'aime plus", Color.Orange1);
            DisplayExamples(LikeStatus.DislikedExplicit, tracksByStatus, settings.ExampleCount, "👎 J'aime pas", Color.Red);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Erreur: {ex.Message}[/]");
            return 1;
        }
    }

    private void DisplayExamples(LikeStatus status, Dictionary<LikeStatus, List<MusicTrack>> tracksByStatus, int count, string title, Color color)
    {
        if (!tracksByStatus.TryGetValue(status, out var tracks) || tracks.Count == 0)
            return;

        var examples = tracks.Take(count).ToList();

        AnsiConsole.Write(new Rule($"[bold {color}]{title} - Exemples ({examples.Count}/{tracks.Count})[/]"));
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(color)
            .AddColumn("#")
            .AddColumn("Titre")
            .AddColumn("Artiste")
            .AddColumn("Album");

        for (int i = 0; i < examples.Count; i++)
        {
            var track = examples[i];
            table.AddRow(
                $"{i + 1}",
                track.Title ?? "[dim]Sans titre[/]",
                track.Artist ?? "[dim]Inconnu[/]",
                track.Album ?? "[dim]Inconnu[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}
