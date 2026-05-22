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

// =============================================================================
// Phase 4C - payments
//
// Talks to the Stripe + Razorpay endpoints created in Phase 4C:
//   POST /api/payments/session          -> open provider session
//   POST /api/payments/{id}/verify      -> sync verify
//   GET  /api/payments/{id}             -> read payment + refunds
//
// Provider SDKs are loaded on demand from their CDNs via loadScript() so
// this customer portal does not need new npm dependencies (it is still
// pinned to React 19 RC).
// =============================================================================

export enum PaymentProvider {
  Stripe = 1,
  Razorpay = 2,
}

export enum PaymentStatus {
  Created = 0,
  Pending = 1,
  Authorized = 2,
  Captured = 3,
  Failed = 4,
  Cancelled = 5,
  Refunded = 6,
  PartiallyRefunded = 7,
}

export interface CreatePaymentSessionRequest {
  provider: PaymentProvider;
  customerId: string;
  licenseId?: string;
  amount: number;          // major units
  currency: string;        // ISO 4217
  description?: string;
  receipt?: string;
  customerEmail?: string;
  customerName?: string;
  customerPhone?: string;
  metadata?: Record<string, string>;
}

export interface PaymentSession {
  paymentId: string;
  provider: PaymentProvider;
  status: PaymentStatus;
  amountMinor: number;
  currency: string;
  /** Stripe: pi_xxx. Razorpay: pay_xxx (filled only after the customer completes). */
  providerPaymentId?: string;
  /** Razorpay: order_xxx. Empty for Stripe. */
  providerOrderId?: string;
  /** Stripe: PaymentIntent client_secret used to confirm via Elements. */
  clientSecret?: string;
  /** Hosted-checkout URL when the provider returns one. */
  checkoutUrl?: string;
  /** Public key the frontend uses to talk to the provider directly. */
  publishableKey?: string;
  createdAtUtc: string;
}

export interface PaymentVerificationRequest {
  providerOrderId?: string;
  providerPaymentId?: string;
  signature?: string;
}

export interface PaymentVerificationResult {
  success: boolean;
  paymentId: string;
  status: PaymentStatus;
  providerPaymentId?: string;
  errorCode?: string;
  errorMessage?: string;
}

interface ApiEnvelope<T> {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
  errors?: string[];
}

async function unwrap<T>(promise: Promise<ApiEnvelope<T>>): Promise<T> {
  const res = await promise;
  if (!res.success || res.data == null) {
    throw new Error(res.error || res.message || "Request failed");
  }
  return res.data;
}

export const paymentsApi = {
  createSession: (data: CreatePaymentSessionRequest) =>
    unwrap<PaymentSession>(
      apiClient.post<ApiEnvelope<PaymentSession>>("/payments/session", data)
    ),
  verify: (paymentId: string, data: PaymentVerificationRequest) =>
    unwrap<PaymentVerificationResult>(
      apiClient.post<ApiEnvelope<PaymentVerificationResult>>(
        `/payments/${paymentId}/verify`,
        data
      )
    ),
  get: (paymentId: string) =>
    apiClient.get<ApiEnvelope<unknown>>(`/payments/${paymentId}`),
};

// -----------------------------------------------------------------------------
// Provider SDK loader - idempotent, in-flight-coalescing dynamic <script> loader
// -----------------------------------------------------------------------------

const _scriptsLoaded = new Set<string>();
const _scriptsInflight = new Map<string, Promise<void>>();

export function loadScript(src: string): Promise<void> {
  if (_scriptsLoaded.has(src)) return Promise.resolve();
  if (_scriptsInflight.has(src)) return _scriptsInflight.get(src)!;
  if (typeof document === "undefined") {
    return Promise.reject(new Error("loadScript called outside the browser"));
  }

  const promise = new Promise<void>((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>(`script[src="${src}"]`);
    if (existing) {
      _scriptsLoaded.add(src);
      return resolve();
    }
    const el = document.createElement("script");
    el.src = src;
    el.async = true;
    el.onload = () => {
      _scriptsLoaded.add(src);
      resolve();
    };
    el.onerror = () => reject(new Error(`Failed to load ${src}`));
    document.head.appendChild(el);
  }).finally(() => {
    _scriptsInflight.delete(src);
  });

  _scriptsInflight.set(src, promise);
  return promise;
}

export const STRIPE_JS_URL = "https://js.stripe.com/v3/";
export const RAZORPAY_CHECKOUT_URL = "https://checkout.razorpay.com/v1/checkout.js";

// -----------------------------------------------------------------------------
// Provider SDK type surface (only what we actually call - no @stripe/stripe-js
// or @types/razorpay deps needed)
// -----------------------------------------------------------------------------

