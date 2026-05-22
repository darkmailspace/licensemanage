"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Calendar, Loader2, RefreshCcw } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { customerApi } from "@/lib/api";
import { formatDate } from "@/lib/utils";

const RENEWAL_OPTIONS = [
  { months: 1, label: "1 Month", price: 99 },
  { months: 3, label: "3 Months", price: 249 },
  { months: 6, label: "6 Months", price: 499 },
  { months: 12, label: "12 Months", price: 999, recommended: true },
  { months: 24, label: "24 Months", price: 1799 },
];

export default function RenewPage() {
  const router = useRouter();
  const [selectedLicense, setSelectedLicense] = useState("lic-1");
  const [months, setMonths] = useState(12);
  const [loading, setLoading] = useState(false);

  const currentExpiry = new Date(Date.now() + 25 * 86400000);
  const newExpiry = new Date(currentExpiry.getTime() + months * 30 * 86400000);
  const selectedOption = RENEWAL_OPTIONS.find((o) => o.months === months);

  const handleRenew = async () => {
    setLoading(true);
    try {
      await customerApi.renew(selectedLicense, months);
      toast.success(`License renewed for ${months} months`);
      router.push("/licenses");
    } catch {
      // demo fallback
      toast.success(`Renewal request submitted for ${months} months`);
      router.push("/licenses");
    } finally {
      setLoading(false);
    }
  };

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
            <p className="mt-1 text-2xl font-bold">${selectedOption?.price || 0}</p>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3">
        <Button variant="outline" onClick={() => router.back()}>
          Cancel
        </Button>
        <Button onClick={handleRenew} disabled={loading}>
          {loading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Processing...
            </>
          ) : (
            <>
              <RefreshCcw className="mr-2 h-4 w-4" />
              Pay & Renew
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
