import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from "axios";
import Cookies from "js-cookie";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";
export const TOKEN_COOKIE_NAME =
  process.env.NEXT_PUBLIC_TOKEN_COOKIE_NAME || "lm_client_token";

class ClientApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: `${API_URL}/api`,
      timeout: 30000,
      headers: { "Content-Type": "application/json" },
    });

    this.client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
      const token = Cookies.get(TOKEN_COOKIE_NAME);
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          Cookies.remove(TOKEN_COOKIE_NAME);
          if (typeof window !== "undefined" && !window.location.pathname.endsWith("/login")) {
            window.location.href = "/client/login";
          }
        }
        return Promise.reject(error);
      }
    );
  }

  setToken(token: string) {
    Cookies.set(TOKEN_COOKIE_NAME, token, {
      expires: 7,
      sameSite: "strict",
      secure: process.env.NODE_ENV === "production",
    });
  }

  clearToken() {
    Cookies.remove(TOKEN_COOKIE_NAME);
  }

  isAuthenticated(): boolean {
    return !!Cookies.get(TOKEN_COOKIE_NAME);
  }

  async get<T = unknown>(url: string, params?: Record<string, unknown>): Promise<T> {
    const res = await this.client.get<T>(url, { params });
    return res.data;
  }

  async post<T = unknown>(url: string, data?: unknown): Promise<T> {
    const res = await this.client.post<T>(url, data);
    return res.data;
  }

  async put<T = unknown>(url: string, data?: unknown): Promise<T> {
    const res = await this.client.put<T>(url, data);
    return res.data;
  }
}

export const apiClient = new ClientApiClient();

export const customerAuthApi = {
  login: (email: string, password: string) =>
    apiClient.post("/customer-portal/auth/login", { email, password }),
  forgotPassword: (email: string) =>
    apiClient.post("/customer-portal/auth/forgot-password", { email }),
  logout: () => apiClient.post("/customer-portal/auth/logout"),
  me: () => apiClient.get("/customer-portal/auth/me"),
};

export const customerApi = {
  // Dashboard
  dashboard: () => apiClient.get("/customer-portal/dashboard"),

  // Licenses
  licenses: () => apiClient.get("/customer-portal/licenses"),
  license: (id: string) => apiClient.get(`/customer-portal/licenses/${id}`),
  renew: (id: string, months: number) =>
    apiClient.post(`/customer-portal/licenses/${id}/renew`, { months }),
  upgrade: (id: string, newType: number) =>
    apiClient.post(`/customer-portal/licenses/${id}/upgrade`, { newLicenseType: newType }),

  // Domains & Devices
  domains: (licenseId: string) =>
    apiClient.get(`/customer-portal/licenses/${licenseId}/domains`),
  devices: (licenseId: string) =>
    apiClient.get(`/customer-portal/licenses/${licenseId}/devices`),

  // Updates
  availableUpdates: () => apiClient.get("/customer-portal/updates"),
  downloadUpdate: (versionId: string) =>
    apiClient.get(`/customer-portal/updates/${versionId}/download`),

  // Tickets
  tickets: () => apiClient.get("/customer-portal/tickets"),
  ticket: (id: string) => apiClient.get(`/customer-portal/tickets/${id}`),
  createTicket: (data: { subject: string; description: string; priority: number; licenseId?: string }) =>
    apiClient.post("/customer-portal/tickets", data),
  addComment: (ticketId: string, comment: string) =>
    apiClient.post(`/customer-portal/tickets/${ticketId}/comments`, { comment }),

  // Invoices
  invoices: () => apiClient.get("/customer-portal/invoices"),
  invoice: (id: string) => apiClient.get(`/customer-portal/invoices/${id}`),
  invoiceDownload: (id: string) =>
    apiClient.get(`/customer-portal/invoices/${id}/download`),

  // Profile
  profile: () => apiClient.get("/customer-portal/profile"),
  updateProfile: (data: Record<string, unknown>) =>
    apiClient.put("/customer-portal/profile", data),
};
