// Common Types
export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Auth Types
export interface LoginCredentials {
  email: string;
  password: string;
}

export interface MfaCredentials {
  email: string;
  code: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: number;
  mfaEnabled: boolean;
  emailVerified: boolean;
  lastLoginAt?: string;
}

// License Types
export enum LicenseType {
  Trial = 1,
  Monthly = 2,
  Quarterly = 3,
  HalfYearly = 4,
  Yearly = 5,
  MultiYear = 6,
  Lifetime = 7,
  Enterprise = 8,
  Franchise = 9,
  WhiteLabel = 10,
  OEM = 11,
  Developer = 12,
  Reseller = 13,
}

export enum LicenseStatus {
  PendingActivation = 1,
  Active = 2,
  Suspended = 3,
  Expired = 4,
  Revoked = 5,
  GracePeriod = 6,
  PendingRenewal = 7,
  Cancelled = 8,
  Transferred = 9,
  Upgraded = 10,
  Downgraded = 11,
}

export interface License {
  id: string;
  licenseKey: string;
  activationToken: string;
  customerId: string;
  productId: string;
  licenseType: LicenseType;
  status: LicenseStatus;
  maxUsers: number;
  maxBranches: number;
  maxDomains: number;
  maxDevices: number;
  maxConcurrentLogins: number;
  maxApiCalls: number;
  maxStorageGB: number;
  maxEmployees: number;
  maxCustomers: number;
  maxLoans: number;
  maxCollections: number;
  startDate: string;
  expiryDate: string;
  activatedAt?: string;
  lastValidatedAt?: string;
  suspendedAt?: string;
  revokedAt?: string;
  domainLockEnabled: boolean;
  hardwareLockEnabled: boolean;
  ipLockEnabled: boolean;
  countryLockEnabled: boolean;
  allowedCountries?: string;
  ipWhitelist?: string;
  inGracePeriod: boolean;
  gracePeriodStartDate?: string;
  gracePeriodDays: number;
  price: number;
  currency: string;
  autoRenewal: boolean;
  paymentMethod?: string;
  notes?: string;
  internalNotes?: string;
  customer?: Customer;
  product?: Product;
  domains?: LicenseDomain[];
  devices?: LicenseDevice[];
  createdAt: string;
  updatedAt?: string;
}

// Customer Types
export interface Customer {
  id: string;
  customerCode: string;
  name: string;
  email: string;
  phone: string;
  companyName?: string;
  companyRegistrationNumber?: string;
  gstNumber?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  website?: string;
  contactPerson?: string;
  contactPersonEmail?: string;
  contactPersonPhone?: string;
  isActive: boolean;
  isVerified: boolean;
  verifiedAt?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

// Product Types
export interface Product {
  id: string;
  productCode: string;
  name: string;
  description?: string;
  version: string;
  isActive: boolean;
  imageUrl?: string;
  basePrice: number;
  currency: string;
  trialDays: number;
  allowTrial: boolean;
  maxDevicesPerLicense: number;
  maxUsersPerLicense: number;
  maxBranchesPerLicense: number;
  requireDomainLock: boolean;
  requireHardwareLock: boolean;
  gracePeriodDays: number;
  validationIntervalHours: number;
  createdAt: string;
  updatedAt?: string;
}

// Domain Types
export interface LicenseDomain {
  id: string;
  licenseId: string;
  domainName: string;
  isWildcard: boolean;
  isActive: boolean;
  verifiedAt?: string;
  lastAccessedAt?: string;
  isPrimary: boolean;
  isVerified: boolean;
  changeRequested: boolean;
  requestedDomain?: string;
  changeApproved: boolean;
  createdAt: string;
}

// Device Types
export interface LicenseDevice {
  id: string;
  licenseId: string;
  deviceName: string;
  deviceFingerprint: string;
  cpuId?: string;
  motherboardId?: string;
  diskSerialNumber?: string;
  macAddress?: string;
  biosSerialNumber?: string;
  operatingSystem?: string;
  osVersion?: string;
  architecture?: string;
  isVirtualMachine: boolean;
  vmPlatform?: string;
  ipAddress?: string;
  country?: string;
  city?: string;
  isActive: boolean;
  firstActivatedAt?: string;
  lastAccessedAt?: string;
  accessCount: number;
  isDeactivated: boolean;
  deactivatedAt?: string;
  deactivationReason?: string;
  createdAt: string;
}

// Activation Types
export interface LicenseActivation {
  id: string;
  licenseId: string;
  activationType: number;
  success: boolean;
  failureReason?: string;
  domainName?: string;
  deviceFingerprint?: string;
  ipAddress?: string;
  country?: string;
  userAgent?: string;
  activationCode?: string;
  createdAt: string;
  license?: License;
}

// Validation Types
export interface LicenseValidation {
  id: string;
  licenseId: string;
  validationResult: number;
  isValid: boolean;
  validationMessage?: string;
  domainName?: string;
  deviceFingerprint?: string;
  ipAddress?: string;
  country?: string;
  userAgent?: string;
  productVersion?: string;
  isHeartbeat: boolean;
  responseTimeMs: number;
  createdAt: string;
  license?: License;
}

// Feature Types
export interface Feature {
  id: string;
  featureCode: string;
  name: string;
  description?: string;
  category?: string;
  isActive: boolean;
  requiresEnterpriseLicense: boolean;
  additionalCost?: number;
  displayOrder: number;
  createdAt: string;
}

// Audit Log Types
export interface AuditLog {
  id: string;
  entityName: string;
  entityId: string;
  action: number;
  userId?: string;
  userName?: string;
  userEmail?: string;
  ipAddress?: string;
  userAgent?: string;
  oldValues?: Record<string, unknown>;
  newValues?: Record<string, unknown>;
  description?: string;
  createdAt: string;
}

// Dashboard Types
export interface DashboardStats {
  totalLicenses: number;
  activeLicenses: number;
  expiringLicenses: number;
  expiredLicenses: number;
  totalCustomers: number;
  totalRevenue: number;
  totalProducts: number;
  activations24h: number;
  validations24h: number;
  successRate: number;
}

export interface ChartDataPoint {
  name: string;
  value: number;
  date?: string;
}
