# Format du fichier Library.musicdb

## Vue d'ensemble

Le fichier `Library.musicdb` est le format de base de données binaire utilisé par Apple Music sur macOS pour stocker la bibliothèque musicale complète. Ce format contient toutes les métadonnées des pistes, albums, artistes et playlists.

## Caractéristiques principales

- **Chiffrement** : AES-128 ECB
- **Clé de déchiffrement** : `BHUILuilfghuila3` (16 caractères ASCII)
- **Compression** : zlib (deflate) après déchiffrement
- **Endianness** : Little-endian
- **Taille du bloc chiffré** : 102 400 octets (100 KiB)

## Structure générale

```
[Données chiffrées AES-128 ECB] → [Déchiffrement] → [Données compressées zlib] → [Décompression] → [Données binaires]
```

### Processus de lecture

1. **Déchiffrement AES-128 ECB**
   - Lire les 102 400 premiers octets du fichier
   - Déchiffrer avec la clé `BHUILuilfghuila3`
   
2. **Décompression zlib**
   - Les données déchiffrées sont compressées avec zlib
   - Décompresser pour obtenir les données binaires brutes

3. **Parsing des sections**
   - Les données décompressées contiennent une série de sections identifiées par des signatures de 4 octets

## Format des sections

Chaque section suit cette structure :

```
[Signature: 4 octets] [Taille: 4 octets UInt32] [Données: N octets]
```

### Signatures de sections

| Signature | Type | Description |
|-----------|------|-------------|
| `hfma` | Header | En-tête de la bibliothèque (version, ID persistant) |
| `plma` | Playlist Master | Section contenant des playlists |
| `lama` | Album List | Liste d'albums |
| `iama` | Album Item | Élément album individuel |
| `lAma` | Artist List | Liste d'artistes |
| `iAma` | Artist Item | Élément artiste individuel |
| `ltma` | Track List | Liste de pistes |
| `itma` | Track Item | Élément piste individuel |
| `lPma` | Playlist List | Liste de playlists |
| `lpma` | Playlist Item | Élément playlist individuel |
| `boma` | Metadata | Section de métadonnées (chaînes, numériques, etc.) |

## Section Header (`hfma`)

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "hfma"
4 | UInt32 | Taille de la section
8 | UInt32 | Version majeure (ex: 3)
12 | UInt32 | Version mineure (ex: 9)
16 | UInt32 | Type de fichier (généralement 6)
28 | UInt64 | Library Persistent ID (identifiant unique de la bibliothèque)

### Exemple de valeurs
- Version : `3.9` → Application Version `1.2.5.7`
- Library Persistent ID : `98FC3FC19705B3BD` (hexa, UInt64)

## Section Track Item (`itma`)

Représente une piste musicale individuelle.

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "itma"
4 | UInt32 | Taille de la section
12 | UInt32 | Nombre de sections boma
16 | Int64 | Track Persistent ID
24+ | boma[] | Sections de métadonnées

### Champs directs de l'itma

