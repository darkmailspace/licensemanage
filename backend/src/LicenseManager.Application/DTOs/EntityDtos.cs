namespace LicenseManager.Application.DTOs;

// =============================================================================
// CUSTOMER DTOs
// =============================================================================

public record CustomerListItem(
    Guid Id,
    string CustomerCode,
    string Name,
    string Email,
    string Phone,
    string? CompanyName,
    string? City,
    string? Country,
    bool IsActive,
    bool IsVerified,
    int LicenseCount,
    DateTime CreatedAt);

public record CreateCustomerRequest(
    string Name,
    string Email,
    string Phone,
    string? CompanyName,
    string? CompanyRegistrationNumber,
    string? GSTNumber,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    string? Website,
    string? ContactPerson,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    string? Notes);

public record UpdateCustomerRequest(
    string? Name,
    string? Email,
    string? Phone,
    string? CompanyName,
    string? CompanyRegistrationNumber,
    string? GSTNumber,
    string? Address,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    string? Website,
    string? ContactPerson,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    bool? IsActive,
    string? Notes);

// =============================================================================
// PRODUCT DTOs
// =============================================================================

public record ProductListItem(
    Guid Id,
    string ProductCode,
    string Name,
    string? Description,
    string Version,
    bool IsActive,
    decimal BasePrice,
    string Currency,
    int TrialDays,
    int LicenseCount,
    int ActiveLicenseCount,
    DateTime CreatedAt);

public record CreateProductRequest(
    string ProductCode,
    string Name,
    string? Description,
    string Version,
    decimal BasePrice,
    string Currency,
    int TrialDays,
    bool AllowTrial,
    int MaxDevicesPerLicense,
    int MaxUsersPerLicense,
    int MaxBranchesPerLicense,
    bool RequireDomainLock,
    bool RequireHardwareLock,
    int GracePeriodDays,
    int ValidationIntervalHours,
    string? ImageUrl);

public record UpdateProductRequest(
    string? Name,
    string? Description,
    string? Version,
    bool? IsActive,
    decimal? BasePrice,
    string? Currency,
    int? TrialDays,
    bool? AllowTrial,
    int? MaxDevicesPerLicense,
    int? MaxUsersPerLicense,
    int? MaxBranchesPerLicense,
    bool? RequireDomainLock,
    bool? RequireHardwareLock,
    int? GracePeriodDays,
    int? ValidationIntervalHours,
    string? ImageUrl);

// =============================================================================
// FEATURE DTOs
// =============================================================================

public record FeatureDto(
    Guid Id,
    string FeatureCode,
    string Name,
    string? Description,
    string? Category,
    bool IsActive,
    bool RequiresEnterpriseLicense,
    decimal? AdditionalCost,
    int DisplayOrder);

public record CreateFeatureRequest(
    string FeatureCode,
    string Name,
    string? Description,
    string? Category,
    bool RequiresEnterpriseLicense,
    decimal? AdditionalCost,
    int DisplayOrder);

// =============================================================================
// DOMAIN DTOs
// =============================================================================

public record DomainListItem(
    Guid Id,
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    string DomainName,
    bool IsWildcard,
    bool IsPrimary,
    bool IsActive,
    bool IsVerified,
    DateTime? VerifiedAt,
    DateTime? LastAccessedAt,
    bool ChangeRequested,
    string? RequestedDomain,
    DateTime CreatedAt);

public record ApproveDomainChangeRequest(bool Approved, string? Reason);

// =============================================================================
// DEVICE DTOs
// =============================================================================

public record DeviceListItem(
    Guid Id,
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    string DeviceName,
    string DeviceFingerprint,
    string? OperatingSystem,
    string? Architecture,
    bool IsVirtualMachine,
    string? IpAddress,
    string? Country,
    string? City,
    bool IsActive,
    bool IsDeactivated,
    int AccessCount,
    DateTime? LastAccessedAt,
    DateTime CreatedAt);

public record DeactivateDeviceRequest(string Reason);

// =============================================================================
// ACTIVATION DTOs
// =============================================================================

public record ActivationListItem(
    Guid Id,
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    int ActivationType,
    bool Success,
    string? FailureReason,
    string? DomainName,
    string? IpAddress,
    string? Country,
    DateTime CreatedAt);

// =============================================================================
// VALIDATION DTOs
// =============================================================================

public record ValidationListItem(
    Guid Id,
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    int ValidationResult,
    bool IsValid,
    string? ValidationMessage,
    string? DomainName,
    string? IpAddress,
    string? Country,
    string? ProductVersion,
    bool IsHeartbeat,
    int ResponseTimeMs,
    DateTime CreatedAt);

// =============================================================================
// AUDIT LOG DTOs
// =============================================================================

public record AuditLogListItem(
    Guid Id,
    string EntityName,
    string EntityId,
    int Action,
    string? UserName,
    string? UserEmail,
    string? IpAddress,
    string? Description,
    DateTime CreatedAt);

// =============================================================================
// ADMIN USER DTOs
// =============================================================================

public record AdminUserListItem(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    int Role,
    string RoleName,
    bool IsActive,
    bool MfaEnabled,
    bool EmailVerified,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record CreateAdminUserRequest(
    string Email,
    string FullName,
    string Password,
    string? Phone,
    int Role);

public record UpdateAdminUserRequest(
    string? FullName,
    string? Phone,
    int? Role,
    bool? IsActive);

// =============================================================================
// SETTING DTOs
// =============================================================================

public record SettingDto(
    string Key,
    string? Value,
    string? Category,
    string? Description,
    bool IsSecret,
    DateTime? UpdatedAt);

public record UpdateSettingsRequest(IDictionary<string, string?> Settings);

// =============================================================================
// REPORT DTOs
// =============================================================================

public record DashboardStatsDto(
    int TotalLicenses,
    int ActiveLicenses,
    int ExpiredLicenses,
    int ExpiringIn30Days,
    int TotalCustomers,
    int TotalProducts,
    decimal TotalRevenue,
    string Currency,
    int Activations24h,
    int SuccessfulActivations24h,
    int Validations24h,
    decimal SuccessRate);

public record TimeSeriesPoint(string Period, decimal Value, int Count);
public record CategoryBreakdown(string Name, int Count, decimal Value);

public record RevenueReportDto(
    decimal TotalRevenue,
    decimal AverageLicenseValue,
    decimal MonthOverMonthGrowth,
    string Currency,
    IReadOnlyList<TimeSeriesPoint> ByMonth,
    IReadOnlyList<CategoryBreakdown> ByProduct,
    IReadOnlyList<CategoryBreakdown> ByLicenseType);

public record LicenseReportDto(
    int Total,
    int Active,
    int Expired,
    int Suspended,
    int Revoked,
    int ExpiringNext30Days,
    decimal RenewalRate,
    IReadOnlyList<CategoryBreakdown> ByStatus,
    IReadOnlyList<CategoryBreakdown> ByType,
    IReadOnlyList<TimeSeriesPoint> ByMonth);

public record ActivationReportDto(
    int Total,
    int Successful,
    int Failed,
    decimal SuccessRate,
    IReadOnlyList<TimeSeriesPoint> ByDay,
    IReadOnlyList<CategoryBreakdown> ByCountry,
    IReadOnlyList<CategoryBreakdown> ByFailureReason);

public record ExpiryReportItem(
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    string ProductName,
    DateTime ExpiryDate,
    int DaysUntilExpiry,
    bool AutoRenewal,
    decimal Price,
    string Currency);
