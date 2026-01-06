using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Microsoft.Extensions.Logging;
using MusicParser.Crypto;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MusicParser.App.Commands;

public class CompareCommand : Command<CompareCommand.Settings>
{
    private readonly ILogger<CompareCommand> _logger;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file1>")]
        public string File1 { get; set; } = string.Empty;
        
        [CommandArgument(1, "<file2>")]
        public string File2 { get; set; } = string.Empty;
    }

    public CompareCommand(ILogger<CompareCommand> logger)
    {
        _logger = logger;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.MarkupLine($"[yellow]Lecture et déchiffrement des fichiers...[/]");
            
            var data1 = ReadAndDecrypt(settings.File1);
            var data2 = ReadAndDecrypt(settings.File2);
            
            AnsiConsole.MarkupLine($"\n[green]✓[/] Fichier 1: {data1.Length:N0} bytes déchiffrés");
            AnsiConsole.MarkupLine($"[green]✓[/] Fichier 2: {data2.Length:N0} bytes déchiffrés");
            
            // Comparer byte par byte
            var differences = new List<(int Offset, byte Byte1, byte Byte2)>();
            var minLength = Math.Min(data1.Length, data2.Length);
            
            for (int i = 0; i < minLength; i++)
            {
                if (data1[i] != data2[i])
                {
                    differences.Add((i, data1[i], data2[i]));
                }
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[cyan]Nombre de bytes différents:[/] {differences.Count:N0}");
            
            if (data1.Length != data2.Length)
            {
                AnsiConsole.MarkupLine($"[yellow]Différence de taille:[/] {Math.Abs(data1.Length - data2.Length)} bytes");
            }
            
            if (differences.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]Les fichiers sont identiques ![/]");
                return 0;
            }
            
            // Afficher les différences
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[yellow]Différences (100 premières)[/]"));
            AnsiConsole.WriteLine();
            
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Offset")
                .AddColumn("File1")
                .AddColumn("File2")
                .AddColumn("Contexte File1")
                .AddColumn("Contexte File2");
            
            foreach (var (offset, byte1, byte2) in differences.Take(100))
            {
                // Contexte: 16 bytes avant et après
                var ctx1 = GetContext(data1, offset, 16);
                var ctx2 = GetContext(data2, offset, 16);
                
                table.AddRow(
                    $"0x{offset:X8}",
                    $"0x{byte1:X2} ({byte1})",
                    $"0x{byte2:X2} ({byte2})",
                    ctx1.EscapeMarkup(),
                    ctx2.EscapeMarkup()
                );
            }
            
            AnsiConsole.Write(table);
            
            // Chercher des patterns
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[yellow]Analyse des patterns[/]"));
            AnsiConsole.WriteLine();
            
            AnalyzePatterns(differences, data1, data2);
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erreur: {Markup.Escape(ex.Message)}[/]");
            _logger.LogError(ex, "Erreur lors de la comparaison");
            return 1;
        }
    }
    
    private byte[] ReadAndDecrypt(string filePath)
    {
        // Utiliser MusicDbDecryptor pour déchiffrer et décompresser
        var (_, _, decompressedData) = MusicDbDecryptor.DecryptAndDecompressFile(filePath);
        return decompressedData;
    }
    
    private string GetContext(byte[] data, int offset, int contextSize)
    {
        var start = Math.Max(0, offset - contextSize);
        var end = Math.Min(data.Length, offset + contextSize + 1);
        var length = end - start;
        
        var bytes = new byte[length];
        Array.Copy(data, start, bytes, 0, length);
        
        // Marquer la position actuelle avec des crochets
        var hex = new System.Text.StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (start + i == offset)
                hex.Append($"[{bytes[i]:X2}]");
            else
                hex.Append($"{bytes[i]:X2} ");
        }
        
        return hex.ToString().Trim();
    }
    
    private void AnalyzePatterns(List<(int Offset, byte Byte1, byte Byte2)> differences, byte[] data1, byte[] data2)
    {
        // Chercher des plages consécutives
        var ranges = new List<(int Start, int End, int Count)>();
        int rangeStart = -1;
        int rangeEnd = -1;
        
        for (int i = 0; i < differences.Count; i++)
        {
            var offset = differences[i].Offset;
            
            if (rangeStart == -1)
            {
                rangeStart = offset;
                rangeEnd = offset;
            }
            else if (offset == rangeEnd + 1)
            {
                rangeEnd = offset;
            }
            else
            {
                if (rangeEnd - rangeStart >= 4) // Plages de 5+ bytes
                {
                    ranges.Add((rangeStart, rangeEnd, rangeEnd - rangeStart + 1));
                }
                rangeStart = offset;
                rangeEnd = offset;
            }
        }
        
        if (rangeStart != -1 && rangeEnd - rangeStart >= 4)
        {
            ranges.Add((rangeStart, rangeEnd, rangeEnd - rangeStart + 1));
        }
        
        if (ranges.Count > 0)
        {
            AnsiConsole.MarkupLine($"[cyan]Plages consécutives de différences (5+ bytes):[/]");
            foreach (var (start, end, count) in ranges.Take(20))
            {
                AnsiConsole.MarkupLine($"  0x{start:X8} - 0x{end:X8} ({count} bytes)");
            }
        }
        
        // Chercher des valeurs spécifiques qui changent
        var singleByteChanges = differences.Where(d => 
        {
            var prevSame = d.Offset == 0 || data1[d.Offset - 1] == data2[d.Offset - 1];
            var nextSame = d.Offset == data1.Length - 1 || data1[d.Offset + 1] == data2[d.Offset + 1];
            return prevSame && nextSame;
        }).ToList();
        
        if (singleByteChanges.Count > 0 && singleByteChanges.Count < 50)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[cyan]Changements de bytes isolés ({singleByteChanges.Count}):[/]");
            foreach (var (offset, byte1, byte2) in singleByteChanges.Take(20))
            {
                AnsiConsole.MarkupLine($"  0x{offset:X8}: 0x{byte1:X2} → 0x{byte2:X2} (décimal: {byte1} → {byte2})");
            }
        }
    }
}
