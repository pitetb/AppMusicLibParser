using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Microsoft.Extensions.Logging;
using MusicParser.Crypto;
using MusicParser.Models;

namespace MusicParser.Parsers;

/// <summary>
/// Parser pour le format binaire MusicDB d'Apple Music
/// Documentation: folder "docs" et https://www.home.vollink.com/gary/playlister/musicdb.html
/// 
/// Format MusicDB:
/// 1. En-tête 'hfma' (160 bytes) - LITTLE ENDIAN
/// 2. Payload chiffré AES-128 ECB (102400 bytes max) puis compressé zlib
/// 3. Sections internes: hsma, hfma, plma, ltma, itma, boma, etc.
/// </summary>
public class MusicDbParser
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly Dictionary<ulong, MusicTrack> _tracks = new();
    private readonly Dictionary<ulong, Album> _albums = new();
    private readonly Dictionary<ulong, Artist> _artists = new();
    private readonly Dictionary<string, Playlist> _playlists = new(); // Clé: "{playlistId}_{position}"
    private int _playlistCounter = 0;
    private ulong _libraryId = 0;
    private Dictionary<uint, int> _unknownBomaTypes = new(); // Compteur des types boma inconnus
    private MusicTrack? _currentTrack = null;
    private Album? _currentAlbum = null;
    private Artist? _currentArtist = null;
    private Playlist? _currentPlaylist = null;
    private uint _maxCryptSize = 0;

    public MusicDbParser(string filePath, ILogger logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MusicLibrary Parse()
    {
        var library = new MusicLibrary
        {
            LibraryPath = _filePath,
            ParsedAt = DateTime.Now
        };

        using var fileStream = File.OpenRead(_filePath);
        using var reader = new BinaryReader(fileStream);

        var envelopeLength = ParseHeader(reader, library);
        
        // Utiliser la méthode factorisée
        var (_, maxCryptSize, decryptedData) = MusicDbDecryptor.DecryptAndDecompressFile(_filePath);
        _maxCryptSize = maxCryptSize;

        using var decryptedStream = new MemoryStream(decryptedData);
        using var decryptedReader = new BinaryReader(decryptedStream);
        ParseInnerSections(decryptedReader, library);

        // Afficher les types boma inconnus
        if (_unknownBomaTypes.Any())
        {
            _logger.LogDebug("\n=== Types boma inconnus ===");
            foreach (var kvp in _unknownBomaTypes.OrderByDescending(x => x.Value).Take(20))
            {
                _logger.LogDebug("  0x{kvp.Key:X4}: {kvp.Value:N0} occurrences", kvp.Key, kvp.Value);
            }
        }

        return library;
    }

    private uint ParseHeader(BinaryReader reader, MusicLibrary library)
    {
        _logger.LogDebug("=== En-tête MusicDB ===");
        
        var signature = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (signature != "hfma")
            throw new InvalidDataException($"Signature invalide: '{signature}'");
        
        var envelopeLength = reader.ReadUInt32();
        var fileSize = reader.ReadUInt32();
        var majorVersion = reader.ReadUInt16();
        var minorVersion = reader.ReadUInt16();
        
        var versionBytes = reader.ReadBytes(32);
        var nullIndex = Array.IndexOf(versionBytes, (byte)0);
        var versionString = Encoding.UTF8.GetString(versionBytes, 0, nullIndex > 0 ? nullIndex : 32);
        
        _libraryId = reader.ReadUInt64();
        var fileType = reader.ReadUInt32();
        reader.ReadBytes(8); // skip unknown
        
        var trackCount = reader.ReadUInt32();
        var playlistCount = reader.ReadUInt32();
        var albumCount = reader.ReadUInt32();
        var artistCount = reader.ReadUInt32();
        _maxCryptSize = reader.ReadUInt32();
        
        // Stocker dans MusicLibrary
        library.LibraryId = _libraryId;
        library.VersionString = versionString;
        library.MajorVersion = majorVersion;
        library.MinorVersion = minorVersion;
        library.FileType = fileType;
        library.EnvelopeLength = envelopeLength;
        library.FileSize = fileSize;
        library.MaxCryptSize = _maxCryptSize;
        library.HeaderTrackCount = trackCount;
        library.HeaderAlbumCount = albumCount;
        library.HeaderArtistCount = artistCount;
        library.HeaderPlaylistCount = playlistCount;
        
        _logger.LogDebug("Library Persistent ID: {LibraryId:X16}", _libraryId);
        _logger.LogDebug("Version: {Version} (format {Major}.{Minor})", versionString, majorVersion, minorVersion);
        _logger.LogDebug("File Type: {FileType}", fileType);
        _logger.LogDebug("Pistes: {TrackCount:N0} | Albums: {AlbumCount:N0} | Artistes: {ArtistCount:N0} | Playlists: {PlaylistCount}", trackCount, albumCount, artistCount, playlistCount);
        
        // Sauter le reste de l'en-tête
        var remaining = envelopeLength - reader.BaseStream.Position;
        if (remaining > 0) reader.ReadBytes((int)remaining);
        
        return envelopeLength;
    }

    private void ParseInnerSections(BinaryReader reader, MusicLibrary library)
    {
        _logger.LogDebug("=== Parsing des sections ===");
        
        int sectionCount = 0;
        
        try
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
            {
                var startPos = reader.BaseStream.Position;
                var signatureBytes = reader.ReadBytes(4);
                
                if (signatureBytes.All(b => b == 0)) break;
                
                var signature = Encoding.ASCII.GetString(signatureBytes);
                
                // Read size field (different position for boma)
                uint sectionSize;
                if (signature == "boma")
                {
                    reader.ReadUInt32(); // skip 0x14 marker
                    sectionSize = reader.ReadUInt32();
                }
                else
                {
                    sectionSize = reader.ReadUInt32();
                }
                
                sectionCount++;
                
                switch (signature)
                {
                    case "lama": // Album master - reset context
                        _currentAlbum = null;
                        reader.BaseStream.Position = startPos + sectionSize;
                        break;
                    
                    case "iama": // Album section
                        _currentTrack = null;
                        _currentArtist = null;
                        ParseAlbumItem(reader, sectionSize);
                        break;
                    
                    case "lAma": // Artist master - reset context
                        _currentArtist = null;
                        reader.BaseStream.Position = startPos + sectionSize;
                        break;
                    
                    case "iAma": // Artist section
                        _currentTrack = null;
                        _currentAlbum = null;
                        ParseArtistItem(reader, sectionSize);
                        break;
                    
                    case "lPma": // Playlists master - reset context
                        _currentPlaylist = null;
                        reader.BaseStream.Position = startPos + sectionSize;
                        break;
                    
                    case "lpma": // Playlist container
                        _currentTrack = null;
                        _currentAlbum = null;
                        _currentArtist = null;
                        ParsePlaylistItem(reader, sectionSize);
                        break;
                    
                    case "ltma": // Track master - reset context
                        _currentTrack = null;
                        var trackCount = reader.ReadUInt32();
                        if (sectionCount <= 20)
                            _logger.LogDebug("Section ltma: {TrackCount} pistes", trackCount);
                        break;
                        
                    case "itma":
                        ParseTrackItem(reader, sectionSize);
                        break;
                    
                    case "boma":
                        ParseBomaSection(reader, sectionSize);
                        break;
                }
                
                // Navigate to next section
                // Standard sections: currentPos + sizeField
                // Boma sections: currentPos + sizeField (size includes header)
                long nextPos;
                if (signature == "boma")
                {
                    nextPos = startPos + sectionSize;
                }
                else
                {
                    nextPos = startPos + sectionSize;
                }
                    
                if (nextPos <= reader.BaseStream.Length)
                    reader.BaseStream.Position = nextPos;
                
                if (sectionCount % 5000 == 0)
                    _logger.LogDebug("Sections: {SectionCount:N0}, Pistes: {TrackCount:N0}, Albums: {AlbumCount:N0}, Artistes: {ArtistCount:N0}, Playlists: {PlaylistCount:N0}", 
                        sectionCount, _tracks.Count, _albums.Count, _artists.Count, _playlists.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Erreur à la section #{SectionCount}: {Message}", sectionCount, ex.Message);
        }
        
        _logger.LogDebug("{SectionCount:N0} sections analysées", sectionCount);
        _logger.LogDebug("   {TrackCount:N0} pistes, {AlbumCount:N0} albums, {ArtistCount:N0} artistes, {PlaylistCount:N0} playlists", 
            _tracks.Count, _albums.Count, _artists.Count, _playlists.Count);
        
        library.Tracks = _tracks.Values.ToList();
        library.Albums = _albums.Values.ToList();
        library.Artists = _artists.Values.ToList();
        library.Playlists = _playlists.Values.ToList();
    }
    
    private void ParseAlbumItem(BinaryReader reader, uint sectionLength)
    {
        var sectionStart = reader.BaseStream.Position - 8;
        
        reader.ReadUInt32(); // offset 8: Associated sections length
        reader.ReadUInt32(); // offset 12: boma count
        var albumId = reader.ReadUInt64(); // offset 16: Album ID
        
        if (!_albums.ContainsKey(albumId))
            _albums[albumId] = new Album { AlbumId = albumId };
        
        // Ne pas naviguer à la fin - laisser les boma être parsés
        reader.BaseStream.Position = sectionStart + 24; // Après l'ID
        // Navigate to end of section
        reader.BaseStream.Position = sectionStart + sectionLength;
    }
    
    private void ParseArtistItem(BinaryReader reader, uint sectionLength)
    {
        var sectionStart = reader.BaseStream.Position - 8;
        
        var associatedLength = reader.ReadUInt32(); // offset 8: Associated sections length
        reader.ReadUInt32(); // offset 12: boma count
        var artistId = reader.ReadUInt64(); // offset 16: Artist ID
        
        if (!_artists.ContainsKey(artistId))
            _artists[artistId] = new Artist { ArtistId = artistId };
        
        _currentArtist = _artists[artistId];
        
        // Retourner après l'ID pour laisser les boma être lus par le switch principal
        reader.BaseStream.Position = sectionStart + 24;
    }
    
    private void ParsePlaylistItem(BinaryReader reader, uint sectionLength)
    {
        var sectionStart = reader.BaseStream.Position - 8;
        
        // Lire boma count à l'offset 12
        reader.BaseStream.Position = sectionStart + 12;
        var bomaCount = reader.ReadInt32();
        
        // Lire track count à l'offset 16
        var trackCount = reader.ReadInt32();
        
        // Lire Playlist Persistent ID à l'offset 30
        reader.BaseStream.Position = sectionStart + 30;
        var playlistId = reader.ReadUInt64();
        
        // Lire ParentId à l'offset 50
        ulong parentId = 0;
        
        if (sectionLength >= 58)
        {
            reader.BaseStream.Position = sectionStart + 50;
            parentId = reader.ReadUInt64();
            
            // Si ParentId = Library ID, c'est la racine (pas de parent)
            if (parentId == _libraryId)
            {
                parentId = 0;
            }
        }
        
        // Lire DistinguishedKind
        // L'offset varie selon si la playlist a un parent ou non:
        // - Sans parent (ParentId = 0): offset 79 (1 octet)
        // - Avec parent (ParentId != 0): offset 80 (1 octet)
        int distinguishedKind = 0;
        if (sectionLength >= 82)
        {
            int distKindOffset = (parentId == 0) ? 79 : 80;
            reader.BaseStream.Position = sectionStart + distKindOffset;
            distinguishedKind = reader.ReadByte();
        }
        
        // Si playlistId=0, c'est une playlist sans ID persistant
        // On garde le vrai ID pour les références ParentId
        ulong actualPlaylistId = playlistId;
        
        // Créer une clé unique pour le dictionnaire
        var playlistKey = $"{playlistId}_{sectionStart}";
        
        // Déterminer le type de playlist
        PlaylistType playlistType;
        if (trackCount == 0 && distinguishedKind == 0)
        {
            playlistType = PlaylistType.Folder;
        }
        else if (distinguishedKind != 0)
        {
            playlistType = PlaylistType.System;
        }
        else
        {
            // Par défaut manuel, sera changé en Smart si on trouve un boma 0x00C9
            playlistType = PlaylistType.Manual;
        }
        
        if (!_playlists.ContainsKey(playlistKey))
        {
            _playlists[playlistKey] = new Playlist 
            { 
                PlaylistId = actualPlaylistId,
                TrackCount = trackCount,
                ParentId = parentId,
                DistinguishedKind = distinguishedKind,
                Type = playlistType,
                IsMaster = (actualPlaylistId == 5)
            };
        }
        
        _currentPlaylist = _playlists[playlistKey];
        
        // Lire les dates si disponibles
        if (sectionLength >= 26)
        {
            reader.BaseStream.Position = sectionStart + 22;
            var createdDate = reader.ReadUInt32();
            _currentPlaylist.CreatedAt = ConvertAppleDate(createdDate);
        }
        
        if (sectionLength >= 142)
        {
            reader.BaseStream.Position = sectionStart + 138;
            var modifiedDate = reader.ReadUInt32();
            _currentPlaylist.ModifiedAt = ConvertAppleDate(modifiedDate);
        }
        
        // Retourner pour laisser les boma être parsés
        reader.BaseStream.Position = sectionStart + 47;
    }

    private void ParseTrackItem(BinaryReader reader, uint sectionLength)
    {
        // sectionStart = position AVANT d'avoir lu signature(4) + length(4)
        var sectionStart = reader.BaseStream.Position - 8; 
        
        reader.ReadUInt32(); // offset 8: unknown
        var bomaCount = reader.ReadUInt32(); // offset 12
        var trackId = reader.ReadUInt64(); // offset 16
        
        // On est maintenant à offset 24
        
        if (!_tracks.ContainsKey(trackId))
            _tracks[trackId] = new MusicTrack { TrackId = trackId };
        
        _currentTrack = _tracks[trackId];
        
        // Lire offset 65 pour le rating
        if (sectionLength >= 66)
        {
            reader.BaseStream.Position = sectionStart + 65;
            var rating = reader.ReadByte();
            if (rating > 0 && rating <= 100)
                _currentTrack.Rating = rating;
        }
        
        // Lire offset 62 pour le LikeStatus (1 byte) - 0=neutre, 1=dislike transitoire, 2=like, 3=dislike explicite
        if (sectionLength >= 63)
        {
            reader.BaseStream.Position = sectionStart + 62;
            var likeStatusByte = reader.ReadByte();
            if (likeStatusByte >= 0 && likeStatusByte <= 3)
                _currentTrack.LikeStatus = (Models.LikeStatus)likeStatusByte;
        }
        
        // Lire Track Number et Year si la section est assez grande
        if (sectionLength >= 172)
        {
            // Offset 86: Movement Count (2 bytes)
            reader.BaseStream.Position = sectionStart + 86;
            var movementCount = reader.ReadUInt16();
            if (movementCount > 0 && movementCount < 1000)
                _currentTrack.MovementCount = movementCount;
            
            // Offset 88: Movement Number (2 bytes)
            var movementNumber = reader.ReadUInt16();
            if (movementNumber > 0 && movementNumber < 1000)
                _currentTrack.MovementNumber = movementNumber;
            
            // Offset 160: Track Number (2 bytes)
            reader.BaseStream.Position = sectionStart + 160;
            var trackNumber = reader.ReadUInt16();
            if (trackNumber > 0 && trackNumber < 10000)
                _currentTrack.TrackNumber = trackNumber;
            
            reader.ReadBytes(6);
            var year = reader.ReadInt32();
            if (year > 1900 && year < 2100)
                _currentTrack.Year = year;
            
            // Offset 172: iama Reference ID (Album)
            reader.BaseStream.Position = sectionStart + 172;
            var albumRef = reader.ReadUInt64();
            if (albumRef != 0)
                _currentTrack.AlbumRef = albumRef;
            
            // Offset 180: iAma Reference ID (Artist)
            var artistRef = reader.ReadUInt64();
            if (artistRef != 0)
                _currentTrack.ArtistRef = artistRef;
        }
    }

    private void ParseBomaSection(BinaryReader reader, uint sectionLength)
    {
        if (_currentTrack == null && _currentAlbum == null && _currentArtist == null && _currentPlaylist == null) return;
        
        var subType = reader.ReadUInt32();
        var dataStart = reader.BaseStream.Position;
        try
        {
            switch (subType)
            {
                case 0x0001: // Numerics
                    ParseBomaNumeric(reader, sectionLength);
                    break;
                
                case 0x0002: // Title
                case 0x0003: // Album
                case 0x0004: // Artist
                case 0x0005: // Genre
                case 0x0006: // Kind
                case 0x0008: // Comment
                case 0x000C: // Composer
                case 0x000E: // Grouping
                case 0x001B: // Album Artist
                case 0x001E: // Sort Title
                case 0x001F: // Sort Album
                case 0x0020: // Sort Artist
                case 0x0021: // Sort Album Artist
                case 0x0022: // Sort Composer
                case 0x003F: // Work Name
                case 0x0040: // Movement Name
                case 0x012C: // Album title (iama)
                case 0x012D: // Album artist (iama)
                case 0x012E: // Album artist alt (iama)
                case 0x00C8: // Playlist name
                    ParseBomaWidechar(reader, subType);
                    break;
                
                case 0x00CE: // Playlist track (ipfa)
                    ParseBomaPlaylistTrack(reader, sectionLength);
                    break;
                
                case 0x00C9: // Smart Playlist Criteria
                    if (_currentPlaylist != null)
                    {
                        _currentPlaylist.HasSmartCriteria = true;
                        _currentPlaylist.Type = PlaylistType.Smart;
                    }
                    break;
                
                case 0x0042: // Book - File path
                case 0x01FC: // Book - File path
                case 0x01FD: // Book - File path
                case 0x0200: // Book - File path
                    ParseBomaBook(reader, sectionLength);
                    break;
                
                case 0x0017: // Play statistics
                    ParseBomaPlayStats(reader, sectionLength);
                    break;
                
                case 0x000B: // File URL
                    ParseBomaFileUrl(reader, sectionLength);
                    break;
                
                default:
                    // Compter les types inconnus
                    if (!_unknownBomaTypes.ContainsKey(subType))
                        _unknownBomaTypes[subType] = 0;
                    _unknownBomaTypes[subType]++;
                    break;
            }
        }
        catch { }
    }
    
    private void ParseBomaPlayStats(BinaryReader reader, uint sectionLength)
    {
        if (_currentTrack == null) return;
        
        try
        {
            // Structure (72 bytes total):
            // 0-3: Padding (00-00-00-00)
            // 4-11: Persistent ID (UInt64)
            // 12-15: Play Date (Apple timestamp UInt32)
            // 16-19: Play Count (UInt32)
            // 20-71: Other data (to be analyzed)
            
            reader.ReadUInt32(); // Padding
            reader.ReadUInt64(); // Persistent ID
            var playDate = reader.ReadUInt32();
            var playCount = reader.ReadUInt32();
            
            // Store in track
            _currentTrack.PlayCount = (int)playCount;
            _currentTrack.LastPlayed = ConvertAppleDate(playDate);
        }
        catch { }
    }
    
    private void ParseBomaFileUrl(BinaryReader reader, uint sectionLength)
    {
        if (_currentTrack == null) return;
        
        try
        {
            reader.ReadUInt32(); // Padding
            reader.ReadUInt32(); // Unknown (always 2?)
            var urlLength = reader.ReadUInt32();
            reader.ReadUInt32(); // Padding
            reader.ReadUInt32(); // Unknown
            
            if (urlLength > 0 && urlLength < 1000)
            {
                var urlBytes = reader.ReadBytes((int)urlLength);
                var url = Encoding.UTF8.GetString(urlBytes);
                
                // Décoder l'URL (remplacer %20 par espace, etc.)
                url = Uri.UnescapeDataString(url);
                
                // Supprimer le préfixe file:///
                if (url.StartsWith("file:///"))
                {
                    url = "/" + url.Substring(8);
                }
                
                _currentTrack.FileUrl = url;
            }
        }
        catch { }
    }
    
    private void ParseBomaBook(BinaryReader reader, uint sectionLength)
    {
        if (_currentTrack == null) return;
        
        try
        {
            var bomaStart = reader.BaseStream.Position - 16;
            
            // Chercher la signature 'book'
            reader.BaseStream.Position = bomaStart + 20;
            var bookSig = Encoding.ASCII.GetString(reader.ReadBytes(4));
            
            if (bookSig != "book") return;
            
            var allParts = new List<string>();
            var pathParts = new List<string>();
            var pos = reader.BaseStream.Position;
            var endPos = bomaStart + sectionLength;
            
            while (pos < endPos - 8)
            {
                reader.BaseStream.Position = pos;
                var strLen = reader.ReadUInt32();
                
                if (strLen > 0 && strLen < 1000)
                {
                    var marker = reader.ReadUInt32();
                    
                    // 0x0101 = chaîne de chemin
                    if (marker == 0x0101)
                    {
                        var strBytes = reader.ReadBytes((int)strLen);
                        var str = Encoding.UTF8.GetString(strBytes).TrimEnd('\0');
                        
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            allParts.Add(str);
                            
                            // Filtrer: garder seulement les vrais composants de chemin
                            // Ignorer: file:///, nom de disque, UUID filesystem, séparateur /
                            var isUuid = str.Length == 36 && str.Count(c => c == '-') == 4;
                            var isProtocol = str.StartsWith("file:///");
                            var isDiskName = str.Contains("Macintosh") || str.Contains("HD");
                            var isSeparator = str == "/";
                            
                            if (!isUuid && !isProtocol && !isDiskName && !isSeparator)
                            {
                                pathParts.Add(str);
                            }
                        }
                        
                        // Aligner sur 4 bytes
                        var padding = (4 - (strLen % 4)) % 4;
                        pos = reader.BaseStream.Position + padding;
                    }
                    else
                    {
                        pos += 4;
                    }
                }
                else
                {
                    pos += 4;
                }
            }
            
            if (pathParts.Count > 0)
            {
                _currentTrack.FilePath = string.Join("/", pathParts);
            }
        }
        catch { }
    }

    private void ParseBomaNumeric(BinaryReader reader, uint sectionLength)
    {
        if (_currentTrack == null || sectionLength < 180) return;
        
        try
        {
            // Les offsets sont depuis le début de la section boma
            // On a déjà lu 16 bytes (sig+marker+size+subtype)
            reader.ReadBytes(92); // skip to offset 108
            
            _currentTrack.BitRate = (int)reader.ReadUInt32();
            
            var dateAdded = reader.ReadUInt32();
            _currentTrack.DateAdded = ConvertAppleDate(dateAdded);
            
            reader.ReadBytes(32); // skip to offset 148
            
            var dateModified = reader.ReadUInt32();
            _currentTrack.DateModified = ConvertAppleDate(dateModified);
            
            reader.ReadBytes(24); // skip to offset 176
            
            _currentTrack.Duration = (int)reader.ReadUInt32();
            
            if (sectionLength >= 320)
            {
                reader.ReadBytes(136);
                _currentTrack.FileSize = reader.ReadUInt32();
            }
        }
        catch { }
    }

    private void ParseBomaWidechar(BinaryReader reader, uint subType)
    {
        // Doit fonctionner pour track, album, artist ET playlist
        if (_currentTrack == null && _currentAlbum == null && _currentArtist == null && _currentPlaylist == null) 
            return;
        
        try
        {
            // Offset 8 depuis le début des données (après subtype)
            reader.ReadBytes(8);
            var stringLength = reader.ReadUInt32();
            
            if (stringLength > 0 && stringLength < 10000)
            {
                reader.ReadBytes(8);
                var stringBytes = reader.ReadBytes((int)stringLength);
                var value = Encoding.Unicode.GetString(stringBytes).TrimEnd('\0');
                
                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Track boma fields
                    if (_currentTrack != null)
                    {
                        switch (subType)
                        {
                            case 0x0002: _currentTrack.Title = value; break;
                            case 0x0003: _currentTrack.Album = value; break;
                            case 0x0004: _currentTrack.Artist = value; break;
                            case 0x0005: _currentTrack.Genre = value; break;
                            case 0x0006: _currentTrack.Kind = value; break;
                            case 0x0008: _currentTrack.Comment = value; break;
                            case 0x000C: _currentTrack.Composer = value; break;
                            case 0x000E: _currentTrack.Grouping = value; break;
                            case 0x001B: _currentTrack.AlbumArtist = value; break;
                            case 0x001E: _currentTrack.SortTitle = value; break;
                            case 0x001F: _currentTrack.SortAlbum = value; break;
                            case 0x0020: _currentTrack.SortArtist = value; break;
                            case 0x0021: _currentTrack.SortAlbumArtist = value; break;
                            case 0x0022: _currentTrack.SortComposer = value; break;
                            case 0x003F: _currentTrack.WorkName = value; break;
                            case 0x0040: _currentTrack.MovementName = value; break;
                        }
                    }
                    // Album boma fields (iama sections)
                    else if (_currentAlbum != null)
                    {
                        switch (subType)
                        {
                            case 0x012C: _currentAlbum.Title = value; break;
                            case 0x012D: _currentAlbum.AlbumArtist = value; break;
                            case 0x012E: _currentAlbum.Artist = value; break;
                        }
                    }
                    // Artist boma fields (iAma sections)
                    else if (_currentArtist != null)
                    {
                        switch (subType)
                        {  
                            case 0x0004: _currentArtist.Name = value; break;
                        }
                    }
                    // Playlist boma fields
                    else if (_currentPlaylist != null)
                    {
                        switch (subType)
                        {
                            case 0x00C8: _currentPlaylist.Name = value; break;
                        }
                    }
                }
            }
        }
        catch { }
    }
    
    private void ParseBomaPlaylistTrack(BinaryReader reader, uint sectionLength)
    {
        if (_currentPlaylist == null) return;
        
        try
        {
            var bomaStart = reader.BaseStream.Position - 16;
            
            // Chercher la signature 'ipfa' à l'offset 20
            reader.BaseStream.Position = bomaStart + 20;
            var ipfaSig = Encoding.ASCII.GetString(reader.ReadBytes(4));
            
            if (ipfaSig != "ipfa") return;
            
            // Offset 40 depuis le début du boma: Track ID
            reader.BaseStream.Position = bomaStart + 40;
            var trackId = reader.ReadUInt64();
            
            if (trackId != 0)
            {
                _currentPlaylist.TrackIds.Add(trackId);
            }
        }
        catch { }
    }

    private DateTime? ConvertAppleDate(uint timestamp)
    {
        if (timestamp == 0) return null;
        
        try
        {
            var appleEpoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return appleEpoch.AddSeconds(timestamp);
        }
        catch
        {
            return null;
        }
    }
}
