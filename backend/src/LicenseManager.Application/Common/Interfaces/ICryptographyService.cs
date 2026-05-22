namespace LicenseManager.Application.Common.Interfaces;

public interface ICryptographyService
{
    // RSA-4096 Operations
    (string publicKey, string privateKey) GenerateRsaKeyPair();
    string SignData(string data, string privateKey);
    bool VerifySignature(string data, string signature, string publicKey);
    string EncryptWithPublicKey(string data, string publicKey);
    string DecryptWithPrivateKey(string encryptedData, string privateKey);
    
    // AES-256 Operations
    string GenerateAesKey();
    string EncryptAes(string plainText, string key);
    string DecryptAes(string cipherText, string key);
    
    // Hash Operations
    string ComputeSha256Hash(string input);
    string ComputeHmacSha256(string data, string key);
    
    // License Key Generation
    string GenerateLicenseKey();
    string GenerateActivationToken();
    
    // Device Fingerprint
    string GenerateDeviceFingerprint(Dictionary<string, string> deviceInfo);
}
