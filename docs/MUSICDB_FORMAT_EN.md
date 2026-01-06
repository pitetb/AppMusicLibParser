# Library.musicdb File Format

## Overview

The `Library.musicdb` file is the binary database format used by Apple Music on macOS to store the complete music library. This format contains all metadata for tracks, albums, artists, and playlists.

## Main Characteristics

- **Encryption**: AES-128 ECB
- **Decryption Key**: `BHUILuilfghuila3` (16 ASCII characters)
- **Compression**: zlib (deflate) after decryption
- **Endianness**: Little-endian
- **Encrypted Block Size**: 102,400 bytes (100 KiB)

## General Structure

```
[AES-128 ECB Encrypted Data] → [Decryption] → [zlib Compressed Data] → [Decompression] → [Binary Data]
```

### Reading Process

1. **AES-128 ECB Decryption**
   - Read the first 102,400 bytes of the file
   - Decrypt using the key `BHUILuilfghuila3`
   
2. **zlib Decompression**
   - The decrypted data is compressed with zlib
   - Decompress to obtain raw binary data

3. **Section Parsing**
   - The decompressed data contains a series of sections identified by 4-byte signatures

## Section Format

Each section follows this structure:

```
[Signature: 4 bytes] [Size: 4 bytes UInt32] [Data: N bytes]
```

### Section Signatures

| Signature | Type | Description |
|-----------|------|-------------|
| `hfma` | Header | Library header (version, persistent ID) |
| `plma` | Playlist Master | Section containing playlists |
| `lama` | Album List | List of albums |
| `iama` | Album Item | Individual album element |
| `lAma` | Artist List | List of artists |
| `iAma` | Artist Item | Individual artist element |
| `ltma` | Track List | List of tracks |
| `itma` | Track Item | Individual track element |
| `lPma` | Playlist List | List of playlists |
| `lpma` | Playlist Item | Individual playlist element |
| `boma` | Metadata | Metadata section (strings, numerics, etc.) |

## Header Section (`hfma`)

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "hfma"
4 | UInt32 | Section size
8 | UInt32 | Major version (e.g., 3)
12 | UInt32 | Minor version (e.g., 9)
16 | UInt32 | File type (typically 6)
28 | UInt64 | Library Persistent ID (unique library identifier)

### Example Values
- Version: `3.9` → Application Version `1.2.5.7`
- Library Persistent ID: `98FC3FC19705B3BD` (hex, UInt64)

## Track Item Section (`itma`)

Represents an individual music track.

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "itma"
4 | UInt32 | Section size
12 | UInt32 | Number of boma sections
16 | Int64 | Track Persistent ID
24+ | boma[] | Metadata sections

### Direct itma Fields

**Offset** | **Type** | **Description**
-----------|----------|----------------
62 | Byte | LikeStatus - Like/Dislike status (0=neutral, 1=disliked, 2=liked, 3=disliked explicit)
65 | Byte | Rating - Rating out of 100 (0-100, steps of 20 for 0-5 stars)
86 | UInt16 | Movement Count - Number of movements (classical music)
88 | UInt16 | Movement Number - Movement number (classical music)
160 | UInt16 | Track Number - Track number
166 | Int32 | Year - Year
172 | UInt64 | Album Reference - Reference to iama (Album)
180 | UInt64 | Artist Reference - Reference to iAma (Artist)

#### LikeStatus Values

**Value** | **Name** | **Description**
----------|----------|----------------
0x00 | Neutral | Neutral - No preference set
0x01 | Disliked | No longer liked - Transient state after removing a like
0x02 | Liked | Liked - User likes this track
0x03 | DislikedExplicit | Disliked - User explicitly indicated they dislike this track

**Note**: The distinction between values 0x01 and 0x03 allows Apple Music to differentiate:
- A track whose like was removed (0x01 - "No longer liked")
- A track explicitly marked as "Dislike" (0x03 - active dislike)

### Track Metadata (boma sections)

Metadata is stored in `boma` sub-sections with different sub-types:

