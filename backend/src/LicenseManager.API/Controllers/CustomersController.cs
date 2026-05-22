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
[Route("api/customers")]
[Authorize(Policy = Policies.AdminOrSupport)]
public class CustomersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ILogger<CustomersController> logger)
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
        CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(pagination.Search))
        {
            var s = pagination.Search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.CustomerCode.ToLower().Contains(s) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
        }

        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        if (isVerified.HasValue) query = query.Where(c => c.IsVerified == isVerified.Value);

        var total = await query.CountAsync(cancellationToken);

        query = pagination.SortBy?.ToLower() switch
        {
            "name" => pagination.IsDescending
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name),
            "email" => pagination.IsDescending
                ? query.OrderByDescending(c => c.Email)
                : query.OrderBy(c => c.Email),
            _ => pagination.IsDescending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
        };

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(c => new CustomerListItem(
                c.Id, c.CustomerCode, c.Name, c.Email, c.Phone,
                c.CompanyName, c.City, c.Country,
                c.IsActive, c.IsVerified,
                _db.Licenses.Count(l => l.CustomerId == c.Id && !l.IsDeleted),
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<CustomerListItem>>.Ok(
            PagedResult<CustomerListItem>.Create(items, total, pagination.Page, pagination.PageSize)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (customer == null) return NotFound(ApiResponse.Fail("Customer not found"));

        var stats = new
        {
            totalLicenses = await _db.Licenses
                .CountAsync(l => l.CustomerId == id && !l.IsDeleted, cancellationToken),
            activeLicenses = await _db.Licenses
                .CountAsync(l => l.CustomerId == id && l.Status == LicenseManager.Domain.Enums.LicenseStatus.Active && !l.IsDeleted,
                    cancellationToken),
            totalSpend = await _db.Licenses
                .Where(l => l.CustomerId == id && !l.IsDeleted)
                .SumAsync(l => (decimal?)l.Price, cancellationToken) ?? 0m,
        };

        return Ok(ApiResponse<object>.Ok(new { customer, stats }));
    }

    [HttpGet("{id}/licenses")]
    [Authorize(Policy = Policies.Support)]
    public async Task<IActionResult> GetLicenses(Guid id, CancellationToken cancellationToken)
    {
        var licenses = await _db.Licenses
            .AsNoTracking()
            .Where(l => l.CustomerId == id && !l.IsDeleted)
            .Include(l => l.Product)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.LicenseKey,
                l.LicenseType,
                l.Status,
                l.StartDate,
                l.ExpiryDate,
                ProductName = l.Product != null ? l.Product.Name : null,
                l.Price,
                l.Currency,
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(licenses));
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email))
            return BadRequest(ApiResponse.Fail("Name and email are required"));

        var emailExists = await _db.Customers
            .AnyAsync(c => c.Email == request.Email && !c.IsDeleted, cancellationToken);
        if (emailExists)
            return BadRequest(ApiResponse.Fail("Customer with this email already exists"));

        var code = await GenerateCustomerCode(cancellationToken);

        var customer = new Customer
        {
            CustomerCode = code,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            CompanyRegistrationNumber = request.CompanyRegistrationNumber,
            GSTNumber = request.GSTNumber,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Country = request.Country,
            PostalCode = request.PostalCode,
            Website = request.Website,
            ContactPerson = request.ContactPerson,
            ContactPersonEmail = request.ContactPersonEmail,
            ContactPersonPhone = request.ContactPersonPhone,
            Notes = request.Notes,
            IsActive = true,
            CreatedBy = _currentUser.Email,
        };

        _db.Customers.Add(customer);
        await WriteAudit("Customer", customer.Id, AuditAction.Create, $"Created customer {customer.Name}");
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { id = customer.Id, code = customer.CustomerCode },
            "Customer created"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        if (customer == null) return NotFound(ApiResponse.Fail("Customer not found"));

        if (request.Name != null) customer.Name = request.Name;
        if (request.Email != null) customer.Email = request.Email;
        if (request.Phone != null) customer.Phone = request.Phone;
        if (request.CompanyName != null) customer.CompanyName = request.CompanyName;
        if (request.CompanyRegistrationNumber != null)
            customer.CompanyRegistrationNumber = request.CompanyRegistrationNumber;
        if (request.GSTNumber != null) customer.GSTNumber = request.GSTNumber;
        if (request.Address != null) customer.Address = request.Address;
        if (request.City != null) customer.City = request.City;
        if (request.State != null) customer.State = request.State;
        if (request.Country != null) customer.Country = request.Country;
        if (request.PostalCode != null) customer.PostalCode = request.PostalCode;
        if (request.Website != null) customer.Website = request.Website;
        if (request.ContactPerson != null) customer.ContactPerson = request.ContactPerson;
        if (request.ContactPersonEmail != null) customer.ContactPersonEmail = request.ContactPersonEmail;
        if (request.ContactPersonPhone != null) customer.ContactPersonPhone = request.ContactPersonPhone;
        if (request.IsActive.HasValue) customer.IsActive = request.IsActive.Value;
        if (request.Notes != null) customer.Notes = request.Notes;
        customer.UpdatedBy = _currentUser.Email;

        await WriteAudit("Customer", customer.Id, AuditAction.Update, $"Updated customer {customer.Name}");
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Customer updated"));
    }

    [HttpPost("{id}/verify")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Verify(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        if (customer == null) return NotFound(ApiResponse.Fail("Customer not found"));

        customer.IsVerified = true;
        customer.VerifiedAt = DateTime.UtcNow;
        await WriteAudit("Customer", customer.Id, AuditAction.Update, $"Verified customer {customer.Name}");
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Customer verified"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        if (customer == null) return NotFound(ApiResponse.Fail("Customer not found"));

        var hasActiveLicenses = await _db.Licenses
            .AnyAsync(l => l.CustomerId == id && l.Status == LicenseManager.Domain.Enums.LicenseStatus.Active && !l.IsDeleted,
                cancellationToken);
        if (hasActiveLicenses)
            return BadRequest(ApiResponse.Fail(
                "Cannot delete a customer with active licenses. Revoke them first."));

        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        customer.IsActive = false;
        await WriteAudit("Customer", customer.Id, AuditAction.Delete, $"Deleted customer {customer.Name}");
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Customer deleted"));
    }

    private async Task<string> GenerateCustomerCode(CancellationToken cancellationToken)
    {
        for (int i = 0; i < 5; i++)
        {
            var candidate = $"CUST{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(1000, 9999)}";
            var exists = await _db.Customers
                .AnyAsync(c => c.CustomerCode == candidate, cancellationToken);
            if (!exists) return candidate;
        }
        return $"CUST{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";
    }

    private Task WriteAudit(string entity, Guid id, AuditAction action, string description)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                EntityName = entity,
                EntityId = id.ToString(),
                Action = action,
                UserId = _currentUser.UserId?.ToString(),
                UserName = _currentUser.FullName,
                UserEmail = _currentUser.Email,
                IPAddress = _currentUser.IpAddress,
                Description = description,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log");
        }
        return Task.CompletedTask;
    }
}
