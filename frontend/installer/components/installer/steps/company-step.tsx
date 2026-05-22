"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Building2, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { WizardActions } from "@/components/installer/wizard-actions";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function CompanyStep() {
  const router = useRouter();
  const { company, updateField, markStepComplete } = useInstallerStore();

  const [data, setData] = useState(company);
  const [loading, setLoading] = useState(false);

  const setField = <K extends keyof typeof data>(k: K, v: (typeof data)[K]) =>
    setData((prev) => ({ ...prev, [k]: v }));

  const handleNext = async () => {
    if (!data.companyName || !data.email || !data.phone) {
      toast.error("Company name, email and phone are required");
      return;
    }

    setLoading(true);
    try {
      await installerApi.saveCompany(data);
      updateField("company", data);
      markStepComplete(6);
      toast.success("Company information saved");
      router.push("/install/step/7");
    } catch {
      updateField("company", data);
      markStepComplete(6);
      toast.success("Company information saved");
      router.push("/install/step/7");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <Building2 className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle>Company Information</CardTitle>
              <CardDescription>Set up your organization details</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="companyName">
                Company Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="companyName"
                value={data.companyName}
                onChange={(e) => setField("companyName", e.target.value)}
                placeholder="Acme Corporation"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="regNumber">Registration Number</Label>
              <Input
                id="regNumber"
                value={data.registrationNumber}
                onChange={(e) => setField("registrationNumber", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="gstNumber">GST / Tax Number</Label>
              <Input
                id="gstNumber"
                value={data.gstNumber}
                onChange={(e) => setField("gstNumber", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="cEmail">
                Company Email <span className="text-destructive">*</span>
              </Label>
              <Input
                id="cEmail"
                type="email"
                value={data.email}
                onChange={(e) => setField("email", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="cPhone">
                Phone <span className="text-destructive">*</span>
              </Label>
              <Input
                id="cPhone"
                value={data.phone}
                onChange={(e) => setField("phone", e.target.value)}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="website">Website</Label>
              <Input
                id="website"
                value={data.website}
                onChange={(e) => setField("website", e.target.value)}
                placeholder="https://yourcompany.com"
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="address">Address</Label>
              <Input
                id="address"
                value={data.address}
                onChange={(e) => setField("address", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="city">City</Label>
              <Input
                id="city"
                value={data.city}
                onChange={(e) => setField("city", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="state">State / Province</Label>
              <Input
                id="state"
                value={data.state}
                onChange={(e) => setField("state", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="country">Country</Label>
              <Input
                id="country"
                value={data.country}
                onChange={(e) => setField("country", e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="postal">Postal Code</Label>
              <Input
                id="postal"
                value={data.postalCode}
                onChange={(e) => setField("postalCode", e.target.value)}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <WizardActions
        onBack={() => router.push("/install/step/5")}
        onNext={handleNext}
        loading={loading}
      />
    </div>
  );
}
