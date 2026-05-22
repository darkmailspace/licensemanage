"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Calendar, Loader2, RefreshCcw } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { formatDate } from "@/lib/utils";

const RENEWAL_OPTIONS = [
  { months: 1, label: "1 Month", price: 99 },
  { months: 3, label: "3 Months (Quarterly)", price: 249 },
  { months: 6, label: "6 Months (Half-Yearly)", price: 499 },
  { months: 12, label: "12 Months (Yearly)", price: 999, recommended: true },
  { months: 24, label: "24 Months (2 Years)", price: 1799 },
  { months: 36, label: "36 Months (3 Years)", price: 2499 },
];

export default function RenewLicensePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const [renewalMonths, setRenewalMonths] = useState(12);
  const [loading, setLoading] = useState(false);

  const currentExpiry = new Date(Date.now() + 30 * 86400000);
  const newExpiry = new Date(
    currentExpiry.getTime() + renewalMonths * 30 * 86400000
  );

  const selectedOption = RENEWAL_OPTIONS.find((o) => o.months === renewalMonths);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      await new Promise((res) => setTimeout(res, 800));
      toast.success(`License renewed for ${renewalMonths} months`);
      router.push(`/licenses/${id}`);
    } catch {
      toast.error("Failed to renew license");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader
        title="Renew License"
        description="Extend the license validity period"
        actions={
          <Link href={`/licenses/${id}`}>
            <Button variant="outline">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Button>
          </Link>
        }
      />

      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="text-lg">Current License</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <div className="rounded-md border p-4">
            <p className="text-xs uppercase text-muted-foreground">Current Expiry</p>
            <p className="mt-1 text-lg font-semibold">{formatDate(currentExpiry)}</p>
          </div>
          <div className="rounded-md border border-primary/30 bg-primary/5 p-4">
            <p className="text-xs uppercase text-primary">New Expiry After Renewal</p>
            <p className="mt-1 text-lg font-semibold text-primary">
              {formatDate(newExpiry)}
            </p>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Choose Renewal Period</CardTitle>
          <CardDescription>Select how long to extend this license</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 md:grid-cols-2">
            {RENEWAL_OPTIONS.map((option) => (
              <button
                key={option.months}
                type="button"
                onClick={() => setRenewalMonths(option.months)}
                className={`relative rounded-lg border-2 p-4 text-left transition-colors ${
                  renewalMonths === option.months
                    ? "border-primary bg-primary/5"
                    : "border-border hover:border-primary/50"
                }`}
              >
                {option.recommended && (
                  <span className="absolute -top-2 right-4 rounded-full bg-primary px-2 py-0.5 text-xs font-medium text-primary-foreground">
                    Recommended
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

          <div className="space-y-2">
            <Label>Or enter custom period (months)</Label>
            <Select
              value={String(renewalMonths)}
              onValueChange={(v) => setRenewalMonths(Number(v))}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {[1, 2, 3, 6, 12, 24, 36, 60].map((m) => (
                  <SelectItem key={m} value={String(m)}>
                    {m} {m === 1 ? "month" : "months"}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <Card className="mt-6 border-primary/30 bg-primary/5">
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">Total to Pay</p>
              <p className="text-3xl font-bold">${selectedOption?.price || 0}</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-muted-foreground">Renewal Period</p>
              <p className="text-xl font-semibold">{renewalMonths} months</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="mt-6 flex justify-end gap-3">
        <Link href={`/licenses/${id}`}>
          <Button variant="outline" disabled={loading}>
            Cancel
          </Button>
        </Link>
        <Button onClick={handleSubmit} disabled={loading}>
          {loading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Processing...
            </>
          ) : (
            <>
              <RefreshCcw className="mr-2 h-4 w-4" />
              Renew for {renewalMonths} months
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
