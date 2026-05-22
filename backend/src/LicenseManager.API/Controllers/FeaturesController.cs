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
[Route("api/features")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class FeaturesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FeaturesController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _db.Features.AsNoTracking().Where(f => !f.IsDeleted);
        if (!string.IsNullOrEmpty(category)) query = query.Where(f => f.Category == category);
        if (isActive.HasValue) query = query.Where(f => f.IsActive == isActive.Value);

        var items = await query
            .OrderBy(f => f.Category).ThenBy(f => f.DisplayOrder).ThenBy(f => f.Name)
            .Select(f => new FeatureDto(
                f.Id, f.FeatureCode, f.Name, f.Description, f.Category,
                f.IsActive, f.RequiresEnterpriseLicense, f.AdditionalCost, f.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var feature = await _db.Features
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
        if (feature == null) return NotFound(ApiResponse.Fail("Feature not found"));
        return Ok(ApiResponse<object>.Ok(feature));
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeatureRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.FeatureCode) || string.IsNullOrEmpty(request.Name))
            return BadRequest(ApiResponse.Fail("Feature code and name are required"));

        var exists = await _db.Features
            .AnyAsync(f => f.FeatureCode == request.FeatureCode && !f.IsDeleted, cancellationToken);
        if (exists) return BadRequest(ApiResponse.Fail("Feature code already exists"));

        var feature = new Feature
        {
            FeatureCode = request.FeatureCode,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            IsActive = true,
            RequiresEnterpriseLicense = request.RequiresEnterpriseLicense,
            AdditionalCost = request.AdditionalCost,
            DisplayOrder = request.DisplayOrder,
            CreatedBy = _currentUser.Email,
        };

        _db.Features.Add(feature);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { id = feature.Id }, "Feature created"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var feature = await _db.Features
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
        if (feature == null) return NotFound(ApiResponse.Fail("Feature not found"));

        var inUse = await _db.LicenseFeatureMappings
            .AnyAsync(m => m.FeatureId == id && !m.IsDeleted, cancellationToken);
        if (inUse) return BadRequest(ApiResponse.Fail(
            "Cannot delete a feature that is assigned to one or more licenses"));

        feature.IsDeleted = true;
        feature.DeletedAt = DateTime.UtcNow;
        feature.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Feature deleted"));
    }
}
