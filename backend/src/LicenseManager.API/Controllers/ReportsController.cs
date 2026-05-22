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
[Route("api/reports")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class ReportsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public ReportsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-12);
        var toDate = to ?? DateTime.UtcNow;

        var licenses = await _db.Licenses
            .AsNoTracking()
            .Where(l => l.CreatedAt >= fromDate && l.CreatedAt <= toDate && !l.IsDeleted)
            .Include(l => l.Product)
            .Select(l => new { l.CreatedAt, l.Price, l.LicenseType, ProductName = l.Product != null ? l.Product.Name : "Unknown" })
            .ToListAsync(cancellationToken);

        var totalRevenue = licenses.Sum(l => l.Price);
        var avgValue = licenses.Count == 0 ? 0m : Math.Round(totalRevenue / licenses.Count, 2);

        var byMonth = licenses
            .GroupBy(l => l.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(g.Key, g.Sum(x => x.Price), g.Count()))
            .ToList();

        // Month over month growth (last vs previous month)
        var growth = 0m;
        if (byMonth.Count >= 2)
        {
            var last = byMonth[^1].Value;
            var prev = byMonth[^2].Value;
            growth = prev == 0 ? 0 : Math.Round(100m * (last - prev) / prev, 2);
        }

        var byProduct = licenses
            .GroupBy(l => l.ProductName)
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), g.Sum(x => x.Price)))
            .OrderByDescending(b => b.Value)
            .ToList();

        var byLicenseType = licenses
            .GroupBy(l => l.LicenseType.ToString())
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), g.Sum(x => x.Price)))
            .OrderByDescending(b => b.Value)
            .ToList();

        var report = new RevenueReportDto(
            totalRevenue, avgValue, growth, "USD", byMonth, byProduct, byLicenseType);

        return Ok(ApiResponse<RevenueReportDto>.Ok(report));
    }

    [HttpGet("licenses")]
    public async Task<IActionResult> Licenses(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var threshold30d = now.AddDays(30);

        var licenses = await _db.Licenses
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .Select(l => new { l.CreatedAt, l.Status, l.LicenseType, l.ExpiryDate })
            .ToListAsync(cancellationToken);

        var byStatus = licenses
            .GroupBy(l => l.Status.ToString())
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), 0))
            .OrderByDescending(b => b.Count)
            .ToList();

        var byType = licenses
            .GroupBy(l => l.LicenseType.ToString())
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), 0))
            .OrderByDescending(b => b.Count)
            .ToList();

        var byMonth = licenses
            .GroupBy(l => l.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(g.Key, 0, g.Count()))
            .ToList();

        var renewed = await _db.LicenseHistory
            .AsNoTracking()
            .CountAsync(h => h.Action == "License Renewed" && !h.IsDeleted, cancellationToken);
        var expired = licenses.Count(l => l.Status == LicenseStatus.Expired);
        var renewalRate = expired == 0 ? 0m : Math.Round(100m * renewed / (renewed + expired), 2);

        var report = new LicenseReportDto(
            Total: licenses.Count,
            Active: licenses.Count(l => l.Status == LicenseStatus.Active),
            Expired: expired,
            Suspended: licenses.Count(l => l.Status == LicenseStatus.Suspended),
            Revoked: licenses.Count(l => l.Status == LicenseStatus.Revoked),
            ExpiringNext30Days: licenses.Count(l =>
                l.Status == LicenseStatus.Active && l.ExpiryDate > now && l.ExpiryDate <= threshold30d),
            RenewalRate: renewalRate,
            ByStatus: byStatus,
            ByType: byType,
            ByMonth: byMonth);

        return Ok(ApiResponse<LicenseReportDto>.Ok(report));
    }

    [HttpGet("activations")]
    public async Task<IActionResult> Activations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var rows = await _db.LicenseActivations
            .AsNoTracking()
            .Where(a => a.CreatedAt >= fromDate && a.CreatedAt <= toDate && !a.IsDeleted)
            .Select(a => new { a.CreatedAt, a.Success, a.Country, a.FailureReason })
            .ToListAsync(cancellationToken);

        var total = rows.Count;
        var successful = rows.Count(r => r.Success);
        var rate = total == 0 ? 0m : Math.Round(100m * successful / total, 2);

        var byDay = rows
            .GroupBy(r => r.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(g.Key, g.Count(r => r.Success), g.Count()))
            .ToList();

        var byCountry = rows
            .Where(r => !string.IsNullOrEmpty(r.Country))
            .GroupBy(r => r.Country!)
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), 0))
            .OrderByDescending(b => b.Count)
            .Take(20)
            .ToList();

        var byFailureReason = rows
            .Where(r => !r.Success && !string.IsNullOrEmpty(r.FailureReason))
            .GroupBy(r => r.FailureReason!)
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), 0))
            .OrderByDescending(b => b.Count)
            .ToList();

        var report = new ActivationReportDto(total, successful, total - successful, rate,
            byDay, byCountry, byFailureReason);
        return Ok(ApiResponse<ActivationReportDto>.Ok(report));
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddDays(Math.Max(1, days));

        var rows = await _db.Licenses
            .AsNoTracking()
            .Where(l => l.Status == LicenseStatus.Active &&
                        l.ExpiryDate > now && l.ExpiryDate <= threshold && !l.IsDeleted)
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .OrderBy(l => l.ExpiryDate)
            .Select(l => new
            {
                l.Id, l.LicenseKey,
                CustomerName = l.Customer != null ? l.Customer.Name : "Unknown",
                ProductName = l.Product != null ? l.Product.Name : "Unknown",
                l.ExpiryDate, l.AutoRenewal, l.Price,
                Currency = l.Currency ?? "USD",
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new ExpiryReportItem(
            r.Id, r.LicenseKey, r.CustomerName, r.ProductName,
            r.ExpiryDate, (int)(r.ExpiryDate - now).TotalDays,
            r.AutoRenewal, r.Price, r.Currency)).ToList();

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken cancellationToken)
    {
        var totalCustomers = await _db.Customers.CountAsync(c => !c.IsDeleted, cancellationToken);
        var activeCustomers = await _db.Customers
            .CountAsync(c => c.IsActive && !c.IsDeleted, cancellationToken);
        var verifiedCustomers = await _db.Customers
            .CountAsync(c => c.IsVerified && !c.IsDeleted, cancellationToken);

        var byCountry = await _db.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Country != null)
            .GroupBy(c => c.Country!)
            .Select(g => new CategoryBreakdown(g.Key, g.Count(), 0))
            .OrderByDescending(b => b.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var rows = await _db.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var byMonth = rows
            .GroupBy(d => d.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(g.Key, 0, g.Count()))
            .ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            totalCustomers,
            activeCustomers,
            verifiedCustomers,
            byCountry,
            byMonth,
        }));
    }
}
