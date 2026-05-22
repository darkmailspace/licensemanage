using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Common.Models;
using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/licenses")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class LicensesController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(
        ILicenseService licenseService,
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ILogger<LicensesController> logger)
    {
        _licenseService = licenseService;
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// List licenses (paginated). Anyone with admin/support role can read.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] int? status,
        [FromQuery] int? licenseType,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? productId,
        CancellationToken cancellationToken)
    {
        var query = _db.Licenses.AsNoTracking().Where(l => !l.IsDeleted);

        if (status.HasValue) query = query.Where(l => (int)l.Status == status.Value);
        if (licenseType.HasValue) query = query.Where(l => (int)l.LicenseType == licenseType.Value);
        if (customerId.HasValue) query = query.Where(l => l.CustomerId == customerId.Value);
        if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(l =>
                l.LicenseKey.ToLower().Contains(s) ||
                (l.Customer != null && l.Customer.Email.ToLower().Contains(s)) ||
                (l.Customer != null && l.Customer.Name.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortBy?.ToLower() switch
        {
            "expiry" => pagination.IsDescending
                ? query.OrderByDescending(l => l.ExpiryDate)
                : query.OrderBy(l => l.ExpiryDate),
            "price" => pagination.IsDescending
                ? query.OrderByDescending(l => l.Price)
                : query.OrderBy(l => l.Price),
            _ => pagination.IsDescending
                ? query.OrderByDescending(l => l.CreatedAt)
                : query.OrderBy(l => l.CreatedAt),
        };

        var items = await query
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(l => new
            {
                l.Id,
                l.LicenseKey,
                l.LicenseType,
                l.Status,
                l.StartDate,
                l.ExpiryDate,
                l.Price,
                l.Currency,
                l.AutoRenewal,
                Customer = l.Customer == null ? null : new
                {
                    l.Customer.Id,
                    l.Customer.Name,
                    l.Customer.Email,
                    l.Customer.CompanyName,
                },
                Product = l.Product == null ? null : new
                {
                    l.Product.Id,
                    l.Product.Name,
                    l.Product.Version,
                },
                ActiveDomains = l.Domains.Count(d => d.IsActive && !d.IsDeleted),
                ActiveDevices = l.Devices.Count(d => d.IsActive && !d.IsDeactivated && !d.IsDeleted),
                l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<object>>.Ok(
            PagedResult<object>.Create(items.Cast<object>().ToList(), total,
                pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var license = await _db.Licenses
            .AsNoTracking()
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Include(l => l.Domains.Where(d => !d.IsDeleted))
            .Include(l => l.Devices.Where(d => !d.IsDeleted))
            .Include(l => l.FeatureMappings.Where(f => !f.IsDeleted))
                .ThenInclude(f => f.Feature)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);

        if (license == null)
            return NotFound(ApiResponse.Fail("License not found"));

        return Ok(ApiResponse<object>.Ok(license));
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken)
    {
        var history = await _db.LicenseHistory
            .AsNoTracking()
            .Where(h => h.LicenseId == id && !h.IsDeleted)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.Id,
                h.Action,
                h.Description,
                h.PreviousStatus,
                h.NewStatus,
                h.PerformedBy,
                h.IPAddress,
                h.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(history));
    }

    /// <summary>
    /// Generate a new license. Admin+ only.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var license = await _licenseService.GenerateLicenseAsync(
                request.CustomerId,
                request.ProductId,
                request.LicenseType,
                request.Configuration,
                cancellationToken);

            return Ok(ApiResponse<object>.Ok(new
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
                license.MaxDomains,
            }, "License generated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating license");
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Validate a license. Anonymous endpoint used by client applications.
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var isValid = await _licenseService.ValidateLicenseAsync(
            request.LicenseKey,
            request.DomainName,
            request.DeviceFingerprint,
            request.IPAddress ?? _currentUser.IpAddress,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            licenseKey = request.LicenseKey,
            isValid,
            validatedAt = DateTime.UtcNow,
        }));
    }

    /// <summary>
    /// Activate a license. Anonymous endpoint used by client applications.
    /// </summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var license = await _licenseService.ActivateLicenseAsync(
                request.LicenseKey,
                request.ActivationToken,
                request.DomainName,
                request.DeviceFingerprint,
                request.Metadata,
                cancellationToken);

            if (license == null)
                return BadRequest(ApiResponse.Fail(
                    "License activation failed. Check key, token, and limits."));

            return Ok(ApiResponse<object>.Ok(new
            {
                license.Id,
                license.LicenseKey,
                license.Status,
                license.ActivatedAt,
                license.ExpiryDate,
            }, "License activated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating license");
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/renew")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Renew(
        Guid id,
        [FromBody] RenewLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var license = await _licenseService.RenewLicenseAsync(id, request.RenewalMonths, cancellationToken);
        if (license == null)
            return NotFound(ApiResponse.Fail("License not found"));

        return Ok(ApiResponse<object>.Ok(new
        {
            license.Id,
            license.LicenseKey,
            license.ExpiryDate,
            renewalMonths = request.RenewalMonths,
        }, "License renewed"));
    }

    [HttpPost("{id}/suspend")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Suspend(
        Guid id,
        [FromBody] SuspendLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var ok = await _licenseService.SuspendLicenseAsync(id, request.Reason, cancellationToken);
        if (!ok) return NotFound(ApiResponse.Fail("License not found"));
        return Ok(ApiResponse.Ok("License suspended"));
    }

    [HttpPost("{id}/revoke")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Revoke(
        Guid id,
        [FromBody] RevokeLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var ok = await _licenseService.RevokeLicenseAsync(id, request.Reason, cancellationToken);
        if (!ok) return NotFound(ApiResponse.Fail("License not found"));
        return Ok(ApiResponse.Ok("License revoked"));
    }

    [HttpPost("{id}/upgrade")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Upgrade(
        Guid id,
        [FromBody] UpgradeLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var license = await _licenseService.UpgradeLicenseAsync(id, request.NewLicenseType, cancellationToken);
        if (license == null) return NotFound(ApiResponse.Fail("License not found"));
        return Ok(ApiResponse<object>.Ok(new { license.Id, license.LicenseType }, "License upgraded"));
    }

    [HttpPost("{id}/features/{featureId}/enable")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> EnableFeature(
        Guid id,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var mapping = await _db.LicenseFeatureMappings
            .FirstOrDefaultAsync(m => m.LicenseId == id && m.FeatureId == featureId && !m.IsDeleted,
                cancellationToken);

        if (mapping == null)
        {
            mapping = new LicenseFeatureMapping
            {
                LicenseId = id,
                FeatureId = featureId,
                IsEnabled = true,
                EnabledAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Email,
            };
            _db.LicenseFeatureMappings.Add(mapping);
        }
        else
        {
            mapping.IsEnabled = true;
            mapping.EnabledAt = DateTime.UtcNow;
            mapping.DisabledAt = null;
            mapping.DisabledBy = null;
            mapping.UpdatedBy = _currentUser.Email;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Feature enabled"));
    }

    [HttpPost("{id}/features/{featureId}/disable")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> DisableFeature(
        Guid id,
        Guid featureId,
        [FromBody] DisableFeatureRequest? request,
        CancellationToken cancellationToken = default)
    {
        var mapping = await _db.LicenseFeatureMappings
            .FirstOrDefaultAsync(m => m.LicenseId == id && m.FeatureId == featureId && !m.IsDeleted,
                cancellationToken);
        if (mapping == null) return NotFound(ApiResponse.Fail("Feature mapping not found"));

        mapping.IsEnabled = false;
        mapping.DisabledAt = DateTime.UtcNow;
        mapping.DisabledBy = _currentUser.Email;
        mapping.DisabledReason = request?.Reason;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Feature disabled"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.SuperAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var license = await _db.Licenses
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        if (license == null) return NotFound(ApiResponse.Fail("License not found"));

        license.IsDeleted = true;
        license.DeletedAt = DateTime.UtcNow;
        license.UpdatedBy = _currentUser.Email;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("License deleted"));
    }
}

// Request DTOs for LicensesController
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
public record UpgradeLicenseRequest(LicenseType NewLicenseType);
public record DisableFeatureRequest(string? Reason);
