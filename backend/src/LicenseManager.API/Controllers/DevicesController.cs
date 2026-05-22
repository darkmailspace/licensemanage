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
[Route("api/devices")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class DevicesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IApplicationDbContext db, ICurrentUserService currentUser, ILogger<DevicesController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDeactivated,
        [FromQuery] bool? isVirtualMachine,
        [FromQuery] Guid? licenseId,
        CancellationToken cancellationToken)
    {
        var query = _db.LicenseDevices.AsNoTracking().Where(d => !d.IsDeleted);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(d =>
                d.DeviceName.ToLower().Contains(s) ||
                d.DeviceFingerprint.ToLower().Contains(s) ||
                (d.IPAddress != null && d.IPAddress.Contains(s)));
        }

        if (isActive.HasValue) query = query.Where(d => d.IsActive == isActive.Value);
        if (isDeactivated.HasValue) query = query.Where(d => d.IsDeactivated == isDeactivated.Value);
        if (isVirtualMachine.HasValue) query = query.Where(d => d.IsVirtualMachine == isVirtualMachine.Value);
        if (licenseId.HasValue) query = query.Where(d => d.LicenseId == licenseId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(d => d.License!).ThenInclude(l => l.Customer)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(d => new DeviceListItem(
                d.Id, d.LicenseId, d.License!.LicenseKey,
                d.License.Customer != null ? d.License.Customer.Name : "Unknown",
                d.DeviceName, d.DeviceFingerprint,
                d.OperatingSystem, d.Architecture, d.IsVirtualMachine,
                d.IPAddress, d.Country, d.City,
                d.IsActive, d.IsDeactivated, d.AccessCount,
                d.LastAccessedAt, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<DeviceListItem>>.Ok(
            PagedResult<DeviceListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var device = await _db.LicenseDevices
            .AsNoTracking()
            .Include(d => d.License!).ThenInclude(l => l.Customer)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (device == null) return NotFound(ApiResponse.Fail("Device not found"));
        return Ok(ApiResponse<object>.Ok(device));
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromBody] DeactivateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _db.LicenseDevices
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (device == null) return NotFound(ApiResponse.Fail("Device not found"));

        device.IsActive = false;
        device.IsDeactivated = true;
        device.DeactivatedAt = DateTime.UtcNow;
        device.DeactivationReason = request.Reason;
        device.UpdatedBy = _currentUser.Email;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Device deactivated"));
    }

    [HttpPost("{id}/reactivate")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var device = await _db.LicenseDevices
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (device == null) return NotFound(ApiResponse.Fail("Device not found"));

        device.IsActive = true;
        device.IsDeactivated = false;
        device.DeactivatedAt = null;
        device.DeactivationReason = null;
        device.UpdatedBy = _currentUser.Email;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Device reactivated"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var device = await _db.LicenseDevices
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);
        if (device == null) return NotFound(ApiResponse.Fail("Device not found"));

        device.IsDeleted = true;
        device.DeletedAt = DateTime.UtcNow;
        device.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Device deleted"));
    }
}
