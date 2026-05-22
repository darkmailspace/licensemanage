using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Common.Models;
using LicenseManager.Application.DTOs;
using LicenseManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

[ApiController]
[Route("api/domains")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class DomainsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DomainsController> _logger;

    public DomainsController(IApplicationDbContext db, ICurrentUserService currentUser, ILogger<DomainsController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isVerified,
        [FromQuery] bool? changeRequested,
        [FromQuery] Guid? licenseId,
        CancellationToken cancellationToken)
    {
        var query = _db.LicenseDomains
            .AsNoTracking()
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(d => d.DomainName.ToLower().Contains(s));
        }

        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive.Value);
        if (isVerified.HasValue) query = query.Where(d => d.IsVerified == isVerified.Value);
        if (changeRequested.HasValue) query = query.Where(d => d.ChangeRequested == changeRequested.Value);
        if (licenseId.HasValue) query = query.Where(d => d.LicenseId == licenseId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(d => d.License!).ThenInclude(l => l.Customer)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(d => new DomainListItem(
                d.Id, d.LicenseId, d.License!.LicenseKey,
                d.License.Customer != null ? d.License.Customer.Name : "Unknown",
                d.DomainName, d.IsWildcard, d.IsPrimary,
                d.IsActive, d.IsVerified, d.VerifiedAt, d.LastAccessedAt,
                d.ChangeRequested, d.RequestedDomain,
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<DomainListItem>>.Ok(
            PagedResult<DomainListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var domain = await _db.LicenseDomains
            .AsNoTracking()
            .Include(d => d.License!).ThenInclude(l => l.Customer)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (domain == null) return NotFound(ApiResponse.Fail("Domain not found"));
        return Ok(ApiResponse<object>.Ok(domain));
    }

    [HttpPost("{id}/verify")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Verify(Guid id, CancellationToken cancellationToken)
    {
        var domain = await _db.LicenseDomains
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (domain == null) return NotFound(ApiResponse.Fail("Domain not found"));

        domain.IsVerified = true;
        domain.VerifiedAt = DateTime.UtcNow;
        domain.IsActive = true;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Domain verified"));
    }

    [HttpPost("{id}/approve-change")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> ApproveChange(
        Guid id,
        [FromBody] ApproveDomainChangeRequest request,
        CancellationToken cancellationToken)
    {
        var domain = await _db.LicenseDomains
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (domain == null) return NotFound(ApiResponse.Fail("Domain not found"));
        if (!domain.ChangeRequested)
            return BadRequest(ApiResponse.Fail("No change request pending"));

        if (request.Approved)
        {
            domain.DomainName = domain.RequestedDomain ?? domain.DomainName;
            domain.IsWildcard = domain.DomainName.StartsWith("*.");
            domain.ChangeApproved = true;
            domain.ChangeApprovedAt = DateTime.UtcNow;
            domain.ApprovedBy = _currentUser.Email;
        }
        domain.ChangeRequested = false;
        domain.RequestedDomain = null;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok(request.Approved ? "Change approved" : "Change denied"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var domain = await _db.LicenseDomains
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (domain == null) return NotFound(ApiResponse.Fail("Domain not found"));

        domain.IsDeleted = true;
        domain.DeletedAt = DateTime.UtcNow;
        domain.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Domain removed"));
    }
}
