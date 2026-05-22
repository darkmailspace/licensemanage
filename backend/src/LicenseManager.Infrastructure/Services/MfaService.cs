using System.Security.Cryptography;
using LicenseManager.Application.Common.Interfaces;
using OtpNet;

namespace LicenseManager.Infrastructure.Services;

/// <summary>
/// TOTP-based multi-factor authentication using OtpNet.
/// Compatible with Google Authenticator, Authy, 1Password, etc.
/// </summary>
public class MfaService : IMfaService
{
    public MfaSetupResult GenerateSetup(string userEmail, string issuer)
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);

        // otpauth URI used to generate QR codes in clients (e.g., Google Auth)
        var qrUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(userEmail)}" +
                    $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

        var backupCodes = GenerateBackupCodes();

        return new MfaSetupResult(secret, qrUri, backupCodes);
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);
            // Allow 1 step in either direction for clock skew tolerance
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    public string[] GenerateBackupCodes(int count = 10)
    {
        var codes = new string[count];
        for (int i = 0; i < count; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(5);
            // 5 bytes -> 10 hex chars formatted as XXXXX-XXXXX
            var hex = Convert.ToHexString(bytes);
            codes[i] = $"{hex.Substring(0, 5)}-{hex.Substring(5, 5)}";
        }
        return codes;
    }

    public bool VerifyBackupCode(string code, IEnumerable<string> validCodes)
    {
        if (string.IsNullOrEmpty(code)) return false;
        var normalized = code.Trim().ToUpperInvariant();
        return validCodes.Any(v => string.Equals(v?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
    }
}