export interface StripeInstance {
  elements: (opts: { clientSecret: string; appearance?: unknown }) => StripeElements;
  confirmPayment: (opts: {
    elements: StripeElements;
    confirmParams?: { return_url?: string };
    redirect?: "always" | "if_required";
  }) => Promise<{
    error?: { type?: string; code?: string; message?: string };
    paymentIntent?: { id: string; status: string };
  }>;
}

export interface StripeElements {
  create: (type: "payment", opts?: unknown) => StripePaymentElement;
}

export interface StripePaymentElement {
  mount: (selectorOrEl: string | HTMLElement) => void;
  unmount: () => void;
}

export interface RazorpayHandlerResponse {
  razorpay_payment_id: string;
  razorpay_order_id: string;
  razorpay_signature: string;
}

export interface RazorpayOptions {
  key: string;
  amount: number;
  currency: string;
  order_id: string;
  name?: string;
  description?: string;
  prefill?: { name?: string; email?: string; contact?: string };
  handler?: (response: RazorpayHandlerResponse) => void | Promise<void>;
  modal?: { ondismiss?: () => void };
  theme?: { color?: string };
}

export interface RazorpayInstance {
  open: () => void;
}

declare global {
  interface Window {
    Stripe?: (publishableKey: string) => StripeInstance;
    Razorpay?: new (options: RazorpayOptions) => RazorpayInstance;
  }
}

// -----------------------------------------------------------------------------
// Imperative provider helpers used by the renew/upgrade/invoices pages
// -----------------------------------------------------------------------------

export interface StripeMount {
  /** Confirms the PaymentIntent and POSTs to /verify. Returns the local paymentId. */
  confirm: () => Promise<{ paymentId: string }>;
  /** Unmount the Stripe Element. Idempotent. */
  unmount: () => void;
}

/**
 * Loads Stripe.js if needed, mounts a PaymentElement bound to the session's
 * client_secret into <c>container</c>, and returns a handle the page calls
 * when the user clicks "Pay".
 */
export async function startStripeAndMount(
  session: PaymentSession,
  container: HTMLElement
): Promise<StripeMount> {
  await loadScript(STRIPE_JS_URL);
  if (!window.Stripe || !session.publishableKey || !session.clientSecret) {
    throw new Error(
      "Stripe is not configured on the server. Set Payments:Stripe:PublishableKey."
    );
  }
  const stripe = window.Stripe(session.publishableKey);
  const elements = stripe.elements({ clientSecret: session.clientSecret });
  const paymentElement = elements.create("payment");
  paymentElement.mount(container);

  return {
    async confirm() {
      const { error, paymentIntent } = await stripe.confirmPayment({
        elements,
        confirmParams: { return_url: window.location.href },
        redirect: "if_required",
      });
      if (error) throw new Error(error.message || "Payment failed.");
      if (!paymentIntent) throw new Error("Stripe did not return a payment intent.");
      const result = await paymentsApi.verify(session.paymentId, {
        providerPaymentId: paymentIntent.id,
      });
      if (!result.success) {
        throw new Error(result.errorMessage || "Verification failed.");
      }
      return { paymentId: result.paymentId };
    },
    unmount() {
      try {
        paymentElement.unmount();
      } catch {
        /* noop */
      }
    },
  };
}

/**
 * Loads Razorpay Checkout JS if needed, opens the modal, awaits the handler
 * callback (or modal dismissal), runs verify, and resolves with the local
 * paymentId. Rejects if the user dismissed the modal or verification failed.
 */
export function payWithRazorpay(
  session: PaymentSession,
  prefill: { name?: string; email?: string; contact?: string },
  description: string
): Promise<{ paymentId: string }> {
  return loadScript(RAZORPAY_CHECKOUT_URL).then(
    () =>
      new Promise<{ paymentId: string }>((resolve, reject) => {
        if (!window.Razorpay || !session.providerOrderId || !session.publishableKey) {
          reject(
            new Error(
              "Razorpay is not configured on the server. Set Payments:Razorpay:KeyId."
            )
          );
          return;
        }
        const rzp = new window.Razorpay({
          key: session.publishableKey,
          amount: session.amountMinor,
          currency: session.currency,
          order_id: session.providerOrderId,
          name: "License Manager",
          description,
          prefill,
          handler: async (resp) => {
            try {
              const result = await paymentsApi.verify(session.paymentId, {
                providerOrderId: resp.razorpay_order_id,
                providerPaymentId: resp.razorpay_payment_id,
                signature: resp.razorpay_signature,
              });
              if (!result.success) {
                reject(new Error(result.errorMessage || "Verification failed."));
                return;
              }
              resolve({ paymentId: result.paymentId });
            } catch (err) {
              reject(err instanceof Error ? err : new Error(String(err)));
            }
          },
          modal: {
            ondismiss: () => reject(new Error("Payment cancelled.")),
          },
          theme: { color: "#0066ff" },
        });
        rzp.open();
      })
  );
}
