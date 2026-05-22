using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.DTOs;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class DashboardController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public DashboardController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var since24h = now.AddHours(-24);
        var threshold30d = now.AddDays(30);

        var licenses = await _db.Licenses
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .GroupBy(l => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(l => l.Status == LicenseStatus.Active),
                Expired = g.Count(l => l.Status == LicenseStatus.Expired || l.ExpiryDate < now),
                ExpiringSoon = g.Count(l => l.Status == LicenseStatus.Active &&
                                            l.ExpiryDate > now && l.ExpiryDate <= threshold30d),
                Revenue = g.Sum(l => (decimal?)l.Price) ?? 0m,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var customerCount = await _db.Customers.CountAsync(c => !c.IsDeleted, cancellationToken);
        var productCount = await _db.Products.CountAsync(p => !p.IsDeleted, cancellationToken);

        var activations24h = await _db.LicenseActivations
            .CountAsync(a => a.CreatedAt >= since24h && !a.IsDeleted, cancellationToken);
        var successful24h = await _db.LicenseActivations
            .CountAsync(a => a.CreatedAt >= since24h && a.Success && !a.IsDeleted, cancellationToken);
        var validations24h = await _db.LicenseValidations
            .CountAsync(v => v.CreatedAt >= since24h && !v.IsDeleted, cancellationToken);
        var validValidations24h = await _db.LicenseValidations
            .CountAsync(v => v.CreatedAt >= since24h && v.IsValid && !v.IsDeleted, cancellationToken);

        var stats = new DashboardStatsDto(
            TotalLicenses: licenses?.Total ?? 0,
            ActiveLicenses: licenses?.Active ?? 0,
            ExpiredLicenses: licenses?.Expired ?? 0,
            ExpiringIn30Days: licenses?.ExpiringSoon ?? 0,
            TotalCustomers: customerCount,
            TotalProducts: productCount,
            TotalRevenue: licenses?.Revenue ?? 0m,
            Currency: "USD",
            Activations24h: activations24h,
            SuccessfulActivations24h: successful24h,
            Validations24h: validations24h,
            SuccessRate: validations24h == 0 ? 0 : Math.Round(100m * validValidations24h / validations24h, 2));

        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> RecentActivity(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var history = await _db.LicenseHistory
            .AsNoTracking()
            .Where(h => !h.IsDeleted)
            .Include(h => h.License!).ThenInclude(l => l.Customer)
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .Select(h => new
            {
                id = h.Id,
                action = h.Action,
                description = h.Description,
                licenseId = h.LicenseId,
                licenseKey = h.License!.LicenseKey,
                customerName = h.License.Customer != null ? h.License.Customer.Name : null,
                previousStatus = h.PreviousStatus,
                newStatus = h.NewStatus,
                performedBy = h.PerformedBy,
                createdAt = h.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(history));
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] string period = "12months",
        CancellationToken cancellationToken = default)
    {
        var (since, format) = period.ToLower() switch
        {
            "7days" => (DateTime.UtcNow.AddDays(-7), "yyyy-MM-dd"),
            "30days" => (DateTime.UtcNow.AddDays(-30), "yyyy-MM-dd"),
            "90days" => (DateTime.UtcNow.AddDays(-90), "yyyy-'W'ww"),
            _ => (DateTime.UtcNow.AddMonths(-12), "yyyy-MM"),
        };

        // Pull rows then group in-memory (date_trunc differs by provider).
        var rows = await _db.Licenses
            .AsNoTracking()
            .Where(l => l.CreatedAt >= since && !l.IsDeleted)
            .Select(l => new { l.CreatedAt, l.Price })
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(r => r.CreatedAt.ToString(format))
            .Select(g => new TimeSeriesPoint(g.Key, g.Sum(x => x.Price), g.Count()))
            .OrderBy(p => p.Period)
            .ToList();

        return Ok(ApiResponse<object>.Ok(grouped));
    }

    [HttpGet("licenses")]
    public async Task<IActionResult> LicensesByPeriod(
        [FromQuery] string period = "12months",
        CancellationToken cancellationToken = default)
    {
        var since = period.ToLower() switch
        {
            "7days" => DateTime.UtcNow.AddDays(-7),
            "30days" => DateTime.UtcNow.AddDays(-30),
            "90days" => DateTime.UtcNow.AddDays(-90),
            _ => DateTime.UtcNow.AddMonths(-12),
        };

        var rows = await _db.Licenses
            .AsNoTracking()
            .Where(l => l.CreatedAt >= since && !l.IsDeleted)
            .Select(l => new { l.CreatedAt, l.Status })
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(r => r.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                period = g.Key,
                total = g.Count(),
                active = g.Count(x => x.Status == LicenseStatus.Active),
                expired = g.Count(x => x.Status == LicenseStatus.Expired),
                revoked = g.Count(x => x.Status == LicenseStatus.Revoked),
            })
            .ToList();

        return Ok(ApiResponse<object>.Ok(grouped));
    }
}
