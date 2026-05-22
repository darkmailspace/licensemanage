using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Common.Models;
using LicenseManager.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/validations")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class ValidationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public ValidationsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? isValid,
        [FromQuery] bool? isHeartbeat,
        [FromQuery] int? validationResult,
        [FromQuery] Guid? licenseId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = _db.LicenseValidations.AsNoTracking().Where(v => !v.IsDeleted);

        if (isValid.HasValue) query = query.Where(v => v.IsValid == isValid.Value);
        if (isHeartbeat.HasValue) query = query.Where(v => v.IsHeartbeat == isHeartbeat.Value);
        if (validationResult.HasValue)
            query = query.Where(v => (int)v.ValidationResult == validationResult.Value);
        if (licenseId.HasValue) query = query.Where(v => v.LicenseId == licenseId.Value);
        if (from.HasValue) query = query.Where(v => v.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(v => v.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(v =>
                (v.DomainName != null && v.DomainName.ToLower().Contains(s)) ||
                (v.IPAddress != null && v.IPAddress.Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(v => v.License!).ThenInclude(l => l.Customer)
            .OrderByDescending(v => v.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(v => new ValidationListItem(
                v.Id, v.LicenseId, v.License!.LicenseKey,
                v.License.Customer != null ? v.License.Customer.Name : "Unknown",
                (int)v.ValidationResult, v.IsValid, v.ValidationMessage,
                v.DomainName, v.IPAddress, v.Country, v.ProductVersion,
                v.IsHeartbeat, v.ResponseTimeMs, v.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<ValidationListItem>>.Ok(
            PagedResult<ValidationListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Max(1, hours));

        var total = await _db.LicenseValidations
            .CountAsync(v => v.CreatedAt >= since && !v.IsDeleted, cancellationToken);
        var valid = await _db.LicenseValidations
            .CountAsync(v => v.CreatedAt >= since && v.IsValid && !v.IsDeleted, cancellationToken);
        var avgMs = total == 0 ? 0 : await _db.LicenseValidations
            .Where(v => v.CreatedAt >= since && !v.IsDeleted)
            .AverageAsync(v => (double?)v.ResponseTimeMs, cancellationToken) ?? 0;

        return Ok(ApiResponse<object>.Ok(new
        {
            total,
            valid,
            failed = total - valid,
            avgResponseMs = Math.Round(avgMs, 1),
            successRate = total == 0 ? 0 : Math.Round(100m * valid / total, 2),
        }));
    }
}
