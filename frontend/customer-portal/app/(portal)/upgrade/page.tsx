"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Check,
  CheckCircle2,
  CreditCard,
  Crown,
  IndianRupee,
  Loader2,
  ShieldCheck,
  Sparkles,
  TrendingUp,
  XCircle,
  Zap,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
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
import { formatCurrency } from "@/lib/utils";
import { useAuthStore } from "@/stores/auth-store";

interface PlanFeature {
  text: string;
  included: boolean;
}

interface Plan {
  id: number;
  name: string;
  description: string;
  price: number;
  icon: React.ComponentType<{ className?: string }>;
  features: PlanFeature[];
  current?: boolean;
  recommended?: boolean;
}

const PLANS: Plan[] = [
  {
    id: 5,
    name: "Yearly",
    description: "Current plan",
    price: 999,
    icon: TrendingUp,
    current: true,
    features: [
      { text: "10 users", included: true },
      { text: "3 branches", included: true },
      { text: "5 devices", included: true },
      { text: "Basic API access", included: true },
      { text: "Email support", included: true },
      { text: "White label branding", included: false },
      { text: "Priority support", included: false },
      { text: "Dedicated manager", included: false },
    ],
  },
  {
    id: 8,
    name: "Enterprise",
    description: "For growing organizations",
    price: 4999,
    icon: Sparkles,
    recommended: true,
    features: [
      { text: "100 users", included: true },
      { text: "Unlimited branches", included: true },
      { text: "50 devices", included: true },
      { text: "Advanced API access", included: true },
      { text: "Priority email + phone support", included: true },
      { text: "White label branding", included: true },
      { text: "Multi-company support", included: true },
      { text: "Dedicated manager", included: false },
    ],
  },
  {
    id: 9,
    name: "Franchise",
    description: "Multi-location franchise",
    price: 9999,
    icon: Crown,
    features: [
      { text: "500 users", included: true },
      { text: "Unlimited everything", included: true },
      { text: "Unlimited devices", included: true },
      { text: "Premium API access", included: true },
      { text: "24/7 priority support", included: true },
      { text: "White label branding", included: true },
      { text: "Multi-company + Multi-tenant", included: true },
      { text: "Dedicated account manager", included: true },
    ],
  },
];

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

export default function UpgradePage() {
  const router = useRouter();
  const { user } = useAuthStore();

  // -- Payment dialog wiring --------------------------------------------------
  const [activePlan, setActivePlan] = useState<Plan | null>(null);
  const [payOpen, setPayOpen] = useState(false);
  const [payStatus, setPayStatus] = useState<PayStatus>({ kind: "idle" });
  const stripeContainerRef = useRef<HTMLDivElement | null>(null);
  const stripeMountRef = useRef<StripeMount | null>(null);

  const licenseId = "lic-1";
  const description = activePlan
    ? `Upgrade to ${activePlan.name} plan`
    : "Upgrade plan";
  const amount = activePlan?.price ?? 0;

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

  const openPaymentDialog = (plan: Plan) => {
    setActivePlan(plan);
    setPayOpen(true);
  };

  const startStripe = async () => {
    if (!user || !activePlan) return;
    setPayStatus({ kind: "session-loading", provider: PaymentProvider.Stripe });
    try {
      const session = await paymentsApi.createSession({
        provider: PaymentProvider.Stripe,
        customerId: user.id,
        licenseId,
        amount,
        currency: "USD",
        description,
        metadata: { flow: "upgrade", planId: String(activePlan.id), planName: activePlan.name },
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
    if (!user || !activePlan) return;
    setPayStatus({ kind: "session-loading", provider: PaymentProvider.Razorpay });
    try {
      const session = await paymentsApi.createSession({
        provider: PaymentProvider.Razorpay,
        customerId: user.id,
        licenseId,
        amount,
        currency: "USD",
        description,
        metadata: { flow: "upgrade", planId: String(activePlan.id), planName: activePlan.name },
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
    if (!activePlan) return;
    setPayStatus({ kind: "success" });
    try {
      await customerApi.upgrade(licenseId, activePlan.id);
    } catch {
      /* webhook will still record the captured payment */
    }
    toast.success(`Upgraded to ${activePlan.name}`, {
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
    <div>
      <PageHeader
        title="Upgrade Your Plan"
        description="Get more features, higher limits, and priority support"
      />

      <div className="grid gap-6 lg:grid-cols-3">
        {PLANS.map((plan) => {
          const Icon = plan.icon;
          return (
            <Card
              key={plan.id}
              className={`relative ${plan.recommended ? "border-primary shadow-lg" : ""}`}
            >
              {plan.recommended && (
                <span className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-primary px-3 py-1 text-xs font-medium text-primary-foreground">
                  Recommended
                </span>
              )}
              {plan.current && (
                <span className="absolute -top-3 right-4 rounded-full bg-green-600 px-3 py-1 text-xs font-medium text-white">
                  Current Plan
                </span>
              )}
              <CardContent className="p-6">
                <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
                  <Icon className="h-6 w-6 text-primary" />
                </div>

                <h3 className="text-2xl font-bold">{plan.name}</h3>
                <p className="text-sm text-muted-foreground">{plan.description}</p>

                <div className="my-6">
                  <span className="text-4xl font-bold">${plan.price}</span>
                  <span className="text-muted-foreground">/year</span>
                </div>

                <ul className="space-y-2.5">
                  {plan.features.map((f, i) => (
                    <li key={i} className="flex items-start gap-2 text-sm">
                      {f.included ? (
                        <Check className="mt-0.5 h-4 w-4 shrink-0 text-green-600" />
                      ) : (
                        <span className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground/40">—</span>
                      )}
                      <span className={f.included ? "" : "text-muted-foreground line-through"}>
                        {f.text}
                      </span>
                    </li>
                  ))}
                </ul>

                <Button
                  className="mt-6 w-full"
                  variant={plan.current ? "outline" : "default"}
                  disabled={plan.current || !user}
                  onClick={() => openPaymentDialog(plan)}
                >
                  {plan.current ? (
                    "Current Plan"
                  ) : (
                    <>
                      <Zap className="mr-2 h-4 w-4" />
                      Upgrade to {plan.name}
                    </>
                  )}
                </Button>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <Card className="mt-6 border-blue-200 bg-blue-50 dark:border-blue-900/40 dark:bg-blue-900/10">
        <CardContent className="p-4 text-sm">
          <p className="font-medium text-blue-900 dark:text-blue-300">Need a custom plan?</p>
          <p className="mt-1 text-blue-800 dark:text-blue-300/80">
            Contact our sales team for tailored enterprise solutions, OEM licensing, or custom
            volume discounts.
          </p>
        </CardContent>
      </Card>

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
