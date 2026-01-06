# MusicParser

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

> 🇬🇧 **English version**: [README_EN.md](README_EN.md)

Une bibliothèque .NET 8 et application console pour parser les fichiers Apple Music `Library.musicdb`.

MusicParser extrait les métadonnées complètes des pistes, albums, artistes et playlists depuis le format binaire propriétaire d'Apple Music, incluant les statistiques de lecture, les ratings et les chemins de fichiers.

Pourquoi ce projet ? L'application AppleMusic n'exporte plus automatiquement la librairie en XML ce qui ne permet plus de developper facilement des applications exploitant la librairie musicale. Les AppleScripts étant trop lent pour les grosses librairies, il fallait une autre solution...

## ✨ Fonctionnalités

- 🎵 **Parsing complet** : Pistes, albums, artistes, playlists
- 📊 **Statistiques de lecture** : Nombre de lectures, date de dernière écoute
- ⭐ **Ratings** : Récupération des notes (1-5 étoiles)
- 🔗 **Relations** : Albums → Artistes, Pistes → Albums/Artistes
- 📁 **Chemins de fichiers** : Extraction des URLs décodées
- 🔐 **Déchiffrement AES-128 ECB** : Support natif du format chiffré
- 📦 **Décompression zlib** : Extraction automatique des données compressées
- 🎨 **CLI riche** : Interface console colorée avec [Spectre.Console](https://spectreconsole.net/)

## 🏗️ Architecture

Le projet suit une architecture en couches avec séparation entre logique métier et interface :

### MusicParser (Bibliothèque)
Bibliothèque réutilisable contenant toute la logique de parsing :

```
MusicParser/
├── Models/              # Modèles de données
│   ├── MusicTrack.cs
│   ├── Album.cs
│   ├── Artist.cs
│   ├── Playlist.cs
│   └── MusicLibrary.cs
├── Parsers/
│   └── MusicDbParser.cs # Parser principal du format binaire
├── Crypto/
│   └── MusicDbDecryptor.cs # Déchiffrement AES-128 ECB + zlib
├── Services/
│   ├── IMusicLibraryService.cs
│   └── MusicLibraryService.cs
└── ServiceCollectionExtensions.cs
```

**Dépendances :**
- `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.1
- `Microsoft.Extensions.Logging.Abstractions` 10.0.1
- `SharpZipLib` 1.4.2 (décompression zlib)

### MusicParser.App (Application Console)
Application CLI professionnelle avec commandes interactives :

```
MusicParser.App/
├── Commands/
│   ├── InfoCommand.cs         # Informations générales de la bibliothèque
│   ├── StatsCommand.cs        # Statistiques détaillées avec top pistes
│   ├── RatingsCommand.cs      # Distribution des ratings (étoiles)
│   ├── LikesCommand.cs        # Statistiques j'aime/j'aime pas
│   ├── SearchCommand.cs       # Recherche de pistes par titre
│   ├── CompareCommand.cs      # Comparaison byte-à-byte de fichiers
│   └── DumpOffsetCommand.cs   # Dump hexadécimal à un offset (debug)
├── Infrastructure/
│   ├── TypeRegistrar.cs
│   └── TypeResolver.cs
└── Program.cs
```

**Dépendances :**
- `Spectre.Console.Cli` 0.53.1
- `Serilog` 4.3.0 + extensions
- `dotenv.net` 4.0.0

## 🚀 Installation

### Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Clé de déchiffrement AES** : elle n'est pas fournie avec le code pour des raisons légales. Elle peut se retrouver sur internet en cherchant un peu...

### Compilation

```bash
cd MusicParser
dotnet build
```

### Configuration

1. Copiez le fichier `.env.example` vers `.env` :
   ```bash
   cp .env.example .env
   ```

2. Éditez `.env` et ajoutez la clé AES de déchiffrement de 16 caractères :
   ```env
   MUSICDB_AES_KEY=ABCDEFGHUILDFK
   ```
   
   > **Note** : La clé par est utilisée par Apple Music pour chiffrer les fichiers `Library.musicdb`.

## 📖 Utilisation

### Application Console

#### Commande `info`
Affiche les informations générales de la bibliothèque :

```bash
dotnet run --project MusicParser.App -- info
dotnet run --project MusicParser.App -- info /path/to/Library.musicdb
```

**Exemple de sortie :**
```
╭─📚 Bibliothèque Apple Music──────────────────╮
│ Fichier: Library.musicdb                     │
│ Taille: 7,113,718 octets (6.78 MB)           │
│ Dernière modification: 2026-01-04 11:30:50   │
│                                              │
│ 📀 Pistes: 13,162                            │
│ 💿 Albums: 1,017                             │
│ 🎤 Artistes: 649                             │
│ 📋 Playlists: 41                             │
╰──────────────────────────────────────────────╯
```

#### Commande `stats`
Affiche les statistiques détaillées avec les pistes les plus écoutées :

```bash
dotnet run --project MusicParser.App -- stats
dotnet run --project MusicParser.App -- stats --top 10
dotnet run --project MusicParser.App -- stats /path/to/Library.musicdb --top 20
```

**Options :**
- `--top <COUNT>` : Nombre de pistes les plus écoutées à afficher (défaut: 5)

**Exemple de sortie :**
```
╭───────────────────────┬────────╮
│ Catégorie             │ Valeur │
├───────────────────────┼────────┤
│ Pistes totales        │ 13,162 │
│ Albums                │  1,017 │
│ Artistes              │    649 │
│ Playlists             │     41 │
│                       │        │
│ Pistes avec lecture   │  8,234 │
│ Lectures totales      │ 45,678 │
│ Moyenne lectures/piste│    5.5 │
╰───────────────────────┴────────╯

🎵 Top 5 pistes les plus écoutées
╭───┬────────────────────┬─────────────┬─────────╮
│ # │ Titre              │ Artiste     │ Lectures│
├───┼────────────────────┼─────────────┼─────────┤
│ 1 │ Song Title         │ Artist Name │     123 │
│ ...│                    │             │         │
╰───┴────────────────────┴─────────────┴─────────╯
```

#### Commande `ratings`
Affiche la distribution des ratings et des exemples de pistes par niveau d'étoiles :

```bash
dotnet run --project MusicParser.App -- ratings
dotnet run --project MusicParser.App -- ratings --count 20
```

**Options :**
- `--count <COUNT>` : Nombre de pistes à afficher par niveau d'étoiles (défaut: 10)

**Exemple de sortie :**
```
⭐ Distribution des ratings

╭───────────────┬────────┬─────────────┬──────────╮
│ Rating        │ Pistes │ Pourcentage │ Barre    │
├───────────────┼────────┼─────────────┼──────────┤
│ ⭐⭐⭐⭐⭐   │  1,234 │      15.2%  │ ████████ │
│ ⭐⭐⭐⭐     │  2,456 │      30.3%  │ ████████ │
│ ⭐⭐⭐       │  3,123 │      38.5%  │ ████████ │
│ ⭐⭐         │    987 │      12.2%  │ ████     │
│ ⭐           │    312 │       3.8%  │ █        │
╰───────────────┴────────┴─────────────┴──────────╯

⭐⭐⭐⭐⭐ Exemples (10/1,234)
╭───┬─────────────────┬───────────────┬──────────╮
│ # │ Titre           │ Artiste       │ Album    │
├───┼─────────────────┼───────────────┼──────────┤
│ 1 │ Great Song      │ Awesome Band  │ Album X  │
│ ...│                 │               │          │
╰───┴─────────────────┴───────────────┴──────────╯
```

#### Commande `likes`
Affiche les statistiques des j'aime/j'aime pas avec distinction entre les différents états :

```bash
dotnet run --project MusicParser.App -- likes
dotnet run --project MusicParser.App -- likes --examples 20
```

**Options :**
- `--examples <COUNT>` : Nombre d'exemples de pistes à afficher par catégorie (défaut: 10)

**États supportés :**
- ❤️ **J'aime** (valeur 2) : Pistes marquées comme aimées
- 💔 **Je n'aime plus** (valeur 1) : État transitoire après retrait d'un like
- 👎 **J'aime pas explicite** (valeur 3) : Dislike actif
- ⚪ **Neutre** (valeur 0) : Par défaut, pas d'avis

#### Commande `search`
Recherche une piste par titre et affiche ses métadonnées complètes :

```bash
dotnet run --project MusicParser.App -- search "nom de la piste"
dotnet run --project MusicParser.App -- search /path/to/Library.musicdb "nom de la piste"
```

**Affichage :**
- Métadonnées complètes (ID, titre, artiste, album)
- Rating avec affichage d'étoiles
- LikeStatus avec emoji et valeur numérique
- Statistiques de lecture
- Chemin du fichier audio

#### Commande `compare`
Compare deux fichiers MusicDB déchiffrés byte par byte, utile pour analyser les différences de format :

```bash
dotnet run --project MusicParser.App -- compare file1.musicdb file2.musicdb
```

**Utilisation :**
- Analyse les différences entre deux versions d'une bibliothèque
- Affiche les offsets où les bytes diffèrent
- Montre le contexte autour des différences
- Utile pour le reverse engineering du format

#### Commande `dump-offset`
Dump le contenu déchiffré à un offset spécifique (outil de debug) :

```bash
dotnet run --project MusicParser.App -- dump-offset /path/to/Library.musicdb 0x2214
```

**Utilisation :**
- Affiche un contexte de 128 bytes autour de l'offset spécifié
- Format hexadécimal + ASCII
- Met en évidence le byte exact à l'offset donné
- Essentiel pour débugger le parsing et analyser le format binaire

### Utilisation de la bibliothèque dans votre code

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicParser;
using MusicParser.Services;

// Configuration avec DI
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddMusicParser();
    })
    .Build();

// Utilisation du service
var musicService = host.Services.GetRequiredService<IMusicLibraryService>();
var library = musicService.ParseLibrary("/path/to/Library.musicdb");

// Accès aux données
Console.WriteLine($"Pistes: {library.Tracks.Count}");
Console.WriteLine($"Albums: {library.Albums.Count}");
Console.WriteLine($"Artistes: {library.Artists.Count}");

// Filtrer les pistes par artiste
var beatlesTracks = library.Tracks
    .Where(t => t.Artist?.Contains("Beatles", StringComparison.OrdinalIgnoreCase) == true)
    .ToList();

// Trouver les pistes les plus écoutées
var topTracks = library.Tracks
    .Where(t => t.PlayCount.HasValue)
    .OrderByDescending(t => t.PlayCount)
    .Take(10)
    .ToList();
```

## 📄 Format MusicDB

Le format du fichier `Library.musicdb` est un format binaire propriétaire utilisé par Apple Music (anciennement iTunes). Il est composé de :

1. **Header non chiffré** (hfma) contenant les métadonnées
2. **Payload chiffré** avec AES-128 ECB
3. **Données compressées** avec zlib

Le format est partiellement documenté :
- [Documentation principale](https://www.home.vollink.com/gary/playlister/musicdb.html)
- [Code PERL de référence](https://gitlab.home.vollink.com/external/musicdb-poc)
- [Parser iTunes ITL](https://github.com/jeanthom/libitlp)

Des documentations détaillées sont disponibles dans le dossier [`docs/`](docs/) :
- [MUSICDB_FORMAT_FR.md](docs/MUSICDB_FORMAT_FR.md) - Documentation en français
- [MUSICDB_FORMAT_EN.md](docs/MUSICDB_FORMAT_EN.md) - Documentation en anglais

### Sections parsées

Le parser supporte les sections suivantes :

- **ltma** : Pistes (itma)
  - Métadonnées : titre, artiste, album, durée, genre, année...
  - Statistiques : play count, last played, date added...
  - Ratings : notes sur 5 étoiles
  - Références : liens vers album/artiste
  
- **ltka** : Albums (itka)
  - Titre, artiste(s), nombre de pistes
  - Références vers artistes
  
- **ltra** : Artistes (itra)
  - Nom de l'artiste
  
- **ltpa** : Playlists (itpa)
  - Nom, type (normale/smart/folder)
  - Hiérarchie parent/enfant
  - Liste des pistes

## 🧪 Tests

```bash
dotnet test
```

## 🔍 Localisation du fichier Library.musicdb

Le fichier `Library.musicdb` se trouve généralement à :

- **macOS** : `~/Music/Music/Music Library.musiclibrary/Library.musicdb`
- **Windows** : `%USERPROFILE%\Music\iTunes\iTunes Library.musiclibrary\Library.musicdb`

> **Note** : Le chemin exact peut varier selon votre configuration et version d'Apple Music.

## 🛠️ Développement

### Structure du projet

```
MusicParser/
├── .github/
│   └── copilot-instructions.md
├── docs/                       # Documentation du format
├── libraries-music-samples/    # Fichiers de test
├── MusicParser/                # Bibliothèque principale
├── MusicParser.App/            # Application console
├── MusicParser.Tests/          # Tests unitaires (à venir)
├── .env.example                # Template de configuration
├── .gitignore
├── LICENSE                     # Licence GPLv3
├── README.md                   # Documentation (Français)
└── README_EN.md                # Documentation (English)
```

### Contribuer

Les contributions sont les bienvenues ! N'hésitez pas à :
1. Fork le projet
2. Créer une branche (`git checkout -b feature/amazing-feature`)
3. Commit vos changements (`git commit -m 'Add amazing feature'`)
4. Push vers la branche (`git push origin feature/amazing-feature`)
5. Ouvrir une Pull Request

## 📝 Logging

La bibliothèque utilise `ILogger<T>` de `Microsoft.Extensions.Logging` pour un logging flexible.

Tous les logs internes (parsing, déchiffrement) sont au niveau `Debug` pour une sortie console propre.

L'application console utilise Serilog avec configuration par défaut au niveau `Information`.

## ⚠️ Limitations connues

- Le parsing est read-only (pas d'écriture dans Library.musicdb)
- Certains champs binaires ne sont pas encore décodés
- Les playlists smart (requêtes) ne sont pas interprétées
- Testé principalement sur macOS avec Apple Music (versions récentes)

## 🙏 Crédits

- **Documentation du format** : [Gary Vollink](https://www.home.vollink.com/gary/playlister/musicdb.html)
- **Référence iTunes ITL** : [jeanthom/libitlp](https://github.com/jeanthom/libitlp)

## 📜 Licence

Ce projet est sous licence GNU General Public License v3.0 - voir le fichier [LICENSE](LICENSE) pour plus de détails.

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

## 🔗 Liens utiles

- [Documentation .NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Spectre.Console](https://spectreconsole.net/)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib)

---

Made with ❤️ for Apple Music lovers and reverse engineering enthusiasts
