namespace LicenseManager.Application.Common.Interfaces;

public record MfaSetupResult(string Secret, string QrCodeUri, string[] BackupCodes);

public interface IMfaService
{
    MfaSetupResult GenerateSetup(string userEmail, string issuer);
    bool VerifyCode(string secret, string code);
    string[] GenerateBackupCodes(int count = 10);
    bool VerifyBackupCode(string code, IEnumerable<string> validCodes);
}
