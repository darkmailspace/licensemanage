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
[Route("api/audit-logs")]
[Authorize(Policy = Policies.Admin)]
public class AuditLogsController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AuditLogsController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] string? entityName,
        [FromQuery] int? action,
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking().Where(l => !l.IsDeleted);

        if (!string.IsNullOrEmpty(entityName)) query = query.Where(l => l.EntityName == entityName);
        if (action.HasValue) query = query.Where(l => (int)l.Action == action.Value);
        if (!string.IsNullOrEmpty(userId)) query = query.Where(l => l.UserId == userId);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(l =>
                (l.Description != null && l.Description.ToLower().Contains(s)) ||
                (l.UserEmail != null && l.UserEmail.ToLower().Contains(s)) ||
                (l.UserName != null && l.UserName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(l => new AuditLogListItem(
                l.Id, l.EntityName, l.EntityId, (int)l.Action,
                l.UserName, l.UserEmail, l.IPAddress, l.Description, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<AuditLogListItem>>.Ok(
            PagedResult<AuditLogListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var log = await _db.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        if (log == null) return NotFound(ApiResponse.Fail("Audit log not found"));
        return Ok(ApiResponse<object>.Ok(log));
    }
}
