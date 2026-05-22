using LicenseManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<LoginHistory> LoginHistory { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<License> Licenses { get; }
    DbSet<LicenseDomain> LicenseDomains { get; }
    DbSet<LicenseDevice> LicenseDevices { get; }
    DbSet<LicenseActivation> LicenseActivations { get; }
    DbSet<LicenseValidation> LicenseValidations { get; }
    DbSet<Feature> Features { get; }
    DbSet<LicenseFeatureMapping> LicenseFeatureMappings { get; }
    DbSet<ProductFeature> ProductFeatures { get; }
    DbSet<ProductVersion> ProductVersions { get; }
    DbSet<UpdateDownload> UpdateDownloads { get; }
    DbSet<LicenseHistory> LicenseHistory { get; }
    DbSet<SupportTicket> SupportTickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ApiLog> ApiLogs { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    // Payments (Phase 4C)
    DbSet<Payment> Payments { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
