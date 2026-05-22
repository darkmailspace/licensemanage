import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from "axios";
import Cookies from "js-cookie";
import { API_URL, TOKEN_COOKIE_NAME, REFRESH_TOKEN_COOKIE_NAME } from "./constants";

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: `${API_URL}/api`,
      timeout: 30000,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor - attach auth token
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = Cookies.get(TOKEN_COOKIE_NAME);
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor - handle 401 and refresh
    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & {
          _retry?: boolean;
        };

        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;

          try {
            const refreshToken = Cookies.get(REFRESH_TOKEN_COOKIE_NAME);
            if (refreshToken) {
              const response = await axios.post(`${API_URL}/api/auth/refresh`, {
                refreshToken,
              });
              const { accessToken } = response.data.data;
              Cookies.set(TOKEN_COOKIE_NAME, accessToken, { expires: 1 });

              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${accessToken}`;
              }

              return this.client(originalRequest);
            }
          } catch {
            this.clearTokens();
            if (typeof window !== "undefined") {
              window.location.href = "/login";
            }
          }
        }

        return Promise.reject(error);
      }
    );
  }

  setTokens(accessToken: string, refreshToken?: string) {
    Cookies.set(TOKEN_COOKIE_NAME, accessToken, {
      expires: 1,
      sameSite: "strict",
      secure: process.env.NODE_ENV === "production",
    });
    if (refreshToken) {
      Cookies.set(REFRESH_TOKEN_COOKIE_NAME, refreshToken, {
        expires: 7,
        sameSite: "strict",
        secure: process.env.NODE_ENV === "production",
      });
    }
  }

  clearTokens() {
    Cookies.remove(TOKEN_COOKIE_NAME);
    Cookies.remove(REFRESH_TOKEN_COOKIE_NAME);
  }

  getToken(): string | undefined {
    return Cookies.get(TOKEN_COOKIE_NAME);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  // Generic methods
  async get<T = unknown>(url: string, params?: Record<string, unknown>): Promise<T> {
    const response = await this.client.get<T>(url, { params });
    return response.data;
  }

  async post<T = unknown>(url: string, data?: unknown): Promise<T> {
    const response = await this.client.post<T>(url, data);
    return response.data;
  }

  async put<T = unknown>(url: string, data?: unknown): Promise<T> {
    const response = await this.client.put<T>(url, data);
    return response.data;
  }

  async patch<T = unknown>(url: string, data?: unknown): Promise<T> {
    const response = await this.client.patch<T>(url, data);
    return response.data;
  }

  async delete<T = unknown>(url: string): Promise<T> {
    const response = await this.client.delete<T>(url);
    return response.data;
  }
}

export const apiClient = new ApiClient();

// Domain-specific API services
export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post("/auth/login", { email, password }),
  verifyMfa: (email: string, code: string) =>
    apiClient.post("/auth/mfa/verify", { email, code }),
  forgotPassword: (email: string) =>
    apiClient.post("/auth/forgot-password", { email }),
  resetPassword: (token: string, newPassword: string) =>
    apiClient.post("/auth/reset-password", { token, newPassword }),
  logout: () => apiClient.post("/auth/logout"),
  me: () => apiClient.get("/auth/me"),
};

export const licensesApi = {
  list: (params?: Record<string, unknown>) => apiClient.get("/licenses", params),
  get: (id: string) => apiClient.get(`/licenses/${id}`),
  create: (data: Record<string, unknown>) => apiClient.post("/licenses", data),
  update: (id: string, data: Record<string, unknown>) =>
    apiClient.put(`/licenses/${id}`, data),
  delete: (id: string) => apiClient.delete(`/licenses/${id}`),
  renew: (id: string, renewalMonths: number) =>
    apiClient.post(`/licenses/${id}/renew`, { renewalMonths }),
  suspend: (id: string, reason: string) =>
    apiClient.post(`/licenses/${id}/suspend`, { reason }),
  revoke: (id: string, reason: string) =>
    apiClient.post(`/licenses/${id}/revoke`, { reason }),
  upgrade: (id: string, newLicenseType: number) =>
    apiClient.post(`/licenses/${id}/upgrade`, { newLicenseType }),
  validate: (data: Record<string, unknown>) =>
    apiClient.post("/licenses/validate", data),
  activate: (data: Record<string, unknown>) =>
    apiClient.post("/licenses/activate", data),
};

export const customersApi = {
  list: (params?: Record<string, unknown>) => apiClient.get("/customers", params),
  get: (id: string) => apiClient.get(`/customers/${id}`),
  create: (data: Record<string, unknown>) => apiClient.post("/customers", data),
  update: (id: string, data: Record<string, unknown>) =>
    apiClient.put(`/customers/${id}`, data),
  delete: (id: string) => apiClient.delete(`/customers/${id}`),
};

export const productsApi = {
  list: (params?: Record<string, unknown>) => apiClient.get("/products", params),
  get: (id: string) => apiClient.get(`/products/${id}`),
  create: (data: Record<string, unknown>) => apiClient.post("/products", data),
  update: (id: string, data: Record<string, unknown>) =>
    apiClient.put(`/products/${id}`, data),
  delete: (id: string) => apiClient.delete(`/products/${id}`),
};

export const featuresApi = {
  list: (params?: Record<string, unknown>) => apiClient.get("/features", params),
  get: (id: string) => apiClient.get(`/features/${id}`),
};

export const domainsApi = {
  list: (params?: Record<string, unknown>) => apiClient.get("/domains", params),
  get: (id: string) => apiClient.get(`/domains/${id}`),
  delete: (id: string) => apiClient.delete(`/domains/${id}`),
};

export const devicesApi = {
  list: (params?: Record<string, unknown>) => apiClient.get("/devices", params),
  get: (id: string) => apiClient.get(`/devices/${id}`),
  deactivate: (id: string, reason: string) =>
    apiClient.post(`/devices/${id}/deactivate`, { reason }),
};

export const activationsApi = {
  list: (params?: Record<string, unknown>) =>
    apiClient.get("/activations", params),
  get: (id: string) => apiClient.get(`/activations/${id}`),
};

export const validationsApi = {
  list: (params?: Record<string, unknown>) =>
    apiClient.get("/validations", params),
  get: (id: string) => apiClient.get(`/validations/${id}`),
};

export const auditLogsApi = {
  list: (params?: Record<string, unknown>) =>
    apiClient.get("/audit-logs", params),
};

export const dashboardApi = {
  stats: () => apiClient.get("/dashboard/stats"),
  recentActivity: () => apiClient.get("/dashboard/recent-activity"),
  revenueChart: (period: string) =>
    apiClient.get("/dashboard/revenue", { period }),
  licenseChart: (period: string) =>
    apiClient.get("/dashboard/licenses", { period }),
};
