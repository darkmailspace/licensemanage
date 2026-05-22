using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicenseManager.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private readonly IApplicationDbContext _context;
    private readonly ICryptographyService _cryptography;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(
        IApplicationDbContext context,
        ICryptographyService cryptography,
        ILogger<LicenseService> logger)
    {
        _context = context;
        _cryptography = cryptography;
        _logger = logger;
    }

    public async Task<License> GenerateLicenseAsync(
        Guid customerId,
        Guid productId,
        LicenseType licenseType,
        Dictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating license for customer {CustomerId}, product {ProductId}", customerId, productId);

        var customer = await _context.Customers.FindAsync(new object[] { customerId }, cancellationToken);
        if (customer == null)
            throw new ArgumentException("Customer not found", nameof(customerId));

        var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null)
            throw new ArgumentException("Product not found", nameof(productId));

        // Generate RSA key pair for this license
        var (publicKey, privateKey) = _cryptography.GenerateRsaKeyPair();

        // Calculate expiry date based on license type
        var startDate = DateTime.UtcNow;
        var expiryDate = licenseType switch
        {
            LicenseType.Trial => startDate.AddDays(product.TrialDays),
            LicenseType.Monthly => startDate.AddMonths(1),
            LicenseType.Quarterly => startDate.AddMonths(3),
            LicenseType.HalfYearly => startDate.AddMonths(6),
            LicenseType.Yearly => startDate.AddYears(1),
            LicenseType.MultiYear => startDate.AddYears(configuration?.ContainsKey("Years") == true ? Convert.ToInt32(configuration["Years"]) : 2),
            LicenseType.Lifetime => startDate.AddYears(99),
            _ => startDate.AddYears(1)
        };

        var license = new License
        {
            LicenseKey = _cryptography.GenerateLicenseKey(),
            ActivationToken = _cryptography.GenerateActivationToken(),
            CustomerId = customerId,
            ProductId = productId,
            LicenseType = licenseType,
            Status = LicenseStatus.PendingActivation,
            
            // Default limits from product
            MaxUsers = product.MaxUsersPerLicense,
            MaxBranches = product.MaxBranchesPerLicense,
            MaxDomains = 1,
            MaxDevices = product.MaxDevicesPerLicense,
            MaxConcurrentLogins = product.MaxUsersPerLicense,
            MaxApiCalls = 100000,
            MaxStorageGB = 50,
            MaxEmployees = 100,
            MaxCustomers = 10000,
            MaxLoans = 50000,
            MaxCollections = 100000,
            
            StartDate = startDate,
            ExpiryDate = expiryDate,
            
            PublicKey = publicKey,
            DomainLockEnabled = product.RequireDomainLock,
            HardwareLockEnabled = product.RequireHardwareLock,
            GracePeriodDays = product.GracePeriodDays,
            
            Price = product.BasePrice,
            Currency = product.Currency
        };

        // Apply configuration overrides if provided
        if (configuration != null)
        {
            if (configuration.ContainsKey("MaxUsers"))
                license.MaxUsers = Convert.ToInt32(configuration["MaxUsers"]);
            if (configuration.ContainsKey("MaxBranches"))
                license.MaxBranches = Convert.ToInt32(configuration["MaxBranches"]);
            if (configuration.ContainsKey("MaxDomains"))
                license.MaxDomains = Convert.ToInt32(configuration["MaxDomains"]);
            if (configuration.ContainsKey("MaxDevices"))
                license.MaxDevices = Convert.ToInt32(configuration["MaxDevices"]);
            if (configuration.ContainsKey("Price"))
                license.Price = Convert.ToDecimal(configuration["Price"]);
        }

        // Create license signature
        var licenseData = $"{license.LicenseKey}|{license.CustomerId}|{license.ProductId}|{license.ExpiryDate:O}";
        license.LicenseSignature = _cryptography.SignData(licenseData, privateKey);

        // Encrypt and store the license payload
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            license.LicenseKey,
            license.CustomerId,
            license.ProductId,
            license.LicenseType,
            license.ExpiryDate,
            license.MaxUsers,
            license.MaxBranches,
            license.MaxDevices
        });
        license.EncryptedPayload = _cryptography.EncryptAes(payload, _cryptography.GenerateAesKey());

        _context.Licenses.Add(license);

        // Copy default features from product
        var productFeatures = await _context.ProductFeatures
            .Where(pf => pf.ProductId == productId && !pf.IsDeleted)
            .Include(pf => pf.Feature)
            .ToListAsync(cancellationToken);

        foreach (var productFeature in productFeatures.Where(pf => pf.IsDefaultEnabled))
        {
            var featureMapping = new LicenseFeatureMapping
            {
                LicenseId = license.Id,
                FeatureId = productFeature.FeatureId,
                IsEnabled = true,
                EnabledAt = DateTime.UtcNow
            };
            _context.LicenseFeatureMappings.Add(featureMapping);
        }

        // Add license history
        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Created",
            NewStatus = LicenseStatus.PendingActivation,
            Description = $"License created for {customer.Name} - {product.Name}",
            Changes = new Dictionary<string, object>
            {
                { "LicenseType", licenseType.ToString() },
                { "ExpiryDate", expiryDate },
                { "Price", license.Price }
            }
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("License generated successfully: {LicenseKey}", license.LicenseKey);

        return license;
    }

    public async Task<bool> ValidateLicenseAsync(
        string licenseKey,
        string? domainName = null,
        string? deviceFingerprint = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating license {LicenseKey}", licenseKey);

        var license = await _context.Licenses
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Include(l => l.Domains)
            .Include(l => l.Devices)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey && !l.IsDeleted, cancellationToken);

        if (license == null)
        {
            await LogValidationAsync(null, ValidationResult.Invalid, false, "License not found", 
                domainName, deviceFingerprint, ipAddress, cancellationToken);
            return false;
        }

        // Check if expired
        if (license.IsExpired())
        {
            if (license.IsInGracePeriod())
            {
                await LogValidationAsync(license.Id, ValidationResult.Valid, true, "License in grace period", 
                    domainName, deviceFingerprint, ipAddress, cancellationToken);
                return true;
            }

            await LogValidationAsync(license.Id, ValidationResult.Expired, false, "License expired", 
                domainName, deviceFingerprint, ipAddress, cancellationToken);
            return false;
        }

        // Check status
        if (license.Status == LicenseStatus.Revoked)
        {
            await LogValidationAsync(license.Id, ValidationResult.Revoked, false, "License revoked", 
                domainName, deviceFingerprint, ipAddress, cancellationToken);
            return false;
        }

        if (license.Status == LicenseStatus.Suspended)
        {
            await LogValidationAsync(license.Id, ValidationResult.Suspended, false, "License suspended", 
                domainName, deviceFingerprint, ipAddress, cancellationToken);
            return false;
        }

        // Check domain lock
        if (license.DomainLockEnabled && !string.IsNullOrEmpty(domainName))
        {
            var domainExists = license.Domains.Any(d => 
                d.IsActive && 
                !d.IsDeleted && 
                (d.DomainName.Equals(domainName, StringComparison.OrdinalIgnoreCase) ||
                 (d.IsWildcard && domainName.EndsWith(d.DomainName.TrimStart('*'), StringComparison.OrdinalIgnoreCase))));

            if (!domainExists)
            {
                await LogValidationAsync(license.Id, ValidationResult.DomainMismatch, false, $"Domain not registered: {domainName}", 
                    domainName, deviceFingerprint, ipAddress, cancellationToken);
                return false;
            }
        }

        // Check hardware lock
        if (license.HardwareLockEnabled && !string.IsNullOrEmpty(deviceFingerprint))
        {
            var deviceExists = license.Devices.Any(d => 
                d.IsActive && 
                !d.IsDeactivated && 
                !d.IsDeleted && 
                d.DeviceFingerprint == deviceFingerprint);

            if (!deviceExists)
            {
                await LogValidationAsync(license.Id, ValidationResult.HardwareMismatch, false, "Device not registered", 
                    domainName, deviceFingerprint, ipAddress, cancellationToken);
                return false;
            }
        }

        // Update last validated timestamp
        license.LastValidatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await LogValidationAsync(license.Id, ValidationResult.Valid, true, "License valid", 
            domainName, deviceFingerprint, ipAddress, cancellationToken);

        return true;
    }

    public async Task<License?> ActivateLicenseAsync(
        string licenseKey,
        string activationToken,
        string domainName,
        string? deviceFingerprint = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating license {LicenseKey}", licenseKey);

        var license = await _context.Licenses
            .Include(l => l.Domains)
            .Include(l => l.Devices)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey && l.ActivationToken == activationToken && !l.IsDeleted, 
                cancellationToken);

        if (license == null)
        {
            _logger.LogWarning("License activation failed: Invalid license key or activation token");
            await LogActivationAsync(null, ActivationType.Online, false, "Invalid license key or activation token", 
                domainName, deviceFingerprint, metadata, cancellationToken);
            return null;
        }

        if (!license.CanActivate())
        {
            _logger.LogWarning("License activation failed: License cannot be activated (Status: {Status})", license.Status);
            await LogActivationAsync(license.Id, ActivationType.Online, false, $"License cannot be activated. Current status: {license.Status}", 
                domainName, deviceFingerprint, metadata, cancellationToken);
            return null;
        }

        // Check domain limit
        if (license.Domains.Count(d => d.IsActive && !d.IsDeleted) >= license.MaxDomains)
        {
            _logger.LogWarning("License activation failed: Maximum domains reached");
            await LogActivationAsync(license.Id, ActivationType.Online, false, "Maximum domains reached", 
                domainName, deviceFingerprint, metadata, cancellationToken);
            return null;
        }

        // Register domain
        var licenseDomain = new LicenseDomain
        {
            LicenseId = license.Id,
            DomainName = domainName,
            IsActive = true,
            IsPrimary = !license.Domains.Any(),
            VerifiedAt = DateTime.UtcNow,
            IsVerified = true
        };
        _context.LicenseDomains.Add(licenseDomain);

        // Register device if fingerprint provided
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            if (license.Devices.Count(d => d.IsActive && !d.IsDeactivated && !d.IsDeleted) >= license.MaxDevices)
            {
                _logger.LogWarning("License activation failed: Maximum devices reached");
                await LogActivationAsync(license.Id, ActivationType.Online, false, "Maximum devices reached", 
                    domainName, deviceFingerprint, metadata, cancellationToken);
                return null;
            }

            var licenseDevice = new LicenseDevice
            {
                LicenseId = license.Id,
                DeviceName = metadata?.ContainsKey("DeviceName") == true ? metadata["DeviceName"].ToString()! : "Unknown Device",
                DeviceFingerprint = deviceFingerprint,
                IsActive = true,
                FirstActivatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                OperatingSystem = metadata?.ContainsKey("OS") == true ? metadata["OS"].ToString() : null,
                Architecture = metadata?.ContainsKey("Architecture") == true ? metadata["Architecture"].ToString() : null,
                IPAddress = metadata?.ContainsKey("IPAddress") == true ? metadata["IPAddress"].ToString() : null
            };
            _context.LicenseDevices.Add(licenseDevice);
        }

        // Activate the license
        license.Activate();

        // Log activation
        await LogActivationAsync(license.Id, ActivationType.Online, true, "License activated successfully", 
            domainName, deviceFingerprint, metadata, cancellationToken);

        // Add license history
        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Activated",
            PreviousStatus = LicenseStatus.PendingActivation,
            NewStatus = LicenseStatus.Active,
            Description = $"License activated for domain: {domainName}",
            Changes = new Dictionary<string, object>
            {
                { "Domain", domainName },
                { "ActivatedAt", DateTime.UtcNow }
            }
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("License activated successfully: {LicenseKey}", licenseKey);

        return license;
    }

    public async Task<bool> DeactivateLicenseAsync(Guid licenseId, string reason, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FindAsync(new object[] { licenseId }, cancellationToken);
        if (license == null) return false;

        license.Status = LicenseStatus.Cancelled;
        
        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Deactivated",
            PreviousStatus = license.Status,
            NewStatus = LicenseStatus.Cancelled,
            Description = $"License deactivated: {reason}"
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<License?> RenewLicenseAsync(Guid licenseId, int renewalMonths, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FindAsync(new object[] { licenseId }, cancellationToken);
        if (license == null) return null;

        var previousExpiryDate = license.ExpiryDate;
        license.ExpiryDate = license.ExpiryDate > DateTime.UtcNow 
            ? license.ExpiryDate.AddMonths(renewalMonths)
            : DateTime.UtcNow.AddMonths(renewalMonths);

        if (license.Status == LicenseStatus.Expired)
            license.Status = LicenseStatus.Active;

        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Renewed",
            Description = $"License renewed for {renewalMonths} months",
            Changes = new Dictionary<string, object>
            {
                { "PreviousExpiryDate", previousExpiryDate },
                { "NewExpiryDate", license.ExpiryDate }
            }
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);
        return license;
    }

    public async Task<bool> SuspendLicenseAsync(Guid licenseId, string reason, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FindAsync(new object[] { licenseId }, cancellationToken);
        if (license == null) return false;

        var previousStatus = license.Status;
        license.Suspend(reason);

        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Suspended",
            PreviousStatus = previousStatus,
            NewStatus = LicenseStatus.Suspended,
            Description = $"License suspended: {reason}"
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeLicenseAsync(Guid licenseId, string reason, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FindAsync(new object[] { licenseId }, cancellationToken);
        if (license == null) return false;

        var previousStatus = license.Status;
        license.Revoke(reason);

        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Revoked",
            PreviousStatus = previousStatus,
            NewStatus = LicenseStatus.Revoked,
            Description = $"License revoked: {reason}"
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<License?> UpgradeLicenseAsync(Guid licenseId, LicenseType newLicenseType, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FindAsync(new object[] { licenseId }, cancellationToken);
        if (license == null) return null;

        var previousType = license.LicenseType;
        license.LicenseType = newLicenseType;
        license.Status = LicenseStatus.Upgraded;

        var history = new LicenseHistory
        {
            LicenseId = license.Id,
            Action = "License Upgraded",
            Description = $"License upgraded from {previousType} to {newLicenseType}",
            Changes = new Dictionary<string, object>
            {
                { "PreviousType", previousType.ToString() },
                { "NewType", newLicenseType.ToString() }
            }
        };
        _context.LicenseHistory.Add(history);

        await _context.SaveChangesAsync(cancellationToken);
        return license;
    }

    #region Private Helper Methods

    private async Task LogValidationAsync(
        Guid? licenseId,
        ValidationResult validationResult,
        bool isValid,
        string message,
        string? domainName,
        string? deviceFingerprint,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (licenseId == null) return;

        var validation = new LicenseValidation
        {
            LicenseId = licenseId.Value,
            ValidationResult = validationResult,
            IsValid = isValid,
            ValidationMessage = message,
            DomainName = domainName,
            DeviceFingerprint = deviceFingerprint,
            IPAddress = ipAddress,
            ResponseTimeMs = 0
        };

        _context.LicenseValidations.Add(validation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task LogActivationAsync(
        Guid? licenseId,
        ActivationType activationType,
        bool success,
        string? failureReason,
        string? domainName,
        string? deviceFingerprint,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken)
    {
        if (licenseId == null) return;

        var activation = new LicenseActivation
        {
            LicenseId = licenseId.Value,
            ActivationType = activationType,
            Success = success,
            FailureReason = failureReason,
            DomainName = domainName,
            DeviceFingerprint = deviceFingerprint,
            IPAddress = metadata?.ContainsKey("IPAddress") == true ? metadata["IPAddress"].ToString() : null,
            UserAgent = metadata?.ContainsKey("UserAgent") == true ? metadata["UserAgent"].ToString() : null,
            RequestMetadata = metadata
        };

        _context.LicenseActivations.Add(activation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
