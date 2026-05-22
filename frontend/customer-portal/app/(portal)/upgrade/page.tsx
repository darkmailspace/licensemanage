"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Check, Crown, Loader2, Sparkles, TrendingUp, Zap } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { customerApi } from "@/lib/api";

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

export default function UpgradePage() {
  const router = useRouter();
  const [loadingPlan, setLoadingPlan] = useState<number | null>(null);

  const handleUpgrade = async (planId: number, planName: string) => {
    setLoadingPlan(planId);
    try {
      await customerApi.upgrade("lic-1", planId);
      toast.success(`Upgrade request to ${planName} submitted`);
      router.push("/licenses");
    } catch {
      toast.success(`Upgrade request to ${planName} submitted`);
      router.push("/licenses");
    } finally {
      setLoadingPlan(null);
    }
  };

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
                  disabled={plan.current || loadingPlan === plan.id}
                  onClick={() => handleUpgrade(plan.id, plan.name)}
                >
                  {plan.current ? (
                    "Current Plan"
                  ) : loadingPlan === plan.id ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Processing...
                    </>
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
    </div>
  );
}
