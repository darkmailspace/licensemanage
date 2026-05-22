"use client";

import { use, useState } from "react";
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

export default function EditLicensePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  const [form, setForm] = useState({
    maxUsers: 10,
    maxBranches: 3,
    maxDomains: 1,
    maxDevices: 5,
    maxApiCalls: 100000,
    maxStorageGB: 50,
    expiryDate: new Date(Date.now() + 335 * 86400000).toISOString().split("T")[0],
    domainLockEnabled: true,
    hardwareLockEnabled: false,
    ipLockEnabled: false,
    countryLockEnabled: false,
    autoRenewal: false,
    notes: "",
    internalNotes: "",
  });

  const update = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await new Promise((res) => setTimeout(res, 800));
      toast.success("License updated successfully");
      router.push(`/licenses/${id}`);
    } catch {
      toast.error("Failed to update license");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Edit License"
        description={`License ID: ${id}`}
        actions={
          <Link href={`/licenses/${id}`}>
            <Button variant="outline">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Button>
          </Link>
        }
      />

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Validity</CardTitle>
            <CardDescription>Update license expiry date</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 max-w-sm">
              <Label htmlFor="expiryDate">Expiry Date</Label>
              <Input
                id="expiryDate"
                type="date"
                value={form.expiryDate}
                onChange={(e) => update("expiryDate", e.target.value)}
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Usage Limits</CardTitle>
            <CardDescription>Modify resource and usage limits</CardDescription>
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
                  value={form[field.key as keyof typeof form] as number}
                  onChange={(e) =>
                    update(field.key as keyof typeof form, Number(e.target.value) as never)
                  }
                />
              </div>
            ))}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Security Settings</CardTitle>
            <CardDescription>Toggle security features</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-3 md:grid-cols-2">
            {[
              { key: "domainLockEnabled", label: "Domain Lock" },
              { key: "hardwareLockEnabled", label: "Hardware Lock" },
              { key: "ipLockEnabled", label: "IP Whitelist" },
              { key: "countryLockEnabled", label: "Country Restriction" },
              { key: "autoRenewal", label: "Auto Renewal" },
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
                  checked={form[field.key as keyof typeof form] as boolean}
                  onCheckedChange={(v) =>
                    update(field.key as keyof typeof form, v as never)
                  }
                />
              </div>
            ))}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Notes</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="notes">Public Notes</Label>
              <Textarea
                id="notes"
                rows={3}
                value={form.notes}
                onChange={(e) => update("notes", e.target.value)}
                placeholder="Notes visible to customer..."
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="internalNotes">Internal Notes</Label>
              <Textarea
                id="internalNotes"
                rows={3}
                value={form.internalNotes}
                onChange={(e) => update("internalNotes", e.target.value)}
                placeholder="Internal notes (not visible to customer)..."
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end gap-3">
          <Link href={`/licenses/${id}`}>
            <Button type="button" variant="outline" disabled={loading}>
              Cancel
            </Button>
          </Link>
          <Button type="submit" disabled={loading}>
            {loading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Saving...
              </>
            ) : (
              <>
                <Save className="mr-2 h-4 w-4" />
                Save Changes
              </>
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
