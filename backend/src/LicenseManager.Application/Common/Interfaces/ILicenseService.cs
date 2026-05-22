using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Common.Interfaces;

public interface ILicenseService
{
    Task<License> GenerateLicenseAsync(
        Guid customerId,
        Guid productId,
        LicenseType licenseType,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> ValidateLicenseAsync(
        string licenseKey,
        string? domainName = null,
        string? deviceFingerprint = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
    
    Task<License?> ActivateLicenseAsync(
        string licenseKey,
        string activationToken,
        string domainName,
        string? deviceFingerprint = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> DeactivateLicenseAsync(
        Guid licenseId,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task<License?> RenewLicenseAsync(
        Guid licenseId,
        int renewalMonths,
        CancellationToken cancellationToken = default);
    
    Task<bool> SuspendLicenseAsync(
        Guid licenseId,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task<bool> RevokeLicenseAsync(
        Guid licenseId,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task<License?> UpgradeLicenseAsync(
        Guid licenseId,
        LicenseType newLicenseType,
        CancellationToken cancellationToken = default);
}
