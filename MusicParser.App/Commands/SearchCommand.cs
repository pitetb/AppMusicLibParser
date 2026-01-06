using System.ComponentModel;
using MusicParser.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

public class SearchCommand : Command<SearchCommand.Settings>
{
    private readonly IMusicLibraryService _musicService;

    public SearchCommand(IMusicLibraryService musicService)
    {
        _musicService = musicService;
    }

    public class Settings : CommandSettings
    {
        [Description("Chemin vers le fichier Library.musicdb")]
        [CommandArgument(0, "[libraryPath]")]
        public string? LibraryPath { get; set; }

        [Description("Titre de la piste à rechercher")]
        [CommandArgument(1, "<title>")]
        public string Title { get; set; } = "";
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

            var library = _musicService.ParseLibrary(libraryPath);

            var tracks = library.Tracks
                .Where(t => t.Title != null && t.Title.Contains(settings.Title, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!tracks.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]Aucune piste trouvée avec le titre contenant '{settings.Title}'[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]✓ {tracks.Count} piste(s) trouvée(s)[/]");
            AnsiConsole.WriteLine();

            foreach (var track in tracks)
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Blue);

                table.AddColumn("[bold]Propriété[/]");
                table.AddColumn("[bold]Valeur[/]");

                table.AddRow("ID", $"{track.TrackId}");
                table.AddRow("Titre", track.Title ?? "[dim]N/A[/]");
                table.AddRow("Artiste", track.Artist ?? "[dim]N/A[/]");
                table.AddRow("Album", track.Album ?? "[dim]N/A[/]");
                
                if (track.LikeStatus.HasValue)
                {
                    var statusText = track.LikeStatus.Value switch
                    {
                        Models.LikeStatus.Neutral => "⚪ Neutre",
                        Models.LikeStatus.Liked => "[green]❤️  J'aime[/]",
                        Models.LikeStatus.Disliked => "[orange1]💔 Je n'aime plus[/]",
                        Models.LikeStatus.DislikedExplicit => "[red]👎 J'aime pas (explicite)[/]",
                        _ => track.LikeStatus.Value.ToString()
                    };
                    table.AddRow("LikeStatus", $"{statusText} ([dim]{(int)track.LikeStatus.Value}[/])");
                }
                else
                {
                    table.AddRow("LikeStatus", "[dim]N/A[/]");
                }

                if (track.Rating.HasValue)
                {
                    var stars = track.Rating.Value / 20; // Rating sur 100 -> étoiles sur 5
                    var starDisplay = new string('⭐', stars);
                    var color = stars switch
                    {
                        5 => "green",
                        4 => "blue",
                        3 => "yellow",
                        2 => "orange1",
                        _ => "red"
                    };
                    table.AddRow("Rating", $"[{color}]{starDisplay}[/] ([dim]{track.Rating}/100[/])");
                }
                if (track.PlayCount.HasValue)
                    table.AddRow("Play Count", $"{track.PlayCount:N0}");
                if (track.Duration.HasValue)
                    table.AddRow("Duration", TimeSpan.FromMilliseconds(track.Duration.Value).ToString(@"mm\:ss"));

                AnsiConsole.Write(table);
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
