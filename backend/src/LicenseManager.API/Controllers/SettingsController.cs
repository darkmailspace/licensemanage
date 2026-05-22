using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.DTOs;
using LicenseManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

/// <summary>
/// Manages key/value system settings (SMTP, security, API).
/// Secret values are masked in responses unless explicitly requested.
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize(Policy = Policies.SuperAdmin)]
public class SettingsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SettingsController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] bool revealSecrets = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SystemSettings.AsNoTracking().Where(s => !s.IsDeleted);
        if (!string.IsNullOrEmpty(category)) query = query.Where(s => s.Category == category);

        var settings = await query
            .OrderBy(s => s.Category).ThenBy(s => s.Key)
            .Select(s => new SettingDto(
                s.Key,
                s.IsSecret && !revealSecrets ? MaskValue(s.Value) : s.Value,
                s.Category,
                s.Description,
                s.IsSecret,
                s.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(settings));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);
        if (setting == null) return NotFound(ApiResponse.Fail("Setting not found"));

        return Ok(ApiResponse<SettingDto>.Ok(new SettingDto(
            setting.Key,
            setting.IsSecret ? MaskValue(setting.Value) : setting.Value,
            setting.Category, setting.Description, setting.IsSecret, setting.UpdatedAt)));
    }

    /// <summary>
    /// Update multiple settings at once. Creates missing keys.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateBulk(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Settings == null || request.Settings.Count == 0)
            return BadRequest(ApiResponse.Fail("No settings provided"));

        var keys = request.Settings.Keys.ToList();
        var existing = await _db.SystemSettings
            .Where(s => keys.Contains(s.Key) && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var (key, value) in request.Settings)
        {
            var setting = existing.FirstOrDefault(s => s.Key == key);
            if (setting == null)
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Category = InferCategory(key),
                    CreatedBy = _currentUser.Email,
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedBy = _currentUser.Email;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok($"{request.Settings.Count} settings updated"));
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSingle(
        string key,
        [FromBody] UpdateSingleSettingRequest request,
        CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = key,
                Value = request.Value,
                Category = InferCategory(key),
                CreatedBy = _currentUser.Email,
            };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = request.Value;
            setting.UpdatedBy = _currentUser.Email;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Setting updated"));
    }

    [HttpPost("test-smtp")]
    public async Task<IActionResult> TestSmtp(
        [FromBody] TestSmtpRequest request,
        CancellationToken cancellationToken)
    {
        // Placeholder: in production wire to a real SMTP send
        await Task.CompletedTask;
        return Ok(ApiResponse.Ok($"Test email queued for {request.ToEmail}"));
    }

    private static string MaskValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= 4) return new string('*', value.Length);
        return new string('*', value.Length - 4) + value[^4..];
    }

    private static string InferCategory(string key)
    {
        var dot = key.IndexOf('.');
        return dot > 0 ? key[..dot] : "general";
    }
}

public record UpdateSingleSettingRequest(string? Value);
public record TestSmtpRequest(string ToEmail);
