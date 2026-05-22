using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicensesController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(
        ILicenseService licenseService,
        IApplicationDbContext context,
        ILogger<LicensesController> logger)
    {
        _licenseService = licenseService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new license
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GenerateLicense([FromBody] GenerateLicenseRequest request)
    {
        try
        {
            var license = await _licenseService.GenerateLicenseAsync(
                request.CustomerId,
                request.ProductId,
                request.LicenseType,
                request.Configuration);

            return Ok(new
            {
                success = true,
                data = new
                {
                    license.Id,
                    license.LicenseKey,
                    license.ActivationToken,
                    license.Status,
                    license.LicenseType,
                    license.StartDate,
                    license.ExpiryDate,
                    license.MaxUsers,
                    license.MaxBranches,
                    license.MaxDevices,
                    license.MaxDomains
                },
                message = "License generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating license");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Validate a license
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateLicense([FromBody] ValidateLicenseRequest request)
    {
        var isValid = await _licenseService.ValidateLicenseAsync(
            request.LicenseKey,
            request.DomainName,
            request.DeviceFingerprint,
            request.IPAddress);

        return Ok(new
        {
            success = true,
            data = new
            {
                licenseKey = request.LicenseKey,
                isValid,
                validatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Activate a license
    /// </summary>
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateLicense([FromBody] ActivateLicenseRequest request)
    {
        try
        {
            var license = await _licenseService.ActivateLicenseAsync(
                request.LicenseKey,
                request.ActivationToken,
                request.DomainName,
                request.DeviceFingerprint,
                request.Metadata);

            if (license == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "License activation failed. Please check your license key and activation token."
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    license.Id,
                    license.LicenseKey,
                    license.Status,
                    license.ActivatedAt,
                    license.ExpiryDate
                },
                message = "License activated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating license");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Renew a license
    /// </summary>
    [HttpPost("{id}/renew")]
    public async Task<IActionResult> RenewLicense(Guid id, [FromBody] RenewLicenseRequest request)
    {
        try
        {
            var license = await _licenseService.RenewLicenseAsync(id, request.RenewalMonths);

            if (license == null)
            {
                return NotFound(new { success = false, error = "License not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    license.Id,
                    license.LicenseKey,
                    license.ExpiryDate,
                    renewalMonths = request.RenewalMonths
                },
                message = "License renewed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing license");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Suspend a license
    /// </summary>
    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendLicense(Guid id, [FromBody] SuspendLicenseRequest request)
    {
        try
        {
            var result = await _licenseService.SuspendLicenseAsync(id, request.Reason);

            if (!result)
            {
                return NotFound(new { success = false, error = "License not found" });
            }

            return Ok(new
            {
                success = true,
                message = "License suspended successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending license");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke a license
    /// </summary>
    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> RevokeLicense(Guid id, [FromBody] RevokeLicenseRequest request)
    {
        try
        {
            var result = await _licenseService.RevokeLicenseAsync(id, request.Reason);

            if (!result)
            {
                return NotFound(new { success = false, error = "License not found" });
            }

            return Ok(new
            {
                success = true,
                message = "License revoked successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking license");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get license by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLicense(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);

        if (license == null || license.IsDeleted)
        {
            return NotFound(new { success = false, error = "License not found" });
        }

        return Ok(new
        {
            success = true,
            data = license
        });
    }
}

// Request DTOs
public record GenerateLicenseRequest(
    Guid CustomerId,
    Guid ProductId,
    LicenseType LicenseType,
    Dictionary<string, object>? Configuration = null);

public record ValidateLicenseRequest(
    string LicenseKey,
    string? DomainName = null,
    string? DeviceFingerprint = null,
    string? IPAddress = null);

public record ActivateLicenseRequest(
    string LicenseKey,
    string ActivationToken,
    string DomainName,
    string? DeviceFingerprint = null,
    Dictionary<string, object>? Metadata = null);

public record RenewLicenseRequest(int RenewalMonths);

public record SuspendLicenseRequest(string Reason);

public record RevokeLicenseRequest(string Reason);
