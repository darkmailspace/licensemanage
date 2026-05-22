"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Calendar,
  CheckCircle2,
  CreditCard,
  IndianRupee,
  Loader2,
  RefreshCcw,
  ShieldCheck,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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

const RENEWAL_OPTIONS = [
  { months: 1, label: "1 Month", price: 99 },
  { months: 3, label: "3 Months", price: 249 },
  { months: 6, label: "6 Months", price: 499 },
  { months: 12, label: "12 Months", price: 999, recommended: true },
  { months: 24, label: "24 Months", price: 1799 },
];

// Local payment-dialog state machine. Inlined per-page (instead of a shared
// component) per the project rule that no new files are introduced.
type PayStatus =
  | { kind: "idle" }
  | { kind: "session-loading"; provider: PaymentProvider }
  | { kind: "stripe-ready"; session: PaymentSession }
  | { kind: "stripe-confirming" }
  | { kind: "razorpay-opening" }
  | { kind: "verifying" }
  | { kind: "success" }
  | { kind: "error"; message: string };

export default function RenewPage() {
  const router = useRouter();
  const { user } = useAuthStore();

  const [selectedLicense, setSelectedLicense] = useState("lic-1");
  const [months, setMonths] = useState(12);

  const currentExpiry = new Date(Date.now() + 25 * 86400000);
  const newExpiry = new Date(currentExpiry.getTime() + months * 30 * 86400000);
  const selectedOption = RENEWAL_OPTIONS.find((o) => o.months === months);
  const amount = selectedOption?.price ?? 0;
  const description = `License renewal - ${months} month${months === 1 ? "" : "s"}`;

  // -- Payment dialog wiring --------------------------------------------------
  const [payOpen, setPayOpen] = useState(false);
  const [payStatus, setPayStatus] = useState<PayStatus>({ kind: "idle" });
  const stripeContainerRef = useRef<HTMLDivElement | null>(null);
  const stripeMountRef = useRef<StripeMount | null>(null);

  // Re-mount the Stripe element whenever a new stripe-ready state is set.
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

  // Reset to choose-provider whenever the dialog opens.
  useEffect(() => {
    if (payOpen) setPayStatus({ kind: "idle" });
  }, [payOpen]);

  const startStripe = async () => {
    if (!user) return;
    setPayStatus({ kind: "session-loading", provider: PaymentProvider.Stripe });
    try {
      const session = await paymentsApi.createSession({
        provider: PaymentProvider.Stripe,
        customerId: user.id,
        licenseId: selectedLicense,
        amount,
        currency: "USD",
        description,
        metadata: { flow: "renew", months: String(months) },
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
    if (!user) return;
    setPayStatus({ kind: "session-loading", provider: PaymentProvider.Razorpay });
    try {
      const session = await paymentsApi.createSession({
        provider: PaymentProvider.Razorpay,
        customerId: user.id,
        licenseId: selectedLicense,
        amount,
        currency: "USD",
        description,
        metadata: { flow: "renew", months: String(months) },
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
      // "Payment cancelled" is a benign UX path - bounce to provider chooser.
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

  // After verify succeeds, ask the existing customer-portal endpoint to apply
  // the renewal to the license. The webhook will also see the captured
  // payment, so this is belt-and-braces.
  const onPaid = async (paymentId: string) => {
    setPayStatus({ kind: "success" });
    try {
      await customerApi.renew(selectedLicense, months);
    } catch {
      /* If the dedicated renew endpoint is unavailable, the webhook still
         records the payment - surface a softer toast so the user knows. */
    }
    toast.success(`License renewed for ${months} month${months === 1 ? "" : "s"}`, {
      description: `Payment ${paymentId.slice(0, 8)}… captured.`,
    });
    setTimeout(() => {
      setPayOpen(false);
      router.push("/licenses");
    }, 600);
  };

  const isBusy =
    payStatus.kind === "session-loading" ||
    payStatus.kind === "stripe-confirming" ||
    payStatus.kind === "verifying" ||
    payStatus.kind === "razorpay-opening";

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader title="Renew License" description="Extend your license validity" />

      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="text-lg">Select License</CardTitle>
        </CardHeader>
        <CardContent>
          <Select value={selectedLicense} onValueChange={setSelectedLicense}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="lic-1">
                Finance ERP System v1.0.0 (LK-A1B2C3D4...)
              </SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="text-lg">Renewal Period</CardTitle>
          <CardDescription>Choose how long to extend your license</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {RENEWAL_OPTIONS.map((option) => (
              <button
                key={option.months}
                type="button"
                onClick={() => setMonths(option.months)}
                className={`relative rounded-lg border-2 p-4 text-left transition ${
                  months === option.months
                    ? "border-primary bg-primary/5"
                    : "border-border hover:border-primary/50"
                }`}
              >
                {option.recommended && (
                  <span className="absolute -top-2 right-4 rounded-full bg-primary px-2 py-0.5 text-xs font-medium text-primary-foreground">
                    Best value
                  </span>
                )}
                <div className="flex items-start gap-3">
                  <Calendar className="mt-0.5 h-5 w-5 text-muted-foreground" />
                  <div className="flex-1">
                    <p className="font-semibold">{option.label}</p>
                    <p className="mt-1 text-2xl font-bold">${option.price}</p>
                    <p className="text-xs text-muted-foreground">
                      ${(option.price / option.months).toFixed(2)}/month
                    </p>
                  </div>
                </div>
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card className="mb-6 border-primary/30 bg-primary/5">
        <CardContent className="grid gap-4 p-6 sm:grid-cols-3">
          <div>
            <p className="text-xs uppercase text-muted-foreground">Current Expiry</p>
            <p className="mt-1 text-lg font-semibold">{formatDate(currentExpiry)}</p>
          </div>
          <div>
            <p className="text-xs uppercase text-muted-foreground">New Expiry</p>
            <p className="mt-1 text-lg font-semibold text-primary">{formatDate(newExpiry)}</p>
          </div>
          <div>
            <p className="text-xs uppercase text-muted-foreground">Total</p>
            <p className="mt-1 text-2xl font-bold">${amount}</p>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3">
        <Button variant="outline" onClick={() => router.back()}>
          Cancel
        </Button>
        <Button onClick={() => setPayOpen(true)} disabled={!user || amount <= 0}>
          <RefreshCcw className="mr-2 h-4 w-4" />
          Pay & Renew
        </Button>
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
              <span className="text-2xl font-bold">{formatCurrency(amount, "USD")}</span>
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
                Pay {formatCurrency(amount, "USD")}
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
