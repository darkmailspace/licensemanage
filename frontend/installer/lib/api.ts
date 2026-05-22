import axios from "axios";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

export const api = axios.create({
  baseURL: `${API_URL}/api`,
  timeout: 30000,
  headers: { "Content-Type": "application/json" },
});

export const installerApi = {
  // Verification steps
  verifyLicense: (licenseKey: string, activationToken: string) =>
    api.post("/installer/verify-license", { licenseKey, activationToken }),
  verifyDomain: (licenseKey: string, domainName: string) =>
    api.post("/installer/verify-domain", { licenseKey, domainName }),
  verifyHardware: (licenseKey: string, fingerprint: string, deviceInfo: Record<string, string>) =>
    api.post("/installer/verify-hardware", { licenseKey, fingerprint, deviceInfo }),

  // Database setup
  testDatabase: (config: DatabaseConfig) =>
    api.post("/installer/test-database", config),
  setupDatabase: (config: DatabaseConfig) =>
    api.post("/installer/setup-database", config),

  // Admin and company
  createAdmin: (data: AdminAccountData) =>
    api.post("/installer/create-admin", data),
  saveCompany: (data: CompanyData) =>
    api.post("/installer/save-company", data),

  // Final steps
  configureApi: (data: ApiConfig) =>
    api.post("/installer/configure-api", data),
  finalize: (licenseKey: string) =>
    api.post("/installer/finalize", { licenseKey }),

  // Status
  status: () => api.get("/installer/status"),
};

export interface DatabaseConfig {
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
  sslMode: string;
}

export interface AdminAccountData {
  fullName: string;
  email: string;
  password: string;
  phone?: string;
}

export interface CompanyData {
  companyName: string;
  registrationNumber?: string;
  gstNumber?: string;
  email: string;
  phone: string;
  website?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
}

export interface ApiConfig {
  smtpHost?: string;
  smtpPort?: number;
  smtpUser?: string;
  smtpPassword?: string;
  smsApiKey?: string;
  whatsappApiKey?: string;
}
