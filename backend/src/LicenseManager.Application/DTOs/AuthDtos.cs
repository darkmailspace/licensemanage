namespace LicenseManager.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record VerifyMfaRequest(string Email, string Code);
public record RefreshTokenRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AuthUserDto(
    Guid Id,
    string Email,
    string FullName,
    int Role,
    string RoleName,
    bool MfaEnabled,
    bool EmailVerified,
    DateTime? LastLoginAt);

public record LoginResponse
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int? ExpiresIn { get; init; }
    public AuthUserDto? User { get; init; }
    public bool RequiresMfa { get; init; }
}

public record EnableMfaRequest(string Password);
public record EnableMfaResponse(string Secret, string QrCodeUri, string[] BackupCodes);
public record VerifyEnableMfaRequest(string Code);
public record DisableMfaRequest(string Password);
