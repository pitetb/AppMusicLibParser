using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace MusicParser.Crypto;

/// <summary>
/// Gère le déchiffrement AES-128 ECB pour les fichiers MusicDB d'Apple Music
/// La clé de déchiffrement doit être fournie via la variable d'environnement MUSICDB_AES_KEY
/// </summary>
public static class MusicDbDecryptor
{
    // AES-128 ECB decryption key (Apple Music key)
    // La clé doit être définie dans la variable d'environnement MUSICDB_AES_KEY
    private static readonly byte[] AesKey = GetAesKeyFromEnvironment();
    
    private static byte[] GetAesKeyFromEnvironment()
    {
        var key = Environment.GetEnvironmentVariable("MUSICDB_AES_KEY");
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException(
                "La variable d'environnement MUSICDB_AES_KEY n'est pas définie. " +
                "Veuillez créer un fichier .env avec MUSICDB_AES_KEY=<votre_clé> " +
                "ou définir la variable d'environnement manuellement.");
        }
        
        var keyBytes = Encoding.ASCII.GetBytes(key);
        if (keyBytes.Length != 16)
        {
            throw new InvalidOperationException(
                $"La clé AES doit faire exactement 16 caractères ASCII. Longueur actuelle: {keyBytes.Length}");
        }
        
        return keyBytes;
    }

    /// <summary>
    /// Déchiffre un bloc de données avec AES-128 ECB
    /// </summary>
    /// <param name="encryptedData">Données chiffrées</param>
    /// <returns>Données déchiffrées</returns>
    public static byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            return Array.Empty<byte>();

        using var aes = Aes.Create();
        aes.Key = AesKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None; // Pas de padding pour ECB

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
    }

    /// <summary>
    /// Déchiffre un flux de données
    /// </summary>
    public static Stream CreateDecryptionStream(Stream encryptedStream)
    {
        var aes = Aes.Create();
        aes.Key = AesKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        var decryptor = aes.CreateDecryptor();
        return new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read);
    }

    /// <summary>
    /// Déchiffre et décompresse un fichier MusicDB complet
    /// </summary>
    /// <param name="filePath">Chemin vers le fichier Library.musicdb</param>
    /// <returns>Tuple contenant (envelopeLength, maxCryptSize, decompressedData)</returns>
    public static (uint envelopeLength, uint maxCryptSize, byte[] decompressedData) DecryptAndDecompressFile(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var reader = new BinaryReader(fs);

        // Lire l'en-tête
        var signature = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (signature != "hfma")
            throw new InvalidDataException($"Invalid file signature: {signature}");

        var envelopeLength = reader.ReadUInt32();
        
        // Lire maxCryptSize à l'offset 84
        reader.BaseStream.Position = 84;
        var maxCryptSize = reader.ReadUInt32();

        // Déchiffrement et décompression
        reader.BaseStream.Position = envelopeLength;
        var payloadLength = fs.Length - envelopeLength;
        var cryptSize = maxCryptSize > 0 && maxCryptSize < payloadLength 
            ? maxCryptSize 
            : (payloadLength / 16) * 16;

        var encryptedData = reader.ReadBytes((int)cryptSize);
        var decryptedCompressed = Decrypt(encryptedData);
        
        var remaining = reader.ReadBytes((int)(payloadLength - cryptSize));
        var fullCompressed = decryptedCompressed.Concat(remaining).ToArray();
        
        // Décompression zlib
        using var inputStream = new MemoryStream(fullCompressed);
        using var zlibStream = new InflaterInputStream(inputStream);
        using var outputStream = new MemoryStream();
        zlibStream.CopyTo(outputStream);
        
        return (envelopeLength, maxCryptSize, outputStream.ToArray());
    }

    /// <summary>
    /// Vérifie si les données semblent être chiffrées
    /// </summary>
    public static bool IsEncrypted(byte[] data)
    {
        if (data.Length < 16) return false;
        
        // Tentative de déchiffrement et vérification de patterns
        var decrypted = Decrypt(data.Take(16).ToArray());
        
        // Vérifier si on obtient des valeurs ASCII ou des patterns reconnaissables
        // Les données déchiffrées devraient avoir plus de sens
        return true; // Pour l'instant, on assume que le fichier nécessite un déchiffrement
    }
}
