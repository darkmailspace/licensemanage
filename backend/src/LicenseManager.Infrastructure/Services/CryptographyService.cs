using System.Security.Cryptography;
using System.Text;
using LicenseManager.Application.Common.Interfaces;

namespace LicenseManager.Infrastructure.Services;

public class CryptographyService : ICryptographyService
{
    private const int RsaKeySize = 4096;
    private const int AesKeySize = 256;

    #region RSA-4096 Operations

    public (string publicKey, string privateKey) GenerateRsaKeyPair()
    {
        using var rsa = RSA.Create(RsaKeySize);
        
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        
        return (publicKey, privateKey);
    }

    public string SignData(string data, string privateKey)
    {
        using var rsa = RSA.Create(RsaKeySize);
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return Convert.ToBase64String(signature);
    }

    public bool VerifySignature(string data, string signature, string publicKey)
    {
        try
        {
            using var rsa = RSA.Create(RsaKeySize);
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);
            
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    public string EncryptWithPublicKey(string data, string publicKey)
    {
        using var rsa = RSA.Create(RsaKeySize);
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var encryptedData = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
        
        return Convert.ToBase64String(encryptedData);
    }

    public string DecryptWithPrivateKey(string encryptedData, string privateKey)
    {
        using var rsa = RSA.Create(RsaKeySize);
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        var decryptedData = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
        
        return Encoding.UTF8.GetString(decryptedData);
    }

    #endregion

    #region AES-256 Operations

    public string GenerateAesKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = AesKeySize;
        aes.GenerateKey();
        
        return Convert.ToBase64String(aes.Key);
    }

    public string EncryptAes(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.KeySize = AesKeySize;
        aes.Key = Convert.FromBase64String(key);
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
        
        return Convert.ToBase64String(result);
    }

    public string DecryptAes(string cipherText, string key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.KeySize = AesKeySize;
        aes.Key = Convert.FromBase64String(key);
        
        // Extract IV from the beginning
        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];
        
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    #endregion

    #region Hash Operations

    public string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        
        return Convert.ToBase64String(hash);
    }

    public string ComputeHmacSha256(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        
        return Convert.ToBase64String(hash);
    }

    #endregion

    #region License Key Generation

    public string GenerateLicenseKey()
    {
        var parts = new string[4];
        
        for (int i = 0; i < 4; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(4);
            parts[i] = BitConverter.ToString(bytes).Replace("-", "");
        }
        
        return $"LK-{parts[0]}-{parts[1]}-{parts[2]}-{parts[3]}";
    }

    public string GenerateActivationToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return $"AT-{BitConverter.ToString(bytes).Replace("-", "")}";
    }

    #endregion

    #region Device Fingerprint

    public string GenerateDeviceFingerprint(Dictionary<string, string> deviceInfo)
    {
        var sb = new StringBuilder();
        
        foreach (var kvp in deviceInfo.OrderBy(x => x.Key))
        {
            sb.Append($"{kvp.Key}:{kvp.Value};");
        }
        
        var fingerprintData = sb.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintData));
        
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    #endregion
}
