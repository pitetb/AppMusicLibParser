# MusicParser

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

> 🇫🇷 **Version française** : [README.md](README.md)

A .NET 8 library and console application to parse Apple Music `Library.musicdb` files.

MusicParser extracts complete metadata from tracks, albums, artists and playlists from Apple Music's proprietary binary format, including playback statistics, ratings and file paths.

Why this project? The Apple Music application no longer automatically exports the library to XML, making it difficult to develop applications that leverage the music library. AppleScripts being too slow for large libraries, another solution was needed...

## ✨ Features

- 🎵 **Complete parsing**: Tracks, albums, artists, playlists
- 📊 **Playback statistics**: Play count, last played date
- ⭐ **Ratings**: Extract star ratings (1-5 stars)
- 🔗 **Relationships**: Albums → Artists, Tracks → Albums/Artists
- 📁 **File paths**: Decoded URL extraction
- 🔐 **AES-128 ECB decryption**: Native support for encrypted format
- 📦 **zlib decompression**: Automatic extraction of compressed data
- 🎨 **Rich CLI**: Colorful console interface with [Spectre.Console](https://spectreconsole.net/)

## 🏗️ Architecture

The project follows a layered architecture with separation between business logic and interface:

### MusicParser (Library)
Reusable library containing all parsing logic:

```
MusicParser/
├── Models/              # Data models
│   ├── MusicTrack.cs
│   ├── Album.cs
│   ├── Artist.cs
│   ├── Playlist.cs
│   └── MusicLibrary.cs
├── Parsers/
│   └── MusicDbParser.cs # Main binary format parser
├── Crypto/
│   └── MusicDbDecryptor.cs # AES-128 ECB decryption + zlib
├── Services/
│   ├── IMusicLibraryService.cs
│   └── MusicLibraryService.cs
└── ServiceCollectionExtensions.cs
```

**Dependencies:**
- `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.1
- `Microsoft.Extensions.Logging.Abstractions` 10.0.1
- `SharpZipLib` 1.4.2 (zlib decompression)

### MusicParser.App (Console Application)
Professional CLI application with interactive commands:

```
MusicParser.App/
├── Commands/
│   ├── InfoCommand.cs         # Library general information
│   ├── StatsCommand.cs        # Detailed statistics with top tracks
│   ├── RatingsCommand.cs      # Ratings distribution (stars)
│   ├── LikesCommand.cs        # Like/dislike statistics
│   ├── SearchCommand.cs       # Track search by title
│   ├── CompareCommand.cs      # Byte-by-byte file comparison
│   └── DumpOffsetCommand.cs   # Hexadecimal dump at offset (debug)
├── Infrastructure/
│   ├── TypeRegistrar.cs
│   └── TypeResolver.cs
└── Program.cs
```

**Dependencies:**
- `Spectre.Console.Cli` 0.53.1
- `Serilog` 4.3.0 + extensions
- `dotenv.net` 4.0.0

## 🚀 Installation

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **AES decryption key**: Not provided with the code for legal reasons. It can be found on the internet with some research...

### Build

```bash
cd MusicParser
dotnet build
```

### Configuration

1. Copy the `.env.example` file to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and add the 16-character AES decryption key:
   ```env
   MUSICDB_AES_KEY=ABCDEFGHUILDFK
   ```
   
   > **Note**: This key is used by Apple Music to encrypt `Library.musicdb` files.

## 📖 Usage

### Console Application

#### `info` Command
Display general library information:

```bash
dotnet run --project MusicParser.App -- info
dotnet run --project MusicParser.App -- info /path/to/Library.musicdb
```

**Example output:**
```
╭─📚 Apple Music Library────────────────────────╮
│ File: Library.musicdb                         │
│ Size: 7,113,718 bytes (6.78 MB)               │
│ Last modified: 2026-01-04 11:30:50            │
│                                               │
│ 📀 Tracks: 13,162                             │
│ 💿 Albums: 1,017                              │
│ 🎤 Artists: 649                               │
│ 📋 Playlists: 41                              │
╰───────────────────────────────────────────────╯
```

#### `stats` Command
Display detailed statistics with most played tracks:

```bash
dotnet run --project MusicParser.App -- stats
dotnet run --project MusicParser.App -- stats --top 10
dotnet run --project MusicParser.App -- stats /path/to/Library.musicdb --top 20
```

**Options:**
- `--top <COUNT>`: Number of most played tracks to display (default: 5)

**Example output:**
```
╭───────────────────────┬────────╮
│ Category              │ Value  │
├───────────────────────┼────────┤
│ Total tracks          │ 13,162 │
│ Albums                │  1,017 │
│ Artists               │    649 │
│ Playlists             │     41 │
│                       │        │
│ Tracks with plays     │  8,234 │
│ Total plays           │ 45,678 │
│ Average plays/track   │    5.5 │
╰───────────────────────┴────────╯

🎵 Top 5 most played tracks
╭───┬────────────────────┬─────────────┬─────────╮
│ # │ Title              │ Artist      │ Plays   │
├───┼────────────────────┼─────────────┼─────────┤
│ 1 │ Song Title         │ Artist Name │     123 │
│ ...│                    │             │         │
╰───┴────────────────────┴─────────────┴─────────╯
```

#### `ratings` Command
Display ratings distribution and track examples by star level:

```bash
dotnet run --project MusicParser.App -- ratings
dotnet run --project MusicParser.App -- ratings --count 20
```

**Options:**
- `--count <COUNT>`: Number of tracks to display per star level (default: 10)

**Example output:**
```
⭐ Ratings distribution

╭───────────────┬────────┬─────────────┬──────────╮
│ Rating        │ Tracks │ Percentage  │ Bar      │
├───────────────┼────────┼─────────────┼──────────┤
│ ⭐⭐⭐⭐⭐   │  1,234 │      15.2%  │ ████████ │
│ ⭐⭐⭐⭐     │  2,456 │      30.3%  │ ████████ │
│ ⭐⭐⭐       │  3,123 │      38.5%  │ ████████ │
│ ⭐⭐         │    987 │      12.2%  │ ████     │
│ ⭐           │    312 │       3.8%  │ █        │
╰───────────────┴────────┴─────────────┴──────────╯

⭐⭐⭐⭐⭐ Examples (10/1,234)
╭───┬─────────────────┬───────────────┬──────────╮
│ # │ Title           │ Artist        │ Album    │
├───┼─────────────────┼───────────────┼──────────┤
│ 1 │ Great Song      │ Awesome Band  │ Album X  │
│ ...│                 │               │          │
╰───┴─────────────────┴───────────────┴──────────╯
```

#### `likes` Command
Display like/dislike statistics with distinction between different states:

```bash
dotnet run --project MusicParser.App -- likes
dotnet run --project MusicParser.App -- likes --examples 20
```

**Options:**
- `--examples <COUNT>`: Number of track examples to display per category (default: 10)

**Supported states:**
- ❤️ **Liked** (value 2): Tracks marked as liked
- 💔 **Unliked** (value 1): Transitional state after removing a like
- 👎 **Explicitly disliked** (value 3): Active dislike
- ⚪ **Neutral** (value 0): Default, no opinion

#### `search` Command
Search for a track by title and display complete metadata:

```bash
dotnet run --project MusicParser.App -- search "track name"
dotnet run --project MusicParser.App -- search /path/to/Library.musicdb "track name"
```

**Display:**
- Complete metadata (ID, title, artist, album)
- Rating with star display
- LikeStatus with emoji and numeric value
- Playback statistics
- Audio file path

#### `compare` Command
Compare two decrypted MusicDB files byte by byte, useful for analyzing format differences:

```bash
dotnet run --project MusicParser.App -- compare file1.musicdb file2.musicdb
```

**Usage:**
- Analyze differences between two library versions
- Display offsets where bytes differ
- Show context around differences
- Useful for reverse engineering the format

#### `dump-offset` Command
Dump decrypted content at a specific offset (debug tool):

```bash
dotnet run --project MusicParser.App -- dump-offset /path/to/Library.musicdb 0x2214
```

**Usage:**
- Display 128 bytes context around the specified offset
- Hexadecimal + ASCII format
- Highlight the exact byte at the offset
- Essential for debugging parsing and analyzing binary format

### Using the Library in Your Code

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicParser;
using MusicParser.Services;

// Configuration with DI
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddMusicParser();
    })
    .Build();

// Using the service
var musicService = host.Services.GetRequiredService<IMusicLibraryService>();
var library = musicService.ParseLibrary("/path/to/Library.musicdb");

// Accessing data
Console.WriteLine($"Tracks: {library.Tracks.Count}");
Console.WriteLine($"Albums: {library.Albums.Count}");
Console.WriteLine($"Artists: {library.Artists.Count}");

// Filter tracks by artist
var beatlesTracks = library.Tracks
    .Where(t => t.Artist?.Contains("Beatles", StringComparison.OrdinalIgnoreCase) == true)
    .ToList();

// Find most played tracks
var topTracks = library.Tracks
    .Where(t => t.PlayCount.HasValue)
    .OrderByDescending(t => t.PlayCount)
    .Take(10)
    .ToList();
```

## 📄 MusicDB Format

The `Library.musicdb` file format is a proprietary binary format used by Apple Music (formerly iTunes). It consists of:

1. **Unencrypted header** (hfma) containing metadata
2. **Encrypted payload** with AES-128 ECB
3. **Compressed data** with zlib

The format is partially documented:
- [Main documentation](https://www.home.vollink.com/gary/playlister/musicdb.html)
- [PERL reference code](https://gitlab.home.vollink.com/external/musicdb-poc)
- [iTunes ITL parser](https://github.com/jeanthom/libitlp)

Detailed documentation is available in the [`docs/`](docs/) folder:
- [MUSICDB_FORMAT_FR.md](docs/MUSICDB_FORMAT_FR.md) - French documentation
- [MUSICDB_FORMAT_EN.md](docs/MUSICDB_FORMAT_EN.md) - English documentation

### Parsed Sections

The parser supports the following sections:

- **ltma**: Tracks (itma)
  - Metadata: title, artist, album, duration, genre, year...
  - Statistics: play count, last played, date added...
  - Ratings: star ratings
  - References: links to album/artist
  
- **ltka**: Albums (itka)
  - Title, artist(s), number of tracks
  - Artist references
  
- **ltra**: Artists (itra)
  - Artist name
  
- **ltpa**: Playlists (itpa)
  - Name, type (normal/smart/folder)
  - Parent/child hierarchy
  - Track list

## 🧪 Tests

```bash
dotnet test
```

## 🔍 Locating the Library.musicdb File

The `Library.musicdb` file is typically located at:

- **macOS**: `~/Music/Music/Music Library.musiclibrary/Library.musicdb`
- **Windows**: `%USERPROFILE%\Music\iTunes\iTunes Library.musiclibrary\Library.musicdb`

> **Note**: The exact path may vary depending on your configuration and Apple Music version.

## 🛠️ Development

### Project Structure

```
MusicParser/
├── .github/
│   └── copilot-instructions.md
├── docs/                       # Format documentation
├── libraries-music-samples/    # Test files
├── MusicParser/                # Main library
├── MusicParser.App/            # Console application
├── MusicParser.Tests/          # Unit tests (coming soon)
├── .env.example                # Configuration template
├── .gitignore
├── LICENSE                     # GPLv3 License
└── README.md
```

### Contributing

Contributions are welcome! Feel free to:
1. Fork the project
2. Create a branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 Logging

The library uses `ILogger<T>` from `Microsoft.Extensions.Logging` for flexible logging.

All internal logs (parsing, decryption) are at `Debug` level for clean console output.

The console application uses Serilog with default configuration at `Information` level.

## ⚠️ Known Limitations

- Parsing is read-only (no writing to Library.musicdb)
- Some binary fields are not yet decoded
- Smart playlists (queries) are not interpreted
- Mainly tested on macOS with Apple Music (recent versions)

## 🙏 Credits

- **Format documentation**: [Gary Vollink](https://www.home.vollink.com/gary/playlister/musicdb.html)
- **iTunes ITL reference**: [jeanthom/libitlp](https://github.com/jeanthom/libitlp)

## 📜 License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

```
Copyright (C) 2026 MusicParser Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
```

## 🔗 Useful Links

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Spectre.Console](https://spectreconsole.net/)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)

---

Made with ❤️ for Apple Music lovers and reverse engineering enthusiasts
