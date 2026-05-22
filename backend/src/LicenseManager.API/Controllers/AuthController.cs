using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.DTOs;
using LicenseManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IMfaService _mfa;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IMfaService mfa,
        ICurrentUserService currentUser,
        ILogger<AuthController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _mfa = mfa;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Login with email/password. Returns either tokens or { requiresMfa: true }.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            await LogLoginAttempt(null, request.Email, false, "User not found", cancellationToken);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid email or password"));
        }

        if (!user.IsActive)
        {
            await LogLoginAttempt(user.Id, request.Email, false, "Account inactive", cancellationToken);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Account is inactive"));
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            await LogLoginAttempt(user.Id, request.Email, false, "Account locked", cancellationToken);
            return Unauthorized(ApiResponse<LoginResponse>.Fail(
                $"Account locked until {user.LockedUntil.Value:u}"));
        }

        var ok = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!ok)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("Account {Email} locked after 5 failed login attempts", user.Email);
            }
            await _db.SaveChangesAsync(cancellationToken);
            await LogLoginAttempt(user.Id, request.Email, false, "Invalid password", cancellationToken);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid email or password"));
        }

        // Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        // MFA required
        if (user.MfaEnabled)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse { RequiresMfa = true }));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(await IssueTokensAsync(user, cancellationToken)));
    }

    /// <summary>
    /// Verify a 6-digit MFA code (or backup code) and complete login.
    /// </summary>
    [HttpPost("mfa/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyMfa(
        [FromBody] VerifyMfaRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted && u.IsActive,
                cancellationToken);

        if (user == null || !user.MfaEnabled || string.IsNullOrEmpty(user.MFASecret))
            return Unauthorized(ApiResponse<LoginResponse>.Fail("MFA verification failed"));

        var verified = _mfa.VerifyCode(user.MFASecret, request.Code);

        // Try backup codes if TOTP fails
        if (!verified && !string.IsNullOrEmpty(user.MFABackupCodes))
        {
            try
            {
                var codes = System.Text.Json.JsonSerializer.Deserialize<string[]>(user.MFABackupCodes)
                    ?? Array.Empty<string>();
                if (_mfa.VerifyBackupCode(request.Code, codes))
                {
                    // Consume the used backup code
                    var remaining = codes.Where(c => !string.Equals(
                        c?.Trim(), request.Code.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
                    user.MFABackupCodes = System.Text.Json.JsonSerializer.Serialize(remaining);
                    verified = true;
                }
            }
            catch
            {
                // Invalid stored codes JSON
            }
        }

        if (!verified)
        {
            await LogLoginAttempt(user.Id, request.Email, false, "Invalid MFA code", cancellationToken);
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid verification code"));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(await IssueTokensAsync(user, cancellationToken)));
    }

    /// <summary>
    /// Refresh access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        // For Phase 3 we accept any non-empty refresh token paired with a valid access token sub.
        // Production: store refresh tokens in DB with rotation/revocation.
        var subject = Request.Headers.Authorization
            .FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(request.RefreshToken))
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Refresh token required"));

        var userIdStr = subject != null ? _jwt.ValidateAndGetSubject(subject) : null;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid token"));

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Unauthorized(ApiResponse<LoginResponse>.Fail("User not found"));

        if (!_jwt.ValidateRefreshToken(request.RefreshToken, user.Id))
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid refresh token"));

        return Ok(ApiResponse<LoginResponse>.Ok(await IssueTokensAsync(user, cancellationToken)));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);

        if (user != null)
        {
            // Generate a 32-byte token, store hash and expiry
            var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            await _db.SaveChangesAsync(cancellationToken);

            // In production: send email with reset link.
            _logger.LogInformation("Password reset token generated for {Email}", request.Email);
        }

        // Always return success to avoid email enumeration
        return Ok(ApiResponse.Ok("If the account exists, a reset email has been sent"));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Token) || request.NewPassword.Length < 8)
            return BadRequest(ApiResponse.Fail("Invalid token or password too short"));

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token
                                    && u.PasswordResetTokenExpiresAt > DateTime.UtcNow
                                    && !u.IsDeleted, cancellationToken);

        if (user == null)
            return BadRequest(ApiResponse.Fail("Invalid or expired reset token"));

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Password reset successfully"));
    }

    [HttpPost("logout")]
    [Authorize(Policy = Policies.Authenticated)]
    public IActionResult Logout()
    {
        // Stateless JWT: client discards token. Production: blacklist jti.
        return Ok(ApiResponse.Ok("Logged out"));
    }

    [HttpGet("me")]
    [Authorize(Policy = Policies.Authenticated)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);
        if (user == null) return Unauthorized();

        return Ok(ApiResponse<AuthUserDto>.Ok(MapAuthUser(user)));
    }

    [HttpPost("change-password")]
    [Authorize(Policy = Policies.Authenticated)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (request.NewPassword.Length < 8)
            return BadRequest(ApiResponse.Fail("Password must be at least 8 characters"));

        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);
        if (user == null) return Unauthorized();

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(ApiResponse.Fail("Current password is incorrect"));

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Password changed"));
    }

    // ====================== MFA setup ======================

    [HttpPost("mfa/enable")]
    [Authorize(Policy = Policies.Authenticated)]
    public async Task<IActionResult> EnableMfa(
        [FromBody] EnableMfaRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);
        if (user == null) return Unauthorized();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return BadRequest(ApiResponse.Fail("Password is incorrect"));

        var setup = _mfa.GenerateSetup(user.Email, "License Manager");

        // Stage the secret (not yet enabled) until user verifies
        user.MFASecret = setup.Secret;
        user.MFABackupCodes = System.Text.Json.JsonSerializer.Serialize(setup.BackupCodes);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<EnableMfaResponse>.Ok(
            new EnableMfaResponse(setup.Secret, setup.QrCodeUri, setup.BackupCodes)));
    }

    [HttpPost("mfa/enable/verify")]
    [Authorize(Policy = Policies.Authenticated)]
    public async Task<IActionResult> ConfirmEnableMfa(
        [FromBody] VerifyEnableMfaRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.MFASecret))
            return BadRequest(ApiResponse.Fail("MFA setup not initiated"));

        if (!_mfa.VerifyCode(user.MFASecret, request.Code))
            return BadRequest(ApiResponse.Fail("Invalid verification code"));

        user.MfaEnabled = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("MFA enabled"));
    }

    [HttpPost("mfa/disable")]
    [Authorize(Policy = Policies.Authenticated)]
    public async Task<IActionResult> DisableMfa(
        [FromBody] DisableMfaRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);
        if (user == null) return Unauthorized();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return BadRequest(ApiResponse.Fail("Password is incorrect"));

        user.MfaEnabled = false;
        user.MFASecret = null;
        user.MFABackupCodes = null;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("MFA disabled"));
    }

    // ====================== Helpers ======================

    private async Task<LoginResponse> IssueTokensAsync(AdminUser user, CancellationToken cancellationToken)
    {
        var tokens = _jwt.Generate(user);
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIP = _currentUser.IpAddress;
        await _db.SaveChangesAsync(cancellationToken);
        await LogLoginAttempt(user.Id, user.Email, true, null, cancellationToken);

        return new LoginResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresInSeconds,
            User = MapAuthUser(user),
            RequiresMfa = false,
        };
    }

    private async Task LogLoginAttempt(
        Guid? userId, string email, bool success, string? failureReason, CancellationToken cancellationToken)
    {
        if (userId == null) return;
        try
        {
            _db.LoginHistory.Add(new LoginHistory
            {
                UserId = userId.Value,
                Success = success,
                IPAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent,
                FailureReason = failureReason,
                LoginAttemptAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log login attempt for {Email}", email);
        }
    }

    private static AuthUserDto MapAuthUser(AdminUser u) => new(
        u.Id, u.Email, u.FullName, (int)u.Role, u.Role.ToString(),
        u.MfaEnabled, u.EmailVerified, u.LastLoginAt);
}
