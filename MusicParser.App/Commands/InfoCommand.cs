using System.ComponentModel;
using MusicParser.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

/// <summary>
/// Commande pour afficher les informations générales de la bibliothèque
/// </summary>
public class InfoCommand : Command<InfoCommand.Settings>
{
    private readonly IMusicLibraryService _musicService;

    public InfoCommand(IMusicLibraryService musicService)
    {
        _musicService = musicService;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[FILE]")]
        [Description("Chemin vers le fichier Library.musicdb")]
        public string? FilePath { get; set; }
    }

    private static void AddPlaylistNode(IHasTreeNodes parent, MusicParser.Models.Playlist playlist, 
        Dictionary<ulong, MusicParser.Models.Playlist> playlistsById, List<MusicParser.Models.Playlist> allPlaylists)
    {
        var icon = playlist.Type switch
        {
            MusicParser.Models.PlaylistType.Smart => "🧠",
            MusicParser.Models.PlaylistType.Folder => "📁",
            MusicParser.Models.PlaylistType.System => "⚙️",
            _ => "🎵"
        };

        var typeTag = playlist.Type switch
        {
            MusicParser.Models.PlaylistType.Smart => "[cyan](Smart)[/]",
            MusicParser.Models.PlaylistType.Folder => "[yellow](Dossier)[/]",
            MusicParser.Models.PlaylistType.System => "[dim](Système)[/]",
            _ => ""
        };

        var smartIndicator = playlist.HasSmartCriteria ? " [cyan]★[/]" : "";
        var trackInfo = playlist.Type != MusicParser.Models.PlaylistType.Folder 
            ? $" [dim]({playlist.TrackCount} pistes)[/]" 
            : "";

        var nodeName = $"{icon} {playlist.Name ?? "Sans nom"}{smartIndicator} {typeTag}{trackInfo}";
        var node = parent.AddNode(nodeName);

        // Ajouter les enfants
        var children = allPlaylists
            .Where(p => p.ParentId == playlist.PlaylistId)
            .OrderBy(p => p.Name)
            .ToList();

        foreach (var child in children)
        {
            AddPlaylistNode(node, child, playlistsById, allPlaylists);
        }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Déterminer le chemin du fichier
            var libraryPath = settings.FilePath 
                ?? "libraries-music-samples/Monterey/SmallerLibrary.musicdb";

            // Vérifier que le fichier existe
            if (!File.Exists(libraryPath))
            {
                AnsiConsole.MarkupLine("[red]❌ Erreur: Le fichier n'existe pas:[/]");
                AnsiConsole.WriteLine($"  {libraryPath}");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Usage: musicparser info <FILE>[/]");
                return 1;
            }

            var fileInfo = new FileInfo(libraryPath);

            // Parser la bibliothèque
            var library = AnsiConsole.Status()
                .Start("Lecture de la bibliothèque...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    return _musicService.ParseLibrary(libraryPath);
                });

            // Calculer les statistiques de playlists
            var smartPlaylists = library.Playlists.Count(p => p.Type == MusicParser.Models.PlaylistType.Smart || p.HasSmartCriteria);
            var manualPlaylists = library.Playlists.Count(p => p.Type == MusicParser.Models.PlaylistType.Manual);
            var folders = library.Playlists.Count(p => p.Type == MusicParser.Models.PlaylistType.Folder);
            var rootPlaylists = library.Playlists.Count(p => p.ParentId == 0 || p.ParentId == 5); // Root ou sous Master

            // Afficher les informations du header
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold cyan]📋 En-tête MusicDB[/]"));
            AnsiConsole.WriteLine();

            var headerTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .AddColumn(new TableColumn("[bold]Propriété[/]"))
                .AddColumn(new TableColumn("[bold]Valeur[/]"));

            headerTable.AddRow("Library ID", $"[dim]{library.LibraryId:X16}[/]");
            headerTable.AddRow("Version", $"[yellow]{library.VersionString}[/] [dim](format {library.MajorVersion}.{library.MinorVersion})[/]");
            headerTable.AddRow("Type de fichier", $"[dim]{library.FileType}[/]");
            headerTable.AddRow("Taille envelope", $"[dim]{library.EnvelopeLength:N0} octets[/]");
            headerTable.AddRow("Taille fichier (header)", $"[dim]{library.FileSize:N0} octets[/]");
            headerTable.AddRow("Max crypt size", $"[dim]{library.MaxCryptSize:N0} octets[/]");
            headerTable.AddEmptyRow();
            headerTable.AddRow("Pistes (header)", $"[green]{library.HeaderTrackCount:N0}[/]");
            headerTable.AddRow("Albums (header)", $"[green]{library.HeaderAlbumCount:N0}[/]");
            headerTable.AddRow("Artistes (header)", $"[green]{library.HeaderArtistCount:N0}[/]");
            headerTable.AddRow("Playlists (header)", $"[green]{library.HeaderPlaylistCount:N0}[/]");

            AnsiConsole.Write(headerTable);
            AnsiConsole.WriteLine();

            // Afficher les informations dans un panel
            var panel = new Panel(
                new Rows(
                    new Markup($"[bold]Fichier:[/] {libraryPath}"),
                    new Markup($"[bold]Taille:[/] {fileInfo.Length:N0} octets ({fileInfo.Length / 1024.0 / 1024.0:F2} MB)"),
                    new Markup($"[bold]Dernière modification:[/] {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}"),
                    Text.Empty,
                    new Markup($"[cyan bold]📀 Pistes:[/] [green]{library.Tracks.Count:N0}[/]"),
                    new Markup($"[cyan bold]💿 Albums:[/] [green]{library.Albums.Count:N0}[/]"),
                    new Markup($"[cyan bold]🎤 Artistes:[/] [green]{library.Artists.Count:N0}[/]"),
                    new Markup($"[cyan bold]📋 Playlists:[/] [green]{library.Playlists.Count:N0}[/]"),
                    new Markup($"    [dim]├─ Smart: {smartPlaylists}[/]"),
                    new Markup($"    [dim]├─ Manuelles: {manualPlaylists}[/]"),
                    new Markup($"    [dim]├─ Dossiers: {folders}[/]"),
                    new Markup($"    [dim]└─ Racine: {rootPlaylists}[/]")
                ))
                .Header("[bold blue]📚 Bibliothèque Apple Music[/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(1, 1);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Afficher l'arborescence des playlists
            if (library.Playlists.Any())
            {
                AnsiConsole.Write(new Rule("[bold yellow]📋 Arborescence des Playlists[/]"));
                AnsiConsole.WriteLine();

                var tree = new Tree("🎵 [bold]Playlists[/]");
                
                // Construire l'arbre hiérarchique
                var playlistsById = library.Playlists.ToDictionary(p => p.PlaylistId);
                var rootItems = library.Playlists
                    .Where(p => p.ParentId == 0 || p.ParentId == 5 || !playlistsById.ContainsKey(p.ParentId))
                    .OrderBy(p => p.Name)
                    .ToList();

                foreach (var playlist in rootItems)
                {
                    AddPlaylistNode(tree, playlist, playlistsById, library.Playlists);
                }

                AnsiConsole.Write(tree);
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
