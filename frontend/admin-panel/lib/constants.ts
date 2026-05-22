export const APP_CONFIG = {
  name: "License Manager",
  description: "Enterprise License Management System",
  version: "1.0.0",
};

export const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

export const TOKEN_COOKIE_NAME = process.env.NEXT_PUBLIC_TOKEN_COOKIE_NAME || "lm_admin_token";
export const REFRESH_TOKEN_COOKIE_NAME = process.env.NEXT_PUBLIC_REFRESH_TOKEN_COOKIE_NAME || "lm_admin_refresh";

export const LICENSE_TYPES = [
  { value: 1, label: "Trial" },
  { value: 2, label: "Monthly" },
  { value: 3, label: "Quarterly" },
  { value: 4, label: "Half-Yearly" },
  { value: 5, label: "Yearly" },
  { value: 6, label: "Multi-Year" },
  { value: 7, label: "Lifetime" },
  { value: 8, label: "Enterprise" },
  { value: 9, label: "Franchise" },
  { value: 10, label: "White Label" },
  { value: 11, label: "OEM" },
  { value: 12, label: "Developer" },
  { value: 13, label: "Reseller" },
] as const;

export const LICENSE_STATUSES = [
  { value: 1, label: "Pending Activation", color: "yellow" },
  { value: 2, label: "Active", color: "green" },
  { value: 3, label: "Suspended", color: "orange" },
  { value: 4, label: "Expired", color: "red" },
  { value: 5, label: "Revoked", color: "red" },
  { value: 6, label: "Grace Period", color: "yellow" },
  { value: 7, label: "Pending Renewal", color: "blue" },
  { value: 8, label: "Cancelled", color: "gray" },
  { value: 9, label: "Transferred", color: "purple" },
  { value: 10, label: "Upgraded", color: "blue" },
  { value: 11, label: "Downgraded", color: "blue" },
] as const;

export const ACTIVATION_TYPES = [
  { value: 1, label: "Online" },
  { value: 2, label: "Offline" },
  { value: 3, label: "Manual" },
  { value: 4, label: "Auto Activation" },
] as const;

export const VALIDATION_RESULTS = [
  { value: 1, label: "Valid", color: "green" },
  { value: 2, label: "Invalid", color: "red" },
  { value: 3, label: "Expired", color: "red" },
  { value: 4, label: "Revoked", color: "red" },
  { value: 5, label: "Suspended", color: "orange" },
  { value: 6, label: "Domain Mismatch", color: "red" },
  { value: 7, label: "Hardware Mismatch", color: "red" },
  { value: 8, label: "Max Devices Reached", color: "orange" },
  { value: 9, label: "Max Users Reached", color: "orange" },
  { value: 10, label: "Feature Not Enabled", color: "yellow" },
  { value: 11, label: "Signature Invalid", color: "red" },
  { value: 12, label: "Tampered License", color: "red" },
  { value: 13, label: "IP Restricted", color: "orange" },
  { value: 14, label: "Country Restricted", color: "orange" },
  { value: 15, label: "Grace Period Expired", color: "red" },
] as const;

export const USER_ROLES = [
  { value: 1, label: "Super Admin" },
  { value: 2, label: "Admin" },
  { value: 3, label: "Support" },
  { value: 4, label: "Viewer" },
] as const;

export const TICKET_STATUSES = [
  { value: 1, label: "Open", color: "blue" },
  { value: 2, label: "In Progress", color: "yellow" },
  { value: 3, label: "Waiting", color: "orange" },
  { value: 4, label: "Resolved", color: "green" },
  { value: 5, label: "Closed", color: "gray" },
  { value: 6, label: "Cancelled", color: "red" },
] as const;

export const TICKET_PRIORITIES = [
  { value: 1, label: "Low", color: "gray" },
  { value: 2, label: "Medium", color: "blue" },
  { value: 3, label: "High", color: "orange" },
  { value: 4, label: "Critical", color: "red" },
] as const;

export const COUNTRIES = [
  "United States", "United Kingdom", "Canada", "Australia", "Germany",
  "France", "Italy", "Spain", "Netherlands", "Belgium", "Switzerland",
  "India", "China", "Japan", "Singapore", "United Arab Emirates",
  "Brazil", "Mexico", "Argentina", "South Africa", "New Zealand",
];
