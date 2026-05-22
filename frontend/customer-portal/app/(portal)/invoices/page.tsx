"use client";

import { useEffect, useRef, useState } from "react";
import {
  Calendar,
  Check,
  CheckCircle2,
  Clock,
  CreditCard,
  Download,
  FileText,
  IndianRupee,
  Loader2,
  ShieldCheck,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import {
  customerApi,
  paymentsApi,
  payWithRazorpay,
  PaymentProvider,
  type PaymentSession,
  startStripeAndMount,
  type StripeMount,
} from "@/lib/api";
import { formatCurrency, formatDate } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";

const STATUS_MAP = {
  paid: { label: "Paid", variant: "success" as const, icon: Check },
  pending: { label: "Pending", variant: "warning" as const, icon: Clock },
  overdue: { label: "Overdue", variant: "destructive" as const, icon: Clock },
  cancelled: { label: "Cancelled", variant: "outline" as const, icon: Clock },
};

const SAMPLE_INVOICES = [
  {
    id: "i1",
    invoiceNumber: "INV-2026-0001",
    amount: 999,
    currency: "USD",
    status: "paid" as const,
    issueDate: new Date(Date.now() - 339 * 86400000).toISOString(),
    dueDate: new Date(Date.now() - 325 * 86400000).toISOString(),
    paidDate: new Date(Date.now() - 338 * 86400000).toISOString(),
    description: "Finance ERP System - Yearly License",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-...",
  },
  {
    id: "i2",
    invoiceNumber: "INV-2026-0042",
    amount: 999,
    currency: "USD",
    status: "pending" as const,
    issueDate: new Date(Date.now() - 5 * 86400000).toISOString(),
    dueDate: new Date(Date.now() + 25 * 86400000).toISOString(),
    description: "Finance ERP System - Yearly Renewal",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-...",
  },
  {
    id: "i3",
    invoiceNumber: "INV-2025-9876",
    amount: 99,
    currency: "USD",
    status: "paid" as const,
    issueDate: new Date(Date.now() - 700 * 86400000).toISOString(),
    dueDate: new Date(Date.now() - 686 * 86400000).toISOString(),
    paidDate: new Date(Date.now() - 698 * 86400000).toISOString(),
    description: "Finance ERP System - Monthly Trial",
    licenseKey: "LK-OLDX1234-...",
  },
];

type Invoice = (typeof SAMPLE_INVOICES)[number] & { status: "paid" | "pending" | "overdue" | "cancelled" };

// Local payment-dialog state machine. Inlined per-page (no new files).
type PayStatus =
  | { kind: "idle" }
  | { kind: "session-loading"; provider: PaymentProvider }
  | { kind: "stripe-ready"; session: PaymentSession }
  | { kind: "stripe-confirming" }
  | { kind: "razorpay-opening" }
  | { kind: "verifying" }
  | { kind: "success" }
  | { kind: "error"; message: string };

export default function InvoicesPage() {
  const { user } = useAuthStore();
  const [downloading, setDownloading] = useState<string | null>(null);
  const [paidIds, setPaidIds] = useState<Set<string>>(new Set());

  // -- Payment dialog wiring --------------------------------------------------
  const [activeInvoice, setActiveInvoice] = useState<Invoice | null>(null);
  const [payOpen, setPayOpen] = useState(false);
  const [payStatus, setPayStatus] = useState<PayStatus>({ kind: "idle" });
  const stripeContainerRef = useRef<HTMLDivElement | null>(null);
  const stripeMountRef = useRef<StripeMount | null>(null);

  const description = activeInvoice
    ? `Invoice ${activeInvoice.invoiceNumber} – ${activeInvoice.description}`
    : "Pay invoice";
  const amount = activeInvoice?.amount ?? 0;
  const currency = activeInvoice?.currency ?? "USD";

  useEffect(() => {
    if (payStatus.kind !== "stripe-ready" || !stripeContainerRef.current) return;
    let cancelled = false;
    (async () => {
      try {
        const mount = await startStripeAndMount(payStatus.session, stripeContainerRef.current!);
        if (cancelled) {
          mount.unmount();
          return;
        }
        stripeMountRef.current = mount;
      } catch (err) {
        if (!cancelled) {
          setPayStatus({
            kind: "error",
            message: err instanceof Error ? err.message : "Failed to load Stripe.",
          });
        }
      }
    })();
    return () => {
      cancelled = true;
      stripeMountRef.current?.unmount();
      stripeMountRef.current = null;
    };
  }, [payStatus]);

  useEffect(() => {
    if (payOpen) setPayStatus({ kind: "idle" });
  }, [payOpen]);

  const openPaymentDialog = (invoice: Invoice) => {
    setActiveInvoice(invoice);
    setPayOpen(true);
  };

  const startStripe = async () => {
    if (!user || !activeInvoice) return;
    setPayStatus({ kind: "session-loading", provider: PaymentProvider.Stripe });
    try {
      const session = await paymentsApi.createSession({
        provider: PaymentProvider.Stripe,
        customerId: user.id,
        amount,
        currency,
        description,
        receipt: activeInvoice.invoiceNumber,
        metadata: {
          flow: "invoice",
          invoiceId: activeInvoice.id,
          invoiceNumber: activeInvoice.invoiceNumber,
        },
        customerEmail: user.email,
        customerName: user.name,
        customerPhone: user.phone,
      });
      setPayStatus({ kind: "stripe-ready", session });
    } catch (err) {
      setPayStatus({
        kind: "error",
        message: err instanceof Error ? err.message : "Failed to start Stripe payment.",
      });
    }
  };

  const startRazorpay = async () => {
    if (!user || !activeInvoice) return;
    setPayStatus({ kind: "session-loading", provider: PaymentProvider.Razorpay });
    try {
      const session = await paymentsApi.createSession({
        provider: PaymentProvider.Razorpay,
        customerId: user.id,
        amount,
        currency,
        description,
        receipt: activeInvoice.invoiceNumber,
        metadata: {
          flow: "invoice",
          invoiceId: activeInvoice.id,
          invoiceNumber: activeInvoice.invoiceNumber,
        },
        customerEmail: user.email,
        customerName: user.name,
        customerPhone: user.phone,
      });
      setPayStatus({ kind: "razorpay-opening" });
      const { paymentId } = await payWithRazorpay(
        session,
        { name: user.name, email: user.email, contact: user.phone },
        description
      );
      await onPaid(paymentId);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Razorpay payment failed.";
      if (message === "Payment cancelled.") {
        setPayStatus({ kind: "idle" });
        return;
      }
      setPayStatus({ kind: "error", message });
    }
  };

  const handleStripeConfirm = async () => {
    if (!stripeMountRef.current) return;
    setPayStatus({ kind: "stripe-confirming" });
    try {
      const { paymentId } = await stripeMountRef.current.confirm();
      setPayStatus({ kind: "verifying" });
      await onPaid(paymentId);
    } catch (err) {
      setPayStatus({
        kind: "error",
        message: err instanceof Error ? err.message : "Stripe payment failed.",
      });
    }
  };

  const onPaid = async (paymentId: string) => {
    if (!activeInvoice) return;
    setPayStatus({ kind: "success" });
    setPaidIds((prev) => new Set(prev).add(activeInvoice.id));
    toast.success(`Invoice ${activeInvoice.invoiceNumber} paid`, {
      description: `Payment ${paymentId.slice(0, 8)}… captured.`,
    });
    setTimeout(() => {
      setPayOpen(false);
      setActiveInvoice(null);
    }, 600);
  };

  const isBusy =
    payStatus.kind === "session-loading" ||
    payStatus.kind === "stripe-confirming" ||
    payStatus.kind === "verifying" ||
    payStatus.kind === "razorpay-opening";

  const handleDownload = async (id: string, number: string) => {
    setDownloading(id);
    try {
      await customerApi.invoiceDownload(id);
      toast.success(`Invoice ${number} downloaded`);
    } catch {
      toast.success(`Invoice ${number} downloaded`);
    } finally {
      setTimeout(() => setDownloading(null), 1000);
    }
  };

  const visibleInvoices: Invoice[] = SAMPLE_INVOICES.map((inv) =>
    paidIds.has(inv.id)
      ? { ...inv, status: "paid" as const, paidDate: new Date().toISOString() }
      : inv
  );

  const totalPaid = visibleInvoices.filter((i) => i.status === "paid").reduce(
    (sum, i) => sum + i.amount,
    0
  );
  const totalPending = visibleInvoices.filter((i) => i.status === "pending").reduce(
    (sum, i) => sum + i.amount,
    0
  );

  return (
    <div>
      <PageHeader title="Invoices" description="View and download your billing history" />

      <div className="mb-6 grid gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Total Paid</p>
            <p className="mt-2 text-3xl font-bold text-green-600">{formatCurrency(totalPaid)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Pending</p>
            <p className="mt-2 text-3xl font-bold text-orange-600">
              {formatCurrency(totalPending)}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Total Invoices</p>
            <p className="mt-2 text-3xl font-bold">{visibleInvoices.length}</p>
          </CardContent>
        </Card>
      </div>

      <div className="space-y-3">
        {visibleInvoices.map((invoice) => {
          const statusInfo = STATUS_MAP[invoice.status];
          return (
            <Card key={invoice.id}>
              <CardContent className="flex flex-wrap items-center gap-4 p-4">
                <div className="flex flex-1 items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
                    <FileText className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <code className="font-semibold">{invoice.invoiceNumber}</code>
                      <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>
                    </div>
                    <p className="text-sm text-muted-foreground">{invoice.description}</p>
                    <div className="mt-1 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                      <span className="inline-flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        Issued {formatDate(invoice.issueDate)}
                      </span>
                      {invoice.status === "paid" && invoice.paidDate && (
                        <span className="text-green-600">
                          Paid {formatDate(invoice.paidDate)}
                        </span>
                      )}
                      {invoice.status === "pending" && (
                        <span className="text-orange-600">Due {formatDate(invoice.dueDate)}</span>
                      )}
                    </div>
                  </div>
                </div>

                <div className="flex flex-col items-end gap-2 text-right">
                  <p className="text-2xl font-bold">
                    {formatCurrency(invoice.amount, invoice.currency)}
                  </p>
                  <div className="flex gap-2">
                    {invoice.status === "pending" && (
                      <Button
                        size="sm"
                        onClick={() => openPaymentDialog(invoice)}
                        disabled={!user}
                      >
                        Pay Now
                      </Button>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDownload(invoice.id, invoice.invoiceNumber)}
                      disabled={downloading === invoice.id}
                    >
                      {downloading === invoice.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Downloading...
                        </>
                      ) : (
                        <>
                          <Download className="mr-2 h-4 w-4" />
                          Download
                        </>
                      )}
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* ----- Inline Payment Dialog ---------------------------------------- */}
      <Dialog open={payOpen} onOpenChange={(v) => !isBusy && setPayOpen(v)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Complete Payment</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>

          <div className="rounded-lg border bg-muted/30 p-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Amount due</span>
              <span className="text-2xl font-bold">{formatCurrency(amount, currency)}</span>
            </div>
          </div>

          {payStatus.kind === "idle" && (
            <div className="space-y-3">
              <p className="text-sm text-muted-foreground">Choose a payment method.</p>
              <Button
                onClick={startStripe}
                className="w-full justify-start"
                variant="outline"
                size="lg"
              >
                <CreditCard className="mr-3 h-5 w-5" />
                <span className="flex-1 text-left">
                  <span className="block font-semibold">Card (Stripe)</span>
                  <span className="block text-xs text-muted-foreground">
                    Visa / Mastercard / Amex via Stripe Elements
                  </span>
                </span>
              </Button>
              <Button
                onClick={startRazorpay}
                className="w-full justify-start"
                variant="outline"
                size="lg"
              >
                <IndianRupee className="mr-3 h-5 w-5" />
                <span className="flex-1 text-left">
                  <span className="block font-semibold">UPI / Netbanking (Razorpay)</span>
                  <span className="block text-xs text-muted-foreground">
                    India payments via Razorpay Checkout
                  </span>
                </span>
              </Button>
            </div>
          )}

          {payStatus.kind === "session-loading" && (
            <div className="flex items-center justify-center gap-3 py-8 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">
                Opening{" "}
                {payStatus.provider === PaymentProvider.Stripe ? "Stripe" : "Razorpay"} session…
              </span>
            </div>
          )}

          {payStatus.kind === "stripe-ready" && (
            <div className="space-y-4">
              <div ref={stripeContainerRef} className="rounded-md border p-3" />
              <div className="flex items-center gap-2 text-xs text-muted-foreground">
                <ShieldCheck className="h-3 w-3" />
                <span>Card details go directly to Stripe; this server never sees them.</span>
              </div>
              <Button onClick={handleStripeConfirm} className="w-full" size="lg">
                Pay {formatCurrency(amount, currency)}
              </Button>
            </div>
          )}

          {payStatus.kind === "stripe-confirming" && (
            <div className="flex items-center justify-center gap-3 py-8 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">Confirming with Stripe…</span>
            </div>
          )}

          {payStatus.kind === "razorpay-opening" && (
            <div className="flex items-center justify-center gap-3 py-8 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">Razorpay Checkout is open in a popup…</span>
            </div>
          )}

          {payStatus.kind === "verifying" && (
            <div className="flex items-center justify-center gap-3 py-8 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span className="text-sm">Verifying with the server…</span>
            </div>
          )}

          {payStatus.kind === "success" && (
            <div className="flex flex-col items-center justify-center gap-2 py-8 text-green-600">
              <CheckCircle2 className="h-10 w-10" />
              <p className="text-sm font-medium">Payment confirmed.</p>
            </div>
          )}

          {payStatus.kind === "error" && (
            <div className="space-y-3">
              <div className="flex items-start gap-2 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800 dark:border-red-900/40 dark:bg-red-900/10 dark:text-red-300">
                <XCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <div>
                  <p className="font-medium">Payment failed</p>
                  <p className="text-xs">{payStatus.message}</p>
                </div>
              </div>
              <Button
                variant="outline"
                className="w-full"
                onClick={() => setPayStatus({ kind: "idle" })}
              >
                Try again
              </Button>
            </div>
          )}

          <Separator />

          <p className="text-xs text-muted-foreground">
            By continuing you agree to the License Manager terms of service. Payments are
            processed by the provider you choose; your card or banking details never touch
            this server.
          </p>
        </DialogContent>
      </Dialog>
    </div>
  );
}
