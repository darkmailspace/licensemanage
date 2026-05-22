export interface CustomerUser {
  id: string;
  customerCode: string;
  name: string;
  email: string;
  phone: string;
  companyName?: string;
  isVerified: boolean;
}

export enum LicenseType {
  Trial = 1, Monthly = 2, Quarterly = 3, HalfYearly = 4,
  Yearly = 5, MultiYear = 6, Lifetime = 7, Enterprise = 8,
  Franchise = 9, WhiteLabel = 10, OEM = 11, Developer = 12, Reseller = 13,
}

export enum LicenseStatus {
  PendingActivation = 1, Active = 2, Suspended = 3, Expired = 4,
  Revoked = 5, GracePeriod = 6, PendingRenewal = 7, Cancelled = 8,
  Transferred = 9, Upgraded = 10, Downgraded = 11,
}

export interface ClientLicense {
  id: string;
  licenseKey: string;
  productName: string;
  productVersion: string;
  licenseType: LicenseType;
  status: LicenseStatus;
  startDate: string;
  expiryDate: string;
  daysUntilExpiry: number;
  maxUsers: number;
  maxBranches: number;
  maxDomains: number;
  maxDevices: number;
  activeDomains: number;
  activeDevices: number;
  price: number;
  currency: string;
  autoRenewal: boolean;
}

export interface ClientDomain {
  id: string;
  domainName: string;
  isWildcard: boolean;
  isPrimary: boolean;
  isVerified: boolean;
  isActive: boolean;
  verifiedAt?: string;
  lastAccessedAt?: string;
}

export interface ClientDevice {
  id: string;
  deviceName: string;
  deviceFingerprint: string;
  operatingSystem?: string;
  ipAddress?: string;
  country?: string;
  isActive: boolean;
  lastAccessedAt?: string;
  firstActivatedAt?: string;
}

export interface ClientUpdate {
  id: string;
  productId: string;
  productName: string;
  version: string;
  releaseNotes: string;
  changelog: string;
  releasedAt: string;
  isMajorUpdate: boolean;
  isForced: boolean;
  fileSizeBytes: number;
  downloadUrl?: string;
  isInstalled: boolean;
}

export interface ClientTicket {
  id: string;
  ticketNumber: string;
  subject: string;
  description: string;
  status: number;
  priority: number;
  createdAt: string;
  updatedAt?: string;
  resolvedAt?: string;
  commentsCount: number;
}

export interface ClientInvoice {
  id: string;
  invoiceNumber: string;
  amount: number;
  currency: string;
  status: "paid" | "pending" | "overdue" | "cancelled";
  issueDate: string;
  dueDate: string;
  paidDate?: string;
  description: string;
  licenseKey?: string;
}
