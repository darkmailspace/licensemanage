using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Common.Models;
using LicenseManager.Application.DTOs;
using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class ProductsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IApplicationDbContext db, ICurrentUserService currentUser, ILogger<ProductsController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking().Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.ProductCode.ToLower().Contains(s));
        }
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(p => new ProductListItem(
                p.Id, p.ProductCode, p.Name, p.Description, p.Version,
                p.IsActive, p.BasePrice, p.Currency, p.TrialDays,
                _db.Licenses.Count(l => l.ProductId == p.Id && !l.IsDeleted),
                _db.Licenses.Count(l => l.ProductId == p.Id && l.Status == LicenseStatus.Active && !l.IsDeleted),
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<ProductListItem>>.Ok(
            PagedResult<ProductListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (product == null) return NotFound(ApiResponse.Fail("Product not found"));

        var features = await _db.ProductFeatures
            .AsNoTracking()
            .Where(pf => pf.ProductId == id && !pf.IsDeleted)
            .Include(pf => pf.Feature)
            .Select(pf => new
            {
                pf.FeatureId,
                pf.Feature!.FeatureCode,
                pf.Feature.Name,
                pf.Feature.Category,
                pf.IsDefaultEnabled,
                pf.IsOptional,
            })
            .ToListAsync(cancellationToken);

        var versions = await _db.ProductVersions
            .AsNoTracking()
            .Where(v => v.ProductId == id && !v.IsDeleted)
            .OrderByDescending(v => v.ReleasedAt)
            .Select(v => new
            {
                v.Id, v.Version, v.ReleaseNotes, v.ReleasedAt,
                v.IsStable, v.IsBeta, v.IsMajorUpdate, v.IsForced,
                v.FileSizeBytes,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { product, features, versions }));
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ProductCode) || string.IsNullOrEmpty(request.Name))
            return BadRequest(ApiResponse.Fail("Product code and name are required"));

        var exists = await _db.Products
            .AnyAsync(p => p.ProductCode == request.ProductCode && !p.IsDeleted, cancellationToken);
        if (exists) return BadRequest(ApiResponse.Fail("Product code already exists"));

        var product = new Product
        {
            ProductCode = request.ProductCode,
            Name = request.Name,
            Description = request.Description,
            Version = request.Version,
            BasePrice = request.BasePrice,
            Currency = request.Currency,
            TrialDays = request.TrialDays,
            AllowTrial = request.AllowTrial,
            MaxDevicesPerLicense = request.MaxDevicesPerLicense,
            MaxUsersPerLicense = request.MaxUsersPerLicense,
            MaxBranchesPerLicense = request.MaxBranchesPerLicense,
            RequireDomainLock = request.RequireDomainLock,
            RequireHardwareLock = request.RequireHardwareLock,
            GracePeriodDays = request.GracePeriodDays,
            ValidationIntervalHours = request.ValidationIntervalHours,
            ImageUrl = request.ImageUrl,
            IsActive = true,
            CreatedBy = _currentUser.Email,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { id = product.Id }, "Product created"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (product == null) return NotFound(ApiResponse.Fail("Product not found"));

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.Version != null) product.Version = request.Version;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
        if (request.BasePrice.HasValue) product.BasePrice = request.BasePrice.Value;
        if (request.Currency != null) product.Currency = request.Currency;
        if (request.TrialDays.HasValue) product.TrialDays = request.TrialDays.Value;
        if (request.AllowTrial.HasValue) product.AllowTrial = request.AllowTrial.Value;
        if (request.MaxDevicesPerLicense.HasValue) product.MaxDevicesPerLicense = request.MaxDevicesPerLicense.Value;
        if (request.MaxUsersPerLicense.HasValue) product.MaxUsersPerLicense = request.MaxUsersPerLicense.Value;
        if (request.MaxBranchesPerLicense.HasValue) product.MaxBranchesPerLicense = request.MaxBranchesPerLicense.Value;
        if (request.RequireDomainLock.HasValue) product.RequireDomainLock = request.RequireDomainLock.Value;
        if (request.RequireHardwareLock.HasValue) product.RequireHardwareLock = request.RequireHardwareLock.Value;
        if (request.GracePeriodDays.HasValue) product.GracePeriodDays = request.GracePeriodDays.Value;
        if (request.ValidationIntervalHours.HasValue) product.ValidationIntervalHours = request.ValidationIntervalHours.Value;
        if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl;
        product.UpdatedBy = _currentUser.Email;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Product updated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (product == null) return NotFound(ApiResponse.Fail("Product not found"));

        var hasLicenses = await _db.Licenses
            .AnyAsync(l => l.ProductId == id && !l.IsDeleted, cancellationToken);
        if (hasLicenses)
            return BadRequest(ApiResponse.Fail("Cannot delete a product with existing licenses"));

        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        product.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Product deleted"));
    }

    // ====================== Versions ======================

    [HttpPost("{id}/versions")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> AddVersion(
        Guid id,
        [FromBody] AddProductVersionRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (product == null) return NotFound(ApiResponse.Fail("Product not found"));

        var version = new ProductVersion
        {
            ProductId = id,
            Version = request.Version,
            ReleaseNotes = request.ReleaseNotes,
            Changelog = request.Changelog,
            ReleasedAt = request.ReleasedAt ?? DateTime.UtcNow,
            IsStable = request.IsStable,
            IsBeta = request.IsBeta,
            IsMajorUpdate = request.IsMajorUpdate,
            IsForced = request.IsForced,
            DownloadUrl = request.DownloadUrl,
            FileSizeBytes = request.FileSizeBytes,
            FileChecksum = request.FileChecksum,
            MinimumCompatibleVersion = request.MinimumCompatibleVersion,
            CreatedBy = _currentUser.Email,
        };

        _db.ProductVersions.Add(version);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { id = version.Id }, "Version added"));
    }
}

public record AddProductVersionRequest(
    string Version,
    string? ReleaseNotes,
    string? Changelog,
    DateTime? ReleasedAt,
    bool IsStable,
    bool IsBeta,
    bool IsMajorUpdate,
    bool IsForced,
    string? DownloadUrl,
    long? FileSizeBytes,
    string? FileChecksum,
    string? MinimumCompatibleVersion);
