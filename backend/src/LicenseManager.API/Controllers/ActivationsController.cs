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
[Route("api/activations")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class ActivationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public ActivationsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? success,
        [FromQuery] int? activationType,
        [FromQuery] Guid? licenseId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = _db.LicenseActivations.AsNoTracking().Where(a => !a.IsDeleted);

        if (success.HasValue) query = query.Where(a => a.Success == success.Value);
        if (activationType.HasValue)
            query = query.Where(a =>
                (int)a.ActivationType == activationType.Value);
        if (licenseId.HasValue) query = query.Where(a => a.LicenseId == licenseId.Value);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(a =>
                (a.DomainName != null && a.DomainName.ToLower().Contains(s)) ||
                (a.IPAddress != null && a.IPAddress.Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(a => a.License!).ThenInclude(l => l.Customer)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(a => new ActivationListItem(
                a.Id, a.LicenseId, a.License!.LicenseKey,
                a.License.Customer != null ? a.License.Customer.Name : "Unknown",
                (int)a.ActivationType, a.Success, a.FailureReason,
                a.DomainName, a.IPAddress, a.Country, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<ActivationListItem>>.Ok(
            PagedResult<ActivationListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var activation = await _db.LicenseActivations
            .AsNoTracking()
            .Include(a => a.License!).ThenInclude(l => l.Customer)
            .Include(a => a.License!).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, cancellationToken);
        if (activation == null) return NotFound(ApiResponse.Fail("Activation not found"));
        return Ok(ApiResponse<object>.Ok(activation));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Max(1, days));

        var total = await _db.LicenseActivations
            .CountAsync(a => a.CreatedAt >= since && !a.IsDeleted, cancellationToken);
        var successful = await _db.LicenseActivations
            .CountAsync(a => a.CreatedAt >= since && a.Success && !a.IsDeleted, cancellationToken);

        var byDay = await _db.LicenseActivations
            .Where(a => a.CreatedAt >= since && !a.IsDeleted)
            .GroupBy(a => new { Date = a.CreatedAt.Date, a.Success })
            .Select(g => new { g.Key.Date, g.Key.Success, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            total,
            successful,
            failed = total - successful,
            successRate = total == 0 ? 0 : Math.Round(100m * successful / total, 2),
            byDay,
        }));
    }
}