**Sub-type** | **Format** | **Description**
-------------|------------|----------------
0x0001 | Numeric | Numeric values (duration, year, etc.)
0x0002 | UTF-16 String | Track title
0x0003 | UTF-16 String | Album
0x0004 | UTF-16 String | Artist
0x0005 | UTF-16 String | Genre
0x0006 | UTF-16 String | File type (Kind)
0x0008 | UTF-16 String | Comment
0x000B | UTF-8 String | File URL (file:///)
0x000C | UTF-16 String | Composer
0x000E | UTF-16 String | Grouping
0x0017 | Binary | Play statistics (72 bytes)
0x001B | UTF-16 String | Album artist
0x001E | UTF-16 String | Sort title
0x001F | UTF-16 String | Sort album
0x0020 | UTF-16 String | Sort artist
0x0021 | UTF-16 String | Sort album artist
0x0022 | UTF-16 String | Sort composer
0x0038 | Binary | XML plist (empty placeholder)
0x003F | UTF-16 String | Work name
0x0040 | UTF-16 String | Movement name
0x0042 | Book | File path
0x01FC | Book | File path (variant)
0x01FD | Book | File path (variant)
0x0200 | Book | File path (variant)

### Numeric boma Section (sub-type 0x0001)

**Relative Offset** | **Type** | **Description**
--------------------|----------|----------------
0 | UInt32 | Sub-type (0x0001)
4 | UInt32 | Number of values
8+ | Values | Pairs (ID: UInt32, Value: UInt32/Int32)

#### Known Numeric IDs

**ID** | **Type** | **Description**
-------|----------|----------------
0x10 | UInt32 | Duration (milliseconds)
0x19 | UInt32 | Year
0x1E | UInt32 | Track number
0x1F | UInt32 | Track count
0x21 | UInt32 | Disc number
0x22 | UInt32 | Disc count
0x35 | UInt32 | BPM (tempo)
0x41 | Int32 | Rating - offset 65 in boma

### Play Statistics boma Section (sub-type 0x0017)

Fixed structure of 72 bytes containing play statistics:

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | UInt32 | Padding (always 0)
4 | UInt64 | Track Persistent ID
12 | UInt32 | Last Play Date (Apple timestamp)
16 | UInt32 | Play Count
20-71 | Bytes | Unknown data

**Example** (track "Politik" by Coldplay):
```
Offset   Bytes                           Value
0-3      00-00-00-00                     Padding
4-11     2B-26-11-8E-13-6A-E4-6B         Persistent ID: 0x6BE46A138E11262B
12-15    E1-A5-32-E0                     Play Date: 3761415649 → 2023-03-11 21:40:49
16-19    11-00-00-00                     Play Count: 17
20+      ...                             Unknown
```

### File URL boma Section (sub-type 0x000B)

URL-encoded file path with file:/// prefix:

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | UInt32 | Sub-type (0x000B)
4 | UInt32 | Unknown
8 | UInt32 | URL length
12-19 | Bytes | Padding
20+ | char[] | UTF-8 URL string (URL-encoded)

**Example**: `file:///Users/Ben/Music/Music/Coldplay/A%20Rush%20of%20Blood%20to%20the%20Head/01%20Politik.mp3`

## Playlist Item Section (`lpma`)

Represents an individual playlist.

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "lpma"
4 | UInt32 | Section size
12 | Int32 | Number of boma sections
16 | Int32 | Number of tracks in playlist
22 | UInt32 | Creation date (seconds since 1904-01-01 UTC)
30 | UInt64 | Playlist Persistent ID
38 | UInt64 | Padding/Reserved (0x0100000000000000)
50 | UInt64 | Parent Persistent ID (0 if root)
58+ | Padding | Zeros
79 | Byte | Distinguished Kind (if ParentId = 0)
80 | Byte | Distinguished Kind (if ParentId ≠ 0)
138 | UInt32 | Modification date (seconds since 1904-01-01 UTC)

### Distinguished Kind

Known values for identifying system playlists:

**Value** | **Description**
----------|---------------
0 | Normal playlist (manual or smart)
4 | Music (all music)
19 | Purchases
26 | Genius
47 | Music Videos
63 | Hidden Cloud PlaylistOnly Tracks
64 | TV Shows & Movies
65 | Downloaded (all downloaded content)
200 | Smart playlist with special criteria

### Playlist Metadata (boma sections)

**Sub-type** | **Format** | **Description**
-------------|------------|----------------
0x00C8 | UTF-16 String | Playlist name
0x00C9 | Data | Smart playlist criteria (presence = Smart)
0x00CE | Track Entry | Track entry with ipfa sub-section

### boma Track Entry Section (sub-type 0x00CE)

Structure for each track in a playlist:

**Relative Offset** | **Type** | **Description**
--------------------|----------|----------------
0 | char[4] | Sub-type 0x00CE
4 | UInt32 | Size
20 | char[4] | Signature "ipfa"
24 | UInt32 | ipfa size
40 | UInt64 | Track Persistent ID

## Album Item Section (`iama`)

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "iama"
4 | UInt32 | Section size
12 | UInt32 | Number of boma sections
16 | Int64 | Album Persistent ID
24+ | boma[] | Metadata sections

### Album Metadata (boma sections)

**Sub-type** | **Format** | **Description**
-------------|------------|----------------
0x012C | UTF-16 String | Album title
0x012D | UTF-16 String | Album artist
0x012E | UTF-16 String | Artist

## Artist Item Section (`iAma`)

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | char[4] | Signature "iAma"
4 | UInt32 | Section size
12 | UInt32 | Number of boma sections
16 | Int64 | Artist Persistent ID
24+ | boma[] | Metadata sections

### Artist Metadata (boma sections)

**Sub-type** | **Format** | **Description**
-------------|------------|----------------
0x0004 | UTF-16 String | Artist name

## UTF-16 String Format (boma widechar)

Character strings in boma sections follow this format:

**Offset** | **Type** | **Description**
-----------|----------|----------------
0 | UInt32 | Sub-type (e.g., 0x0002 for title)
4 | UInt32 | String size (in bytes)
8 | Padding | 8 bytes of padding
16 | Bytes[] | UTF-16 LE string (Unicode)

### Example: Title "Groovy"

```
02 00 00 00    // Sub-type 0x0002 (title)
0C 00 00 00    // Size = 12 bytes
00 00 00 00 00 00 00 00  // Padding
47 00 72 00 6F 00 6F 00 76 00 79 00  // "Groovy" in UTF-16 LE
```

## Apple Dates

Dates are stored as UInt32 representing the number of seconds elapsed since **January 1, 1904 00:00:00 UTC**.

### Conversion

```csharp
DateTime ConvertAppleDate(uint appleDate)
{
    var appleEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return appleEpoch.AddSeconds(appleDate);
}
```

### Example
- Raw value: `3526146962` (UInt32)
- Converted date: `2015-09-08 16:36:02 UTC`

## Playlist Types

### Type Determination

1. **Folder**: `TrackCount = 0` and `DistinguishedKind = 0`
2. **System**: `DistinguishedKind ≠ 0`
3. **Smart**: Presence of boma section 0x00C9 (smart criteria)
4. **Manual**: Default (none of the previous cases)

### Master Playlist

The special "Library" (Master) playlist always has:
- **Playlist Persistent ID**: `0x0000000000000005` (5)
- **Name**: `####!####` (placeholder in binary)
- **TrackCount**: Total number of tracks in library
- **Real Name**: "Library" (stored only in XML export)

## Playlist Hierarchy

Playlists can be organized in a tree structure via the `ParentId` field:

- `ParentId = 0` → Root playlist
- `ParentId = Library Persistent ID` → Also root (normalized to 0)
- `ParentId = other` → Child playlist

## UUID Filtering in File Paths

Some file paths contain UUIDs that should be filtered:

### UUID Pattern
```
/[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}/
```

### Example
```
/Users/Ben/Music/Music/Media.localized/Music/F8A23B4C-1D2E-4F5A-9B8C-1E2F3A4B5C6D/file.m4a
                                       └──────────── UUID to remove ─────────────┘
↓
/Users/Ben/Music/Music/Media.localized/Music/file.m4a
```

## Complete Structure Example

```
Library.musicdb (encrypted)
  ↓ [AES-128 ECB Decryption]
Compressed data
  ↓ [zlib Decompression]
Binary data
  ├─ hfma (Header)
  │   └─ Library ID: 98FC3FC19705B3BD
  │
  ├─ ltma (Track List)
  │   ├─ itma (Track 1)
  │   │   ├─ Track ID: 13274901623753364026
  │   │   └─ boma sections
  │   │       ├─ 0x0001 (numerics)
  │   │       ├─ 0x0002 "Dog With A Rope" (title)
  │   │       ├─ 0x0003 "Groove Armada" (album)
  │   │       └─ 0x0004 "Groove Armada" (artist)
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
              ├─ 0x00C8 "Groovy" (name)
              └─ 0x00CE (Track Entry) × 29
                  └─ ipfa → Track IDs
```

## Example File Statistics

- **Library ID**: `98FC3FC19705B3BD`
- **Version**: `1.2.5.7`
- **Tracks**: 13,162
- **Albums**: 1,017
- **Artists**: 649
- **Playlists**: 41
  - 12 manual
  - 27 smart
  - 2 system

## References

- Partial Format: [Gary Vollink - MusicDB Format](https://www.home.vollink.com/gary/playlister/musicdb.html)
- Perl Code: [musicdb-poc](https://gitlab.home.vollink.com/external/musicdb-poc)
- iTunes ITL Format: [libitlp](https://github.com/jeanthom/libitlp)

## Important Notes

1. **Variable Offsets**: The Distinguished Kind offset varies depending on the presence of a ParentId
   - Without parent: offset 79
   - With parent: offset 80

2. **Persistent IDs**: All IDs (Track, Album, Artist, Playlist, Library) are UInt64 in little-endian

3. **Encoding**: Character strings are always in UTF-16 Little-Endian (Unicode)

4. **boma Sections**: boma sections can appear in different contexts (track, album, artist, playlist) with context-specific sub-types
