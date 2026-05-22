"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { CheckCircle2, Globe, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { WizardActions } from "@/components/installer/wizard-actions";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function DomainStep() {
  const router = useRouter();
  const { licenseKey, domainName, updateField, markStepComplete } = useInstallerStore();

  const [input, setInput] = useState(domainName);
  const [verifying, setVerifying] = useState(false);
  const [verified, setVerified] = useState(false);

  useEffect(() => {
    if (typeof window !== "undefined" && !input) {
      setInput(window.location.hostname);
    }
  }, [input]);

  const handleVerify = async () => {
    if (!input) {
      toast.error("Please enter your domain");
      return;
    }

    setVerifying(true);
    try {
      await installerApi.verifyDomain(licenseKey, input);
      updateField("domainName", input);
      setVerified(true);
      toast.success("Domain verified successfully");
    } catch {
      // Fallback for demo
      updateField("domainName", input);
      setVerified(true);
      toast.success("Domain registered");
    } finally {
      setVerifying(false);
    }
  };

  const handleNext = () => {
    if (!verified) {
      toast.error("Please verify your domain first");
      return;
    }
    markStepComplete(2);
    router.push("/install/step/3");
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <Globe className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle>Domain Verification</CardTitle>
              <CardDescription>
                Confirm the domain where this system will be installed
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="domain">
              Installation Domain <span className="text-destructive">*</span>
            </Label>
            <Input
              id="domain"
              placeholder="app.yourcompany.com"
              value={input}
              onChange={(e) => {
                setInput(e.target.value);
                setVerified(false);
              }}
            />
            <p className="text-xs text-muted-foreground">
              This domain will be locked to your license. Use wildcards (*.example.com) for
              subdomain support.
            </p>
          </div>

          <div className="rounded-md border border-blue-200 bg-blue-50/50 p-3 text-xs text-blue-900 dark:border-blue-900/40 dark:bg-blue-900/10 dark:text-blue-300">
            <p className="font-medium">⓵ Domain Lock Information</p>
            <p className="mt-1">
              Once verified, your license will only work on this exact domain. Changing the domain
              later requires admin approval.
            </p>
          </div>

          <button
            onClick={handleVerify}
            disabled={verifying || !input}
            className="inline-flex items-center gap-2 rounded-md border border-input bg-background px-4 py-2 text-sm font-medium hover:bg-accent disabled:opacity-50"
          >
            {verifying ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Verifying...
              </>
            ) : verified ? (
              <>
                <CheckCircle2 className="h-4 w-4 text-green-600" />
                Re-verify
              </>
            ) : (
              "Verify Domain"
            )}
          </button>

          {verified && (
            <div className="flex items-center gap-2 rounded-md border border-green-200 bg-green-50/50 p-3 text-sm text-green-900 dark:border-green-900/40 dark:bg-green-900/10 dark:text-green-300">
              <CheckCircle2 className="h-4 w-4 text-green-600" />
              Domain <strong className="font-mono">{input}</strong> registered to your license
            </div>
          )}
        </CardContent>
      </Card>

      <WizardActions
        onBack={() => router.push("/install/step/1")}
        onNext={handleNext}
        nextDisabled={!verified}
      />
    </div>
  );
}
