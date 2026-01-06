using System.ComponentModel;
using MusicParser.Crypto;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

public class DumpOffsetCommand : Command<DumpOffsetCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Chemin vers le fichier Library.musicdb")]
        [CommandArgument(0, "<libraryPath>")]
        public string LibraryPath { get; set; } = "";

        [Description("Offset hexadécimal (ex: 0x2214)")]
        [CommandArgument(1, "<offset>")]
        public string Offset { get; set; } = "";
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var offset = Convert.ToInt32(settings.Offset.Replace("0x", ""), 16);

            // Déchiffrer
            AnsiConsole.MarkupLine($"[cyan]Déchiffrement de {settings.LibraryPath}...[/]");
            var (_, _, data) = MusicDbDecryptor.DecryptAndDecompressFile(settings.LibraryPath);

            // Afficher contexte autour de l'offset  
            var start = Math.Max(0, offset - 64);
            var end = Math.Min(data.Length, offset + 64);

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine($"Offset 0x{offset:X} dans {settings.LibraryPath}");
            AnsiConsole.WriteLine($"Contexte [0x{start:X}-0x{end:X}]");
            AnsiConsole.WriteLine();

            for (int i = start; i < end; i += 16)
            {
                var lineSize = Math.Min(16, end - i);
                var line = data.Skip(i).Take(lineSize).ToArray();
                var hexStr = string.Join(" ", line.Select(b => $"{b:X2}"));
                var asciiStr = string.Join("", line.Select(b => b >= 32 && b < 127 ? (char)b : '·'));
                
                // Marquer le byte exact
                if (i <= offset && offset < i + lineSize)
                {
                    var bytePos = offset - i;
                    var before = string.Join(" ", line.Take(bytePos).Select(b => $"{b:X2}"));
                    var target = $"[{line[bytePos]:X2}]";
                    var after = string.Join(" ", line.Skip(bytePos + 1).Select(b => $"{b:X2}"));
                    AnsiConsole.MarkupLine($"[green]>>> {i:X4}:[/] {before.EscapeMarkup()} [red]{target.EscapeMarkup()}[/] {after.EscapeMarkup(),-40} {asciiStr.EscapeMarkup()}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"    {i:X4}: {hexStr.EscapeMarkup(),-48} {asciiStr.EscapeMarkup()}");
                }
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
