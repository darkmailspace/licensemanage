"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Save } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
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
import { LICENSE_TYPES } from "@/lib/constants";

interface LicenseFormData {
  customerName: string;
  companyName: string;
  email: string;
  phone: string;
  productId: string;
  licenseType: number;
  maxUsers: number;
  maxBranches: number;
  maxDomains: number;
  maxDevices: number;
  maxApiCalls: number;
  maxStorageGB: number;
  startDate: string;
  expiryDate: string;
  price: number;
  currency: string;
  domainLockEnabled: boolean;
  hardwareLockEnabled: boolean;
  ipLockEnabled: boolean;
  countryLockEnabled: boolean;
  autoRenewal: boolean;
  notes: string;
}

const DEFAULT_FORM: LicenseFormData = {
  customerName: "",
  companyName: "",
  email: "",
  phone: "",
  productId: "",
  licenseType: 5,
  maxUsers: 10,
  maxBranches: 3,
  maxDomains: 1,
  maxDevices: 5,
  maxApiCalls: 100000,
  maxStorageGB: 50,
  startDate: new Date().toISOString().split("T")[0],
  expiryDate: new Date(Date.now() + 365 * 86400000).toISOString().split("T")[0],
  price: 999,
  currency: "USD",
  domainLockEnabled: true,
  hardwareLockEnabled: false,
  ipLockEnabled: false,
  countryLockEnabled: false,
  autoRenewal: false,
  notes: "",
};

