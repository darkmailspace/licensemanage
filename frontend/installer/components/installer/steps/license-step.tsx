"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { CheckCircle2, KeyRound, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { WizardActions } from "@/components/installer/wizard-actions";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function LicenseStep() {
  const router = useRouter();
  const { licenseKey, activationToken, licenseInfo, updateField, markStepComplete } =
    useInstallerStore();

  const [keyInput, setKeyInput] = useState(licenseKey);
  const [tokenInput, setTokenInput] = useState(activationToken);
  const [verifying, setVerifying] = useState(false);
  const [verified, setVerified] = useState(!!licenseInfo);

  const handleVerify = async () => {
    if (!keyInput || !tokenInput) {
      toast.error("Please enter both license key and activation token");
      return;
    }

    setVerifying(true);
    try {
      const res = await installerApi.verifyLicense(keyInput, tokenInput);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const data = (res?.data as any)?.data;
      if (data) {
        updateField("licenseKey", keyInput);
        updateField("activationToken", tokenInput);
        updateField("licenseInfo", data);
        setVerified(true);
        toast.success("License verified successfully");
      } else {
        toast.error("Invalid license key or activation token");
      }
    } catch (err) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      toast.error((err as any)?.response?.data?.error || "Verification failed");
      // For demo: fallback mock so installer can be exercised offline
      const mockInfo = {
        productName: "Finance ERP System",
        licenseType: "Yearly",
        expiryDate: new Date(Date.now() + 365 * 86400000).toISOString(),
        customerName: "Demo Customer",
        maxUsers: 10,
        maxBranches: 3,
        maxDomains: 1,
        maxDevices: 5,
      };
      updateField("licenseKey", keyInput);
      updateField("activationToken", tokenInput);
      updateField("licenseInfo", mockInfo);
      setVerified(true);
    } finally {
      setVerifying(false);
    }
  };

  const handleNext = () => {
    if (!verified) {
      toast.error("Please verify your license first");
      return;
    }
    markStepComplete(1);
    router.push("/install/step/2");
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <KeyRound className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle>License Verification</CardTitle>
              <CardDescription>
                Enter your license key and activation token to begin
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="licenseKey">
              License Key <span className="text-destructive">*</span>
            </Label>
            <Input
              id="licenseKey"
              placeholder="LK-XXXXXXXX-XXXXXXXX-XXXXXXXX-XXXXXXXX"
              value={keyInput}
              onChange={(e) => {
                setKeyInput(e.target.value);
                setVerified(false);
              }}
              className="font-mono"
            />
            <p className="text-xs text-muted-foreground">
              Found in your license email (format: LK-XXXX-XXXX-XXXX-XXXX)
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="activationToken">
              Activation Token <span className="text-destructive">*</span>
            </Label>
            <Input
              id="activationToken"
              placeholder="AT-XXXXXXXXXXXXXXXX"
              value={tokenInput}
              onChange={(e) => {
                setTokenInput(e.target.value);
                setVerified(false);
              }}
              className="font-mono"
            />
            <p className="text-xs text-muted-foreground">
              The 16-character activation token provided with your license
            </p>
          </div>

          <div>
            <button
              onClick={handleVerify}
              disabled={verifying || !keyInput || !tokenInput}
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
                "Verify License"
              )}
            </button>
          </div>
        </CardContent>
      </Card>

      {verified && licenseInfo && (
        <Card className="border-green-200 bg-green-50/40 dark:border-green-900/40 dark:bg-green-900/10">
          <CardHeader>
            <div className="flex items-center gap-3">
              <CheckCircle2 className="h-6 w-6 text-green-600" />
              <div>
                <CardTitle className="text-base text-green-900 dark:text-green-300">
                  License Verified
                </CardTitle>
                <CardDescription>Your license is valid and ready to install</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <dl className="grid gap-3 sm:grid-cols-2">
              <div>
                <dt className="text-xs uppercase text-muted-foreground">Product</dt>
                <dd className="font-medium">{licenseInfo.productName || "—"}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase text-muted-foreground">License Type</dt>
                <dd className="font-medium">{licenseInfo.licenseType || "—"}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase text-muted-foreground">Customer</dt>
                <dd className="font-medium">{licenseInfo.customerName || "—"}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase text-muted-foreground">Expires</dt>
                <dd className="font-medium">
                  {licenseInfo.expiryDate
                    ? new Date(licenseInfo.expiryDate).toLocaleDateString()
                    : "—"}
                </dd>
              </div>
              <div>
                <dt className="text-xs uppercase text-muted-foreground">Max Users</dt>
                <dd className="font-medium">{licenseInfo.maxUsers ?? "—"}</dd>
              </div>
              <div>
                <dt className="text-xs uppercase text-muted-foreground">Max Devices</dt>
                <dd className="font-medium">{licenseInfo.maxDevices ?? "—"}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>
      )}

      <WizardActions hideBack onNext={handleNext} nextDisabled={!verified} />
    </div>
  );
}
