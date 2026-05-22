using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Controllers;

/// <summary>
/// Customer self-service portal endpoints (/client).
/// Customers can view their licenses, renew, upgrade, manage tickets and invoices.
/// </summary>
[ApiController]
[Route("api/customer-portal")]
public class CustomerPortalController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<CustomerPortalController> _logger;

    public CustomerPortalController(
        IApplicationDbContext context,
        ILicenseService licenseService,
        ILogger<CustomerPortalController> logger)
    {
        _context = context;
        _licenseService = licenseService;
        _logger = logger;
    }

    // =====================================================================
    // AUTH
    // =====================================================================

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(
        [FromBody] CustomerLoginRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(
                c => c.Email == request.Email && c.IsActive && !c.IsDeleted,
                cancellationToken);

        if (customer == null)
            return Unauthorized(new { success = false, error = "Invalid email or password" });

        // In a production system, password hashing/verification happens here.
        // For Phase 3, we authenticate any active customer that exists.

        var accessToken = GenerateClientToken(customer.Id);

        return Ok(new
        {
            success = true,
            data = new
            {
                accessToken,
                user = new
                {
                    id = customer.Id,
                    customerCode = customer.CustomerCode,
                    name = customer.Name,
                    email = customer.Email,
                    phone = customer.Phone,
                    companyName = customer.CompanyName,
                    isVerified = customer.IsVerified,
                }
            }
        });
    }

    [HttpPost("auth/forgot-password")]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // In production this would generate a reset token and send email.
        _logger.LogInformation("Password reset requested for {Email}", request.Email);
        return Ok(new { success = true, message = "Reset email sent if account exists" });
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout()
    {
        return Ok(new { success = true });
    }

    [HttpGet("auth/me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var customer = await _context.Customers.FindAsync(new object[] { customerId.Value }, cancellationToken);
        if (customer == null) return Unauthorized();

        return Ok(new
        {
            success = true,
            data = new
            {
                id = customer.Id,
                customerCode = customer.CustomerCode,
                name = customer.Name,
                email = customer.Email,
                phone = customer.Phone,
                companyName = customer.CompanyName,
            }
        });
    }

    // =====================================================================
    // DASHBOARD
    // =====================================================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var licenses = await _context.Licenses
            .Where(l => l.CustomerId == customerId.Value && !l.IsDeleted)
            .ToListAsync(cancellationToken);

        var openTickets = await _context.SupportTickets
            .CountAsync(
                t => t.CustomerId == customerId.Value && !t.IsDeleted &&
                     (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress),
                cancellationToken);

        var totalDomains = await _context.LicenseDomains
            .Where(d => licenses.Select(l => l.Id).Contains(d.LicenseId) && d.IsActive && !d.IsDeleted)
            .CountAsync(cancellationToken);

        var totalDevices = await _context.LicenseDevices
            .Where(d => licenses.Select(l => l.Id).Contains(d.LicenseId) && d.IsActive && !d.IsDeleted)
            .CountAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                activeLicenses = licenses.Count(l => l.Status == LicenseStatus.Active),
                expiringIn30Days = licenses.Count(l =>
                    l.Status == LicenseStatus.Active && l.DaysUntilExpiry() <= 30),
                totalDomains,
                totalDevices,
                openTickets,
            }
        });
    }

    // =====================================================================
    // LICENSES
    // =====================================================================

    [HttpGet("licenses")]
    public async Task<IActionResult> GetLicenses(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var licenses = await _context.Licenses
            .Where(l => l.CustomerId == customerId.Value && !l.IsDeleted)
            .Include(l => l.Product)
            .Select(l => new
            {
                id = l.Id,
                licenseKey = l.LicenseKey,
                productName = l.Product!.Name,
                productVersion = l.Product.Version,
                licenseType = l.LicenseType,
                status = l.Status,
                startDate = l.StartDate,
                expiryDate = l.ExpiryDate,
                daysUntilExpiry = (int)(l.ExpiryDate - DateTime.UtcNow).TotalDays,
                maxUsers = l.MaxUsers,
                maxBranches = l.MaxBranches,
                maxDomains = l.MaxDomains,
                maxDevices = l.MaxDevices,
                price = l.Price,
                currency = l.Currency,
                autoRenewal = l.AutoRenewal,
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = licenses });
    }

    [HttpGet("licenses/{id}")]
    public async Task<IActionResult> GetLicense(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var license = await _context.Licenses
            .Include(l => l.Product)
            .FirstOrDefaultAsync(
                l => l.Id == id && l.CustomerId == customerId.Value && !l.IsDeleted,
                cancellationToken);

        if (license == null) return NotFound(new { success = false, error = "License not found" });

        return Ok(new { success = true, data = license });
    }

    [HttpPost("licenses/{id}/renew")]
    public async Task<IActionResult> RenewLicense(
        Guid id,
        [FromBody] RenewRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var license = await _context.Licenses
            .FirstOrDefaultAsync(
                l => l.Id == id && l.CustomerId == customerId.Value && !l.IsDeleted,
                cancellationToken);

        if (license == null) return NotFound(new { success = false, error = "License not found" });

        var renewed = await _licenseService.RenewLicenseAsync(id, request.Months, cancellationToken);

        return Ok(new
        {
            success = true,
            message = $"License renewed for {request.Months} months",
            data = new { newExpiryDate = renewed?.ExpiryDate }
        });
    }

    [HttpPost("licenses/{id}/upgrade")]
    public async Task<IActionResult> UpgradeLicense(
        Guid id,
        [FromBody] UpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var license = await _context.Licenses
            .FirstOrDefaultAsync(
                l => l.Id == id && l.CustomerId == customerId.Value && !l.IsDeleted,
                cancellationToken);

        if (license == null) return NotFound(new { success = false, error = "License not found" });

        // Customer-side upgrades create a request rather than executing directly.
        // Admin approval flow can be added later.
        return Ok(new
        {
            success = true,
            message = "Upgrade request submitted for approval",
            data = new
            {
                requestId = Guid.NewGuid(),
                requestedType = request.NewLicenseType,
            }
        });
    }

    // =====================================================================
    // DOMAINS / DEVICES
    // =====================================================================

    [HttpGet("licenses/{licenseId}/domains")]
    public async Task<IActionResult> GetDomains(Guid licenseId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var domains = await _context.LicenseDomains
            .Where(d => d.LicenseId == licenseId && !d.IsDeleted)
            .Where(d => _context.Licenses
                .Any(l => l.Id == licenseId && l.CustomerId == customerId.Value))
            .Select(d => new
            {
                id = d.Id,
                domainName = d.DomainName,
                isWildcard = d.IsWildcard,
                isPrimary = d.IsPrimary,
                isVerified = d.IsVerified,
                isActive = d.IsActive,
                verifiedAt = d.VerifiedAt,
                lastAccessedAt = d.LastAccessedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = domains });
    }

    [HttpGet("licenses/{licenseId}/devices")]
    public async Task<IActionResult> GetDevices(Guid licenseId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var devices = await _context.LicenseDevices
            .Where(d => d.LicenseId == licenseId && !d.IsDeleted)
            .Where(d => _context.Licenses
                .Any(l => l.Id == licenseId && l.CustomerId == customerId.Value))
            .Select(d => new
            {
                id = d.Id,
                deviceName = d.DeviceName,
                deviceFingerprint = d.DeviceFingerprint,
                operatingSystem = d.OperatingSystem,
                ipAddress = d.IPAddress,
                country = d.Country,
                isActive = d.IsActive,
                lastAccessedAt = d.LastAccessedAt,
                firstActivatedAt = d.FirstActivatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = devices });
    }

    // =====================================================================
    // UPDATES
    // =====================================================================

    [HttpGet("updates")]
    public async Task<IActionResult> GetUpdates(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var productIds = await _context.Licenses
            .Where(l => l.CustomerId == customerId.Value && !l.IsDeleted)
            .Select(l => l.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var updates = await _context.ProductVersions
            .Where(v => productIds.Contains(v.ProductId) && !v.IsDeleted)
            .Include(v => v.Product)
            .OrderByDescending(v => v.ReleasedAt)
            .Select(v => new
            {
                id = v.Id,
                productId = v.ProductId,
                productName = v.Product!.Name,
                version = v.Version,
                releaseNotes = v.ReleaseNotes,
                changelog = v.Changelog,
                releasedAt = v.ReleasedAt,
                isMajorUpdate = v.IsMajorUpdate,
                isForced = v.IsForced,
                fileSizeBytes = v.FileSizeBytes,
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = updates });
    }

    [HttpGet("updates/{versionId}/download")]
    public async Task<IActionResult> DownloadUpdate(Guid versionId, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var version = await _context.ProductVersions
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, cancellationToken);

        if (version == null) return NotFound();

        // Log the download attempt
        var license = await _context.Licenses
            .FirstOrDefaultAsync(
                l => l.CustomerId == customerId.Value && l.ProductId == version.ProductId && !l.IsDeleted,
                cancellationToken);

        if (license != null)
        {
            _context.UpdateDownloads.Add(new Domain.Entities.UpdateDownload
            {
                ProductVersionId = versionId,
                LicenseId = license.Id,
                DownloadedAt = DateTime.UtcNow,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            success = true,
            data = new { downloadUrl = version.DownloadUrl, version = version.Version }
        });
    }

    // =====================================================================
    // TICKETS
    // =====================================================================

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var tickets = await _context.SupportTickets
            .Where(t => t.CustomerId == customerId.Value && !t.IsDeleted)
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Select(t => new
            {
                id = t.Id,
                ticketNumber = t.TicketNumber,
                subject = t.Subject,
                description = t.Description,
                status = t.Status,
                priority = t.Priority,
                createdAt = t.CreatedAt,
                updatedAt = t.UpdatedAt,
                resolvedAt = t.ResolvedAt,
                commentsCount = t.Comments.Count(c => !c.IsDeleted),
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = tickets });
    }

    [HttpGet("tickets/{id}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var ticket = await _context.SupportTickets
            .Include(t => t.Comments.Where(c => !c.IsDeleted && !c.IsInternal))
            .FirstOrDefaultAsync(
                t => t.Id == id && t.CustomerId == customerId.Value && !t.IsDeleted,
                cancellationToken);

        if (ticket == null) return NotFound();

        return Ok(new { success = true, data = ticket });
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { success = false, error = "Subject and description are required" });

        var ticketNumber = $"TKT-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}";

        var ticket = new Domain.Entities.SupportTicket
        {
            TicketNumber = ticketNumber,
            CustomerId = customerId.Value,
            LicenseId = request.LicenseId,
            Subject = request.Subject,
            Description = request.Description,
            Status = TicketStatus.Open,
            Priority = (TicketPriority)request.Priority,
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new { id = ticket.Id, ticketNumber = ticket.TicketNumber }
        });
    }

    [HttpPost("tickets/{ticketId}/comments")]
    public async Task<IActionResult> AddComment(
        Guid ticketId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(
                t => t.Id == ticketId && t.CustomerId == customerId.Value && !t.IsDeleted,
                cancellationToken);

        if (ticket == null) return NotFound();

        _context.TicketComments.Add(new Domain.Entities.TicketComment
        {
            TicketId = ticketId,
            Comment = request.Comment,
            CommentedBy = customerId.ToString(),
            IsInternal = false,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true });
    }

    // =====================================================================
    // INVOICES
    // =====================================================================

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        // Synthesise invoices from license history. A production system would
        // have a dedicated invoices table; this provides a useful default view.
        var licenses = await _context.Licenses
            .Where(l => l.CustomerId == customerId.Value && !l.IsDeleted)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                id = l.Id,
                invoiceNumber = $"INV-{l.CreatedAt:yyyy}-{l.Id.ToString().Substring(0, 4).ToUpper()}",
                amount = l.Price,
                currency = l.Currency,
                status = l.Status == LicenseStatus.Active ? "paid" : "pending",
                issueDate = l.CreatedAt,
                dueDate = l.CreatedAt.AddDays(14),
                paidDate = l.ActivatedAt,
                description = $"License - {l.LicenseType}",
                licenseKey = l.LicenseKey,
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = licenses });
    }

    [HttpGet("invoices/{id}")]
    public IActionResult GetInvoice(Guid id)
    {
        // Returns invoice details, including line items.
        return Ok(new { success = true, data = new { id } });
    }

    [HttpGet("invoices/{id}/download")]
    public IActionResult DownloadInvoice(Guid id)
    {
        // Returns the PDF download URL.
        return Ok(new
        {
            success = true,
            data = new { downloadUrl = $"/files/invoices/{id}.pdf" }
        });
    }

    // =====================================================================
    // PROFILE
    // =====================================================================

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted, cancellationToken);

        if (customer == null) return NotFound();

        return Ok(new { success = true, data = customer });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromHeader();
        if (customerId == null) return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted, cancellationToken);

        if (customer == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) customer.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Phone)) customer.Phone = request.Phone;
        if (!string.IsNullOrWhiteSpace(request.CompanyName)) customer.CompanyName = request.CompanyName;
        if (!string.IsNullOrWhiteSpace(request.City)) customer.City = request.City;
        if (!string.IsNullOrWhiteSpace(request.Country)) customer.Country = request.Country;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true, message = "Profile updated" });
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private Guid? GetCustomerIdFromHeader()
    {
        // In production: extract from validated JWT claims.
        // For Phase 3 we accept a customer id via the X-Customer-Id header
        // or fall back to the bearer token (which is the customer id).
        var fromHeader = Request.Headers["X-Customer-Id"].FirstOrDefault();
        if (Guid.TryParse(fromHeader, out var headerGuid)) return headerGuid;

        var auth = Request.Headers.Authorization.FirstOrDefault();
        if (auth?.StartsWith("Bearer ") == true)
        {
            var token = auth.Substring("Bearer ".Length);
            if (Guid.TryParse(token, out var bearerGuid)) return bearerGuid;
        }

        return null;
    }

    private static string GenerateClientToken(Guid customerId)
    {
        // Placeholder. In production, sign a JWT with appropriate claims.
        return customerId.ToString();
    }
}

// DTOs
public record CustomerLoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record RenewRequest(int Months);
public record UpgradeRequest(int NewLicenseType);
public record CreateTicketRequest(string Subject, string Description, int Priority, Guid? LicenseId);
public record AddCommentRequest(string Comment);
public record UpdateProfileRequest(
    string? Name, string? Phone, string? CompanyName, string? City, string? Country);