export default function CreateLicensePage() {
  const router = useRouter();
  const [form, setForm] = useState<LicenseFormData>(DEFAULT_FORM);
  const [loading, setLoading] = useState(false);

  const updateField = <K extends keyof LicenseFormData>(key: K, value: LicenseFormData[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      // await licensesApi.create(form);
      await new Promise((res) => setTimeout(res, 1000));
      toast.success("License created successfully");
      router.push("/licenses");
    } catch {
      toast.error("Failed to create license");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Create License"
        description="Generate a new license with custom configuration"
        actions={
          <Link href="/licenses">
            <Button variant="outline">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Button>
          </Link>
        }
      />

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Customer Information */}
        <Card>
          <CardHeader>
            <CardTitle>Customer Information</CardTitle>
            <CardDescription>Customer details for this license</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="customerName">
                Customer Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="customerName"
                placeholder="John Doe"
                value={form.customerName}
                onChange={(e) => updateField("customerName", e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="companyName">Company Name</Label>
              <Input
                id="companyName"
                placeholder="ABC Technologies"
                value={form.companyName}
                onChange={(e) => updateField("companyName", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">
                Email <span className="text-destructive">*</span>
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="customer@example.com"
                value={form.email}
                onChange={(e) => updateField("email", e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="phone">Phone</Label>
              <Input
                id="phone"
                placeholder="+1 234 567 8900"
                value={form.phone}
                onChange={(e) => updateField("phone", e.target.value)}
              />
            </div>
          </CardContent>
        </Card>

        {/* License Configuration */}
        <Card>
          <CardHeader>
            <CardTitle>License Configuration</CardTitle>
            <CardDescription>Set the license type and validity</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Product</Label>
              <Select
                value={form.productId}
                onValueChange={(v) => updateField("productId", v)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select product" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="prod-1">Finance ERP System v1.0.0</SelectItem>
                  <SelectItem value="prod-2">CRM Pro v2.0.0</SelectItem>
                  <SelectItem value="prod-3">Inventory Manager v1.5.0</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>License Type</Label>
              <Select
                value={String(form.licenseType)}
                onValueChange={(v) => updateField("licenseType", Number(v))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {LICENSE_TYPES.map((t) => (
                    <SelectItem key={t.value} value={String(t.value)}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="startDate">Start Date</Label>
              <Input
                id="startDate"
                type="date"
                value={form.startDate}
                onChange={(e) => updateField("startDate", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="expiryDate">Expiry Date</Label>
              <Input
                id="expiryDate"
                type="date"
                value={form.expiryDate}
                onChange={(e) => updateField("expiryDate", e.target.value)}
              />
            </div>
          </CardContent>
        </Card>

        {/* Limits */}
        <Card>
          <CardHeader>
            <CardTitle>Usage Limits</CardTitle>
            <CardDescription>Configure usage and resource limits</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-3">
            {[
              { key: "maxUsers", label: "Max Users" },
              { key: "maxBranches", label: "Max Branches" },
              { key: "maxDomains", label: "Max Domains" },
              { key: "maxDevices", label: "Max Devices" },
              { key: "maxApiCalls", label: "Max API Calls" },
              { key: "maxStorageGB", label: "Max Storage (GB)" },
            ].map((field) => (
              <div key={field.key} className="space-y-2">
                <Label htmlFor={field.key}>{field.label}</Label>
                <Input
                  id={field.key}
                  type="number"
                  min={0}
                  value={form[field.key as keyof LicenseFormData] as number}
                  onChange={(e) =>
                    updateField(field.key as keyof LicenseFormData, Number(e.target.value) as never)
                  }
                />
              </div>
            ))}
          </CardContent>
        </Card>

        {/* Pricing & Security */}
        <Card>
          <CardHeader>
            <CardTitle>Pricing & Security</CardTitle>
            <CardDescription>Configure billing and security locking</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="price">Price</Label>
                <Input
                  id="price"
                  type="number"
                  step="0.01"
                  min={0}
                  value={form.price}
                  onChange={(e) => updateField("price", Number(e.target.value))}
                />
              </div>
              <div className="space-y-2">
                <Label>Currency</Label>
                <Select
                  value={form.currency}
                  onValueChange={(v) => updateField("currency", v)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="USD">USD</SelectItem>
                    <SelectItem value="EUR">EUR</SelectItem>
                    <SelectItem value="GBP">GBP</SelectItem>
                    <SelectItem value="INR">INR</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-center justify-between rounded-md border p-3">
                <Label htmlFor="autoRenewal" className="cursor-pointer">
                  Auto Renewal
                </Label>
                <Switch
                  id="autoRenewal"
                  checked={form.autoRenewal}
                  onCheckedChange={(v) => updateField("autoRenewal", v)}
                />
              </div>
            </div>

            <div className="grid gap-3 md:grid-cols-2">
              {[
                { key: "domainLockEnabled", label: "Domain Lock" },
                { key: "hardwareLockEnabled", label: "Hardware Lock" },
                { key: "ipLockEnabled", label: "IP Whitelist" },
                { key: "countryLockEnabled", label: "Country Restriction" },
              ].map((field) => (
                <div
                  key={field.key}
                  className="flex items-center justify-between rounded-md border p-3"
                >
                  <Label htmlFor={field.key} className="cursor-pointer">
                    {field.label}
                  </Label>
                  <Switch
                    id={field.key}
                    checked={form[field.key as keyof LicenseFormData] as boolean}
                    onCheckedChange={(v) =>
                      updateField(field.key as keyof LicenseFormData, v as never)
                    }
                  />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Notes */}
        <Card>
          <CardHeader>
            <CardTitle>Notes</CardTitle>
            <CardDescription>Additional notes for this license</CardDescription>
          </CardHeader>
          <CardContent>
            <Textarea
              placeholder="Add any additional notes about this license..."
              rows={4}
              value={form.notes}
              onChange={(e) => updateField("notes", e.target.value)}
            />
          </CardContent>
        </Card>

        {/* Form Actions */}
        <div className="flex justify-end gap-3">
          <Link href="/licenses">
            <Button type="button" variant="outline" disabled={loading}>
              Cancel
            </Button>
          </Link>
          <Button type="submit" disabled={loading}>
            {loading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Creating...
              </>
            ) : (
              <>
                <Save className="mr-2 h-4 w-4" />
                Create License
              </>
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
