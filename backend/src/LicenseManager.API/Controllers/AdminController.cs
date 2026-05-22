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

/// <summary>
/// Manage admin users (super-admin only).
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = Policies.SuperAdmin)]
public class AdminController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        ILogger<AdminController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _db.AdminUsers.AsNoTracking().Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.FullName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        query = pagination.IsDescending
            ? query.OrderByDescending(u => u.CreatedAt)
            : query.OrderBy(u => u.CreatedAt);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(u => new AdminUserListItem(
                u.Id, u.Email, u.FullName, u.Phone,
                (int)u.Role, u.Role.ToString(),
                u.IsActive, u.MfaEnabled, u.EmailVerified,
                u.LastLoginAt, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<AdminUserListItem>>.Ok(
            PagedResult<AdminUserListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user == null) return NotFound(ApiResponse.Fail("Admin user not found"));

        return Ok(ApiResponse<AdminUserListItem>.Ok(new AdminUserListItem(
            user.Id, user.Email, user.FullName, user.Phone,
            (int)user.Role, user.Role.ToString(),
            user.IsActive, user.MfaEnabled, user.EmailVerified,
            user.LastLoginAt, user.CreatedAt)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(ApiResponse.Fail("Email and password are required"));
        if (request.Password.Length < 8)
            return BadRequest(ApiResponse.Fail("Password must be at least 8 characters"));
        if (request.Role < 1 || request.Role > 4)
            return BadRequest(ApiResponse.Fail("Invalid role"));

        var exists = await _db.AdminUsers
            .AnyAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);
        if (exists)
            return BadRequest(ApiResponse.Fail("An admin with this email already exists"));

        var user = new AdminUser
        {
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = (UserRole)request.Role,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true,
            EmailVerified = false,
            CreatedBy = _currentUser.Email,
        };

        _db.AdminUsers.Add(user);
        await WriteAuditLog("AdminUser", user.Id, AuditAction.Create,
            $"Created admin user {user.Email}", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { id = user.Id }, "Admin user created"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user == null) return NotFound(ApiResponse.Fail("Admin user not found"));

        // Prevent demoting yourself or demoting the last super-admin
        if (request.Role.HasValue && request.Role.Value != (int)user.Role)
        {
            if (user.Id == _currentUser.UserId)
                return BadRequest(ApiResponse.Fail("You cannot change your own role"));

            if (user.Role == UserRole.SuperAdmin && request.Role.Value != 1)
            {
                var superAdmins = await _db.AdminUsers
                    .CountAsync(u => u.Role == UserRole.SuperAdmin && !u.IsDeleted, cancellationToken);
                if (superAdmins <= 1)
                    return BadRequest(ApiResponse.Fail("Cannot demote the last super-admin"));
            }
        }

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.Role.HasValue) user.Role = (UserRole)request.Role.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        user.UpdatedBy = _currentUser.Email;

        await WriteAuditLog("AdminUser", user.Id, AuditAction.Update,
            $"Updated admin user {user.Email}", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Admin user updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user == null) return NotFound(ApiResponse.Fail("Admin user not found"));

        if (user.Id == _currentUser.UserId)
            return BadRequest(ApiResponse.Fail("You cannot delete yourself"));

        if (user.Role == UserRole.SuperAdmin)
        {
            var superAdmins = await _db.AdminUsers
                .CountAsync(u => u.Role == UserRole.SuperAdmin && !u.IsDeleted && u.Id != id,
                    cancellationToken);
            if (superAdmins == 0)
                return BadRequest(ApiResponse.Fail("Cannot delete the last super-admin"));
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;

        await WriteAuditLog("AdminUser", user.Id, AuditAction.Delete,
            $"Deleted admin user {user.Email}", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Admin user deleted"));
    }

    [HttpGet("{id}/login-history")]
    public async Task<IActionResult> LoginHistory(
        Guid id,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var query = _db.LoginHistory
            .AsNoTracking()
            .Where(l => l.UserId == id && !l.IsDeleted)
            .OrderByDescending(l => l.LoginAttemptAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(l => new
            {
                l.Id,
                l.Success,
                l.IPAddress,
                l.UserAgent,
                l.Country,
                l.City,
                l.FailureReason,
                l.LoginAttemptAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            items,
            totalCount = total,
            pageNumber = pagination.Page,
            pageSize = pagination.PageSize,
        }));
    }

    private async Task WriteAuditLog(
        string entityName, Guid entityId, AuditAction action, string description,
        CancellationToken cancellationToken)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId.ToString(),
                Action = action,
                UserId = _currentUser.UserId?.ToString(),
                UserName = _currentUser.FullName,
                UserEmail = _currentUser.Email,
                IPAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent,
                Description = description,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log");
            await Task.CompletedTask;
        }
    }
}
