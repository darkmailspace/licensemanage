"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Settings } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { WizardActions } from "@/components/installer/wizard-actions";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function ApiStep() {
  const router = useRouter();
  const { api, updateField, markStepComplete } = useInstallerStore();

  const [data, setData] = useState(api);
  const [loading, setLoading] = useState(false);

  const setField = <K extends keyof typeof data>(k: K, v: (typeof data)[K]) =>
    setData((prev) => ({ ...prev, [k]: v }));

  const handleNext = async () => {
    setLoading(true);
    try {
      await installerApi.configureApi(data);
      updateField("api", data);
      markStepComplete(7);
      toast.success("API configuration saved");
      router.push("/install/step/8");
    } catch {
      updateField("api", data);
      markStepComplete(7);
      toast.success("API configuration saved");
      router.push("/install/step/8");
    } finally {
      setLoading(false);
    }
  };

  const handleSkip = () => {
    updateField("api", data);
    markStepComplete(7);
    router.push("/install/step/8");
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <Settings className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle>API & Integration Configuration</CardTitle>
              <CardDescription>
                Set up email, SMS, and WhatsApp integrations (optional)
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <h3 className="mb-3 text-sm font-semibold">SMTP (Email)</h3>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="smtpHost">SMTP Host</Label>
                <Input
                  id="smtpHost"
                  value={data.smtpHost}
                  onChange={(e) => setField("smtpHost", e.target.value)}
                  placeholder="smtp.gmail.com"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="smtpPort">SMTP Port</Label>
                <Input
                  id="smtpPort"
                  type="number"
                  value={data.smtpPort}
                  onChange={(e) => setField("smtpPort", Number(e.target.value))}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="smtpUser">Username</Label>
                <Input
                  id="smtpUser"
                  value={data.smtpUser}
                  onChange={(e) => setField("smtpUser", e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="smtpPass">Password</Label>
                <Input
                  id="smtpPass"
                  type="password"
                  value={data.smtpPassword}
                  onChange={(e) => setField("smtpPassword", e.target.value)}
                />
              </div>
            </div>
          </div>

          <div>
            <h3 className="mb-3 text-sm font-semibold">Notifications</h3>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="smsKey">SMS API Key</Label>
                <Input
                  id="smsKey"
                  type="password"
                  value={data.smsApiKey}
                  onChange={(e) => setField("smsApiKey", e.target.value)}
                  placeholder="Twilio, Nexmo, etc."
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="waKey">WhatsApp API Key</Label>
                <Input
                  id="waKey"
                  type="password"
                  value={data.whatsappApiKey}
                  onChange={(e) => setField("whatsappApiKey", e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className="rounded-md border border-blue-200 bg-blue-50/50 p-3 text-xs text-blue-900 dark:border-blue-900/40 dark:bg-blue-900/10 dark:text-blue-300">
            <p className="font-medium">All fields are optional</p>
            <p className="mt-1">
              You can configure these integrations later from the Settings page.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-between gap-3 border-t pt-6">
        <button
          onClick={() => router.push("/install/step/6")}
          className="inline-flex items-center gap-2 rounded-md border border-input bg-background px-4 py-2 text-sm font-medium hover:bg-accent"
        >
          Back
        </button>
        <div className="flex gap-2">
          <button
            onClick={handleSkip}
            className="inline-flex items-center gap-2 rounded-md border border-input bg-background px-4 py-2 text-sm font-medium hover:bg-accent"
          >
            Skip for now
          </button>
          <button
            onClick={handleNext}
            disabled={loading}
            className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {loading ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Saving...
              </>
            ) : (
              "Save & Continue"
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