**Offset** | **Type** | **Description**
-----------|----------|----------------
62 | Byte | LikeStatus - Statut j'aime/j'aime pas (0=neutre, 1=je n'aime plus, 2=j'aime, 3=j'aime pas explicite)
65 | Byte | Rating - Note sur 100 (0-100, par pas de 20 pour 0-5 étoiles)
86 | UInt16 | Movement Count - Nombre de mouvements (musique classique)
88 | UInt16 | Movement Number - Numéro du mouvement (musique classique)
160 | UInt16 | Track Number - Numéro de piste
166 | Int32 | Year - Année
172 | UInt64 | Album Reference - Référence vers iama (Album)
180 | UInt64 | Artist Reference - Référence vers iAma (Artist)

#### Valeurs LikeStatus

**Valeur** | **Nom** | **Description**
-----------|---------|----------------
0x00 | Neutral | Neutre - Pas de préférence définie
0x01 | Disliked | Je n'aime plus - État transitoire après retrait d'un like
0x02 | Liked | J'aime - L'utilisateur aime cette piste
0x03 | DislikedExplicit | J'aime pas - L'utilisateur a explicitement indiqué qu'il n'aime pas cette piste

**Note** : La distinction entre les valeurs 0x01 et 0x03 permet à Apple Music de différencier :
- Une piste dont le like a été retiré (0x01 - "Je n'aime plus")
- Une piste explicitement marquée comme "Je n'aime pas" (0x03 - dislike actif)

### Métadonnées track (sections boma)

Les métadonnées sont stockées dans des sous-sections `boma` avec différents sous-types :

**Sous-type** | **Format** | **Description**
--------------|------------|----------------
0x0001 | Numérique | Valeurs numériques (durée, année, etc.)
0x0002 | String UTF-16 | Titre de la piste
0x0003 | String UTF-16 | Album
0x0004 | String UTF-16 | Artiste
0x0005 | String UTF-16 | Genre
0x0006 | String UTF-16 | Type de fichier (Kind)
0x0008 | String UTF-16 | Commentaire
0x000B | String UTF-8 | URL du fichier (file:///)
0x000C | String UTF-16 | Compositeur
0x000E | String UTF-16 | Groupement
0x0017 | Binaire | Statistiques de lecture (72 octets)
0x001B | String UTF-16 | Artiste de l'album
0x001E | String UTF-16 | Titre de tri
0x001F | String UTF-16 | Album de tri
0x0020 | String UTF-16 | Artiste de tri
0x0021 | String UTF-16 | Artiste d'album de tri
0x0022 | String UTF-16 | Compositeur de tri
0x0038 | Binaire | Plist XML (placeholder vide)
0x003F | String UTF-16 | Nom de l'œuvre
0x0040 | String UTF-16 | Nom du mouvement
0x0042 | Book | Chemin du fichier
0x01FC | Book | Chemin du fichier (variante)
0x01FD | Book | Chemin du fichier (variante)
0x0200 | String UTF-16 | Chemin du fichier (variante)

### Section boma numérique (sous-type 0x0001)

**Offset relatif** | **Type** | **Description**
-------------------|----------|----------------
0 | UInt32 | Sous-type (0x0001)
4 | UInt32 | Nombre de valeurs
8+ | Valeurs | Paires (ID: UInt32, Valeur: UInt32/Int32)

#### IDs numériques connus

**ID** | **Type** | **Description**
-------|----------|----------------
0x10 | UInt32 | Durée (millisecondes)
0x19 | UInt32 | Année
0x1E | UInt32 | Numéro de piste
0x1F | UInt32 | Nombre de pistes
0x21 | UInt32 | Numéro de disque
0x22 | UInt32 | Nombre de disques
0x35 | UInt32 | BPM (tempo)
0x41 | Int32 | Note (rating) - offset 65 dans boma

### Section boma Statistiques de lecture (sous-type 0x0017)

Structure fixe de 72 octets contenant les statistiques de lecture :

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | UInt32 | Padding (toujours 0)
4 | UInt64 | Track Persistent ID
12 | UInt32 | Date de dernière lecture (timestamp Apple)
16 | UInt32 | Nombre de lectures
20-71 | Bytes | Données inconnues

**Exemple** (piste "Politik" de Coldplay) :
```
Offset   Octets                          Valeur
0-3      00-00-00-00                     Padding
4-11     2B-26-11-8E-13-6A-E4-6B         Persistent ID: 0x6BE46A138E11262B
12-15    E1-A5-32-E0                     Date lecture: 3761415649 → 2023-03-11 21:40:49
16-19    11-00-00-00                     Nb lectures: 17
20+      ...                             Inconnu
```

### Section boma URL de fichier (sous-type 0x000B)

Chemin de fichier encodé URL avec préfixe file:/// :

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | UInt32 | Sous-type (0x000B)
4 | UInt32 | Inconnu
8 | UInt32 | Longueur de l'URL
12-19 | Bytes | Padding
20+ | char[] | Chaîne URL UTF-8 (encodée URL)

**Exemple** : `file:///Users/Ben/Music/Music/Coldplay/A%20Rush%20of%20Blood%20to%20the%20Head/01%20Politik.mp3`

## Section Playlist Item (`lpma`)

Représente une playlist individuelle.

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "lpma"
4 | UInt32 | Taille de la section
12 | Int32 | Nombre de sections boma
16 | Int32 | Nombre de pistes dans la playlist
22 | UInt32 | Date de création (secondes depuis 1904-01-01 UTC)
30 | UInt64 | Playlist Persistent ID
38 | UInt64 | Padding/Reserved (0x0100000000000000)
50 | UInt64 | Parent Persistent ID (0 si racine)
58+ | Padding | Zéros
79 | Byte | Distinguished Kind (si ParentId = 0)
80 | Byte | Distinguished Kind (si ParentId ≠ 0)
138 | UInt32 | Date de modification (secondes depuis 1904-01-01 UTC)

### Distinguished Kind

Valeurs connues pour identifier les playlists système :

**Valeur** | **Description**
-----------|---------------
0 | Playlist normale (manuelle ou intelligente)
4 | Musique (toute la musique)
19 | Achats (titres achetés)
26 | Genius
47 | Clips vidéo
63 | Hidden Cloud PlaylistOnly Tracks
64 | Séries et films
65 | Téléchargé (tout le contenu téléchargé)
200 | Playlist intelligente avec critères spéciaux

### Métadonnées playlist (sections boma)

**Sous-type** | **Format** | **Description**
--------------|------------|----------------
0x00C8 | String UTF-16 | Nom de la playlist
0x00C9 | Data | Critères de playlist intelligente (présence = Smart)
0x00CE | Track Entry | Entrée de piste avec sous-section ipfa

### Section boma Track Entry (sous-type 0x00CE)

Structure pour chaque piste dans une playlist :

**Offset relatif** | **Type** | **Description**
-------------------|----------|----------------
0 | char[4] | Sous-type 0x00CE
4 | UInt32 | Taille
20 | char[4] | Signature "ipfa"
24 | UInt32 | Taille ipfa
40 | UInt64 | Track Persistent ID

## Section Album Item (`iama`)

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "iama"
4 | UInt32 | Taille de la section
12 | UInt32 | Nombre de sections boma
16 | Int64 | Album Persistent ID
24+ | boma[] | Sections de métadonnées

### Métadonnées album (sections boma)

**Sous-type** | **Format** | **Description**
--------------|------------|----------------
0x012C | String UTF-16 | Titre de l'album
0x012D | String UTF-16 | Artiste de l'album
0x012E | String UTF-16 | Artiste

## Section Artist Item (`iAma`)

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "iAma"
4 | UInt32 | Taille de la section
12 | UInt32 | Nombre de sections boma
16 | Int64 | Artist Persistent ID
24+ | boma[] | Sections de métadonnées

### Métadonnées artiste (sections boma)

**Sous-type** | **Format** | **Description**
--------------|------------|----------------
0x0004 | String UTF-16 | Nom de l'artiste

## Format des chaînes UTF-16 (boma widechar)

Les chaînes de caractères dans les sections boma suivent ce format :

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | UInt32 | Sous-type (ex: 0x0002 pour titre)
4 | UInt32 | Taille de la chaîne (en octets)
8 | Padding | 8 octets de padding
16 | Bytes[] | Chaîne UTF-16 LE (Unicode)

### Exemple : Titre "Groovy"

```
02 00 00 00    // Sous-type 0x0002 (titre)
0C 00 00 00    // Taille = 12 octets
00 00 00 00 00 00 00 00  // Padding
47 00 72 00 6F 00 6F 00 76 00 79 00  // "Groovy" en UTF-16 LE
```

## Dates Apple

Les dates sont stockées en UInt32 représentant le nombre de secondes écoulées depuis **1er janvier 1904 00:00:00 UTC**.

### Conversion

```csharp
DateTime ConvertAppleDate(uint appleDate)
{
    var appleEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return appleEpoch.AddSeconds(appleDate);
}
```

### Exemple
- Valeur brute : `3526146962` (UInt32)
- Date convertie : `2015-09-08 16:36:02 UTC`

## Types de playlists

### Détermination du type

1. **Folder** : `TrackCount = 0` et `DistinguishedKind = 0`
2. **System** : `DistinguishedKind ≠ 0`
3. **Smart** : Présence de section boma 0x00C9 (critères intelligents)
4. **Manual** : Par défaut (aucun des cas précédents)

### Playlist Master

La playlist spéciale "Bibliothèque" (Master) a toujours :
- **Playlist Persistent ID** : `0x0000000000000005` (5)
- **Nom** : `####!####` (placeholder dans le binaire)
- **TrackCount** : Nombre total de pistes de la bibliothèque
- **Nom réel** : "Bibliothèque" (stocké uniquement dans l'export XML)

## Hiérarchie des playlists

Les playlists peuvent être organisées en arborescence via le champ `ParentId` :

- `ParentId = 0` → Playlist racine
- `ParentId = Library Persistent ID` → Également racine (normalisé à 0)
- `ParentId = autre` → Playlist enfant

## Filtrage des UUIDs dans les chemins de fichiers

Certains chemins de fichiers contiennent des UUIDs qui doivent être filtrés :

### Pattern UUID
```
/[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}/
```

### Exemple
```
/Users/Ben/Music/Music/Media.localized/Music/F8A23B4C-1D2E-4F5A-9B8C-1E2F3A4B5C6D/file.m4a
                                       └─────────────── UUID à supprimer ──────────────┘
↓
/Users/Ben/Music/Music/Media.localized/Music/file.m4a
```

## Exemple de structure complète

```
Library.musicdb (chiffré)
  ↓ [Déchiffrement AES-128 ECB]
Données compressées
  ↓ [Décompression zlib]
Données binaires
  ├─ hfma (Header)
  │   └─ Library ID: 98FC3FC19705B3BD
  │
  ├─ ltma (Track List)
  │   ├─ itma (Track 1)
  │   │   ├─ Track ID: 13274901623753364026
  │   │   └─ boma sections
  │   │       ├─ 0x0001 (numériques)
  │   │       ├─ 0x0002 "Dog With A Rope" (titre)
  │   │       ├─ 0x0003 "Groove Armada" (album)
  │   │       └─ 0x0004 "Groove Armada" (artiste)
  │   └─ itma (Track 2)...
  │
  ├─ lama (Album List)
  │   └─ iama (Album 1)
  │       ├─ Album ID
  │       └─ boma sections
  │
  ├─ lAma (Artist List)
  │   └─ iAma (Artist 1)
  │       ├─ Artist ID
  │       └─ boma sections
  │
  └─ lPma (Playlist List)
      └─ lpma (Playlist 1)
          ├─ Playlist ID: CFB5CC40347104E5
          ├─ Name: "Groovy"
          ├─ TrackCount: 29
          └─ boma sections
              ├─ 0x00C8 "Groovy" (nom)
              └─ 0x00CE (Track Entry) × 29
                  └─ ipfa → Track IDs
```

## Statistiques d'un fichier exemple

- **Library ID** : `98FC3FC19705B3BD`
- **Version** : `1.2.5.7`
- **Pistes** : 13 162
- **Albums** : 1 017
- **Artistes** : 649
- **Playlists** : 41
  - 12 manuelles
  - 27 intelligentes (Smart)
  - 2 système

## Références

- Format partiel : [Gary Vollink - MusicDB Format](https://www.home.vollink.com/gary/playlister/musicdb.html)
- Code Perl : [musicdb-poc](https://gitlab.home.vollink.com/external/musicdb-poc)
- Format iTunes ITL : [libitlp](https://github.com/jeanthom/libitlp)

## Notes importantes

1. **Offsets variables** : L'offset du Distinguished Kind varie selon la présence d'un ParentId
   - Sans parent : offset 79
   - Avec parent : offset 80

2. **IDs persistants** : Tous les IDs (Track, Album, Artist, Playlist, Library) sont des UInt64 en little-endian

3. **Encodage** : Les chaînes de caractères sont toujours en UTF-16 Little-Endian (Unicode)

4. **Sections boma** : Les sections boma peuvent apparaître dans différents contextes (track, album, artist, playlist) avec des sous-types spécifiques à chaque contexte
