# MusicParser Project

Une bibliothèque .NET 8 et application console pour parser les fichiers Apple Music `Library.musicdb`.

Le projet parse le format binaire MusicDB pour extraire les métadonnées de pistes, albums, artistes et playlists avec leurs statistiques de lecture.

## Architecture

Le projet suit une architecture en couches avec séparation entre logique métier et interface :

- **MusicParser** - Bibliothèque réutilisable contenant toute la logique de parsing
- **MusicParser.App** - Application console CLI utilisant la bibliothèque
- **MusicParser.Tests** - Tests unitaires xUnit

## Project Structure

### MusicParser (Library)
```
MusicParser/
├── Models/              # Modèles de données (Track, Album, Artist, Playlist, MusicLibrary)
├── Parsers/             # MusicDbParser - Parser principal du format binaire
├── Crypto/              # MusicDbDecryptor - Déchiffrement AES-128 ECB
├── Services/            # IMusicLibraryService - Interface du service principal
│   ├── IMusicLibraryService.cs
│   ├── MusicLibraryService.cs
│   └── LibraryStatistics.cs
└── ServiceCollectionExtensions.cs  # Extensions DI (AddMusicParser)
```

**Packages:**
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Logging.Abstractions  
- SharpZipLib (décompression zlib)

### MusicParser.App (Console Application)
```
MusicParser.App/
└── Program.cs           # Application console avec Serilog et DI
```

**Packages:**
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Hosting
- Serilog + Serilog.Extensions.Hosting + Serilog.Sinks.Console
- Référence projet MusicParser

### Documentation & Samples
```
docs/                              # Documentation du format
├── MUSICDB_FORMAT_EN.md          # Format MusicDB (English)
└── MUSICDB_FORMAT_FR.md          # Format MusicDB (Français)

libraries-music-samples/           # Fichiers d'exemple
└── Library.musicdb               # Fichier binaire de test
```

## MusicDB Format

Le format du fichier est principalement décrit ici : https://www.home.vollink.com/gary/playlister/musicdb.html et dont un code PERL est disponible ici : https://gitlab.home.vollink.com/external/musicdb-poc

La description est partielle et a été complétée dans les fichiers `docs/MUSICDB_FORMAT_*.md`.

Le format reprend tout ou partie du format iTunes ITL dont un parser existe ici :
https://github.com/jeanthom/libitlp

Il existe aussi en ressource ce projet : https://github.com/rinsuki/musicdb2sqlite qui implémente un parser en Python.


## Usage

### Utilisation de la bibliothèque

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicParser;
using MusicParser.Services;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddMusicParser();
    })
    .Build();

var musicService = host.Services.GetRequiredService<IMusicLibraryService>();
var library = musicService.ParseLibrary("path/to/Library.musicdb");
var stats = musicService.GetStatistics(library);

Console.WriteLine($"Pistes: {stats.TotalTracks}, Albums: {stats.TotalAlbums}");
```

### Application console

```bash
# Utiliser le fichier par défaut (libraries-music-samples/Library.musicdb)
dotnet run --project MusicParser.App

# Spécifier un fichier
dotnet run --project MusicParser.App /path/to/Library.musicdb
```

## Build & Run

```bash
# Compiler toute la solution
dotnet build

# Compiler uniquement la bibliothèque
dotnet build MusicParser/MusicParser.csproj

# Exécuter l'application console
dotnet run --project MusicParser.App

# Exécuter les tests
dotnet test
```

## Fonctionnalités parsées

- ✅ Pistes (13,162 dans l'exemple) - Titre, artiste, album, durée, etc.
- ✅ Albums (1,017) - Titre, artiste, nombre de pistes
- ✅ Artistes (649) - Nom, références aux albums
- ✅ Playlists (41) - Nom, type (normale/smart/folder), hiérarchie
- ✅ Statistiques de lecture - Play count, last played
- ✅ Chemins de fichiers - URLs décodées
- ✅ Ratings - Notation sur 5 étoiles (offset 65 de l'itma)
- ✅ LikeStatus - J'aime/J'aime pas (offset 62 de l'itma, 4 valeurs: 0=neutre, 1=je n'aime plus, 2=j'aime, 3=j'aime pas explicite)
- ✅ Album→Artist references - Liste ArtistRefs dans Album
- ✅ Movement Count/Number - Support de la musique classique

## Commandes disponibles

### info
Affiche les informations générales de la bibliothèque (version, nombre de pistes, albums, etc.)

```bash
dotnet run --project MusicParser.App -- info [libraryPath]
```

### stats
Affiche les statistiques détaillées (top pistes, lectures totales, moyennes)

```bash
dotnet run --project MusicParser.App -- stats [libraryPath] [--top N]
```

### ratings
Affiche la distribution des ratings avec des exemples de pistes par niveau

```bash
dotnet run --project MusicParser.App -- ratings [libraryPath] [--count N]
```

### likes
Affiche les statistiques des j'aime/j'aime pas avec distinction entre :
- ❤️ J'aime (valeur 2)
- 💔 Je n'aime plus (valeur 1 - état transitoire après retrait d'un like)
- 👎 J'aime pas explicite (valeur 3 - dislike actif)
- ⚪ Neutre (valeur 0 - par défaut)

```bash
dotnet run --project MusicParser.App -- likes [libraryPath] [--examples N]
```

### search
Recherche une piste par titre et affiche ses métadonnées complètes (rating avec étoiles, LikeStatus, etc.)

```bash
dotnet run --project MusicParser.App -- search [libraryPath] <titre>
```

### compare
Compare deux fichiers MusicDB déchiffrés byte par byte, utile pour analyser les différences de format

```bash
dotnet run --project MusicParser.App -- compare <file1> <file2>
```

### dump-offset
Dump le contenu déchiffré à un offset spécifique (outil de debug)

```bash
dotnet run --project MusicParser.App -- dump-offset <file> <offset_hex>
```

## Logging

La bibliothèque utilise `ILogger<T>` de Microsoft.Extensions.Logging pour un logging flexible.
L'application console utilise Serilog avec sortie console formatée.

Niveaux de log utilisés :
- `LogInformation` - Informations importantes (headers, résumés)
- `LogDebug` - Détails de progression (ltma sections)
- `LogWarning` - Erreurs non-fatales lors du parsing
- `LogError` - Erreurs fatales
