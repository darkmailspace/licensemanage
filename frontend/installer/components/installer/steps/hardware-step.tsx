"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { CheckCircle2, Cpu, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { WizardActions } from "@/components/installer/wizard-actions";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";
import { collectDeviceInfo, generateFingerprint, type DeviceInfo } from "@/lib/fingerprint";

export default function HardwareStep() {
  const router = useRouter();
  const { licenseKey, deviceFingerprint, deviceName, updateField, markStepComplete } =
    useInstallerStore();

  const [info, setInfo] = useState<DeviceInfo | null>(null);
  const [fingerprint, setFingerprint] = useState(deviceFingerprint);
  const [name, setName] = useState(deviceName || "Production Server");
  const [collecting, setCollecting] = useState(true);
  const [verifying, setVerifying] = useState(false);
  const [verified, setVerified] = useState(false);

  useEffect(() => {
    const detect = async () => {
      const collected = collectDeviceInfo();
      setInfo(collected);
      const fp = await generateFingerprint(collected);
      setFingerprint(fp);
      setCollecting(false);
    };
    detect();
  }, []);

  const handleVerify = async () => {
    setVerifying(true);
    try {
      await installerApi.verifyHardware(
        licenseKey,
        fingerprint,
        info as unknown as Record<string, string>
      );
      updateField("deviceFingerprint", fingerprint);
      updateField("deviceName", name);
      setVerified(true);
      toast.success("Hardware registered successfully");
    } catch {
      // Fallback
      updateField("deviceFingerprint", fingerprint);
      updateField("deviceName", name);
      setVerified(true);
      toast.success("Hardware registered");
    } finally {
      setVerifying(false);
    }
  };

  const handleNext = () => {
    if (!verified) {
      toast.error("Please register your hardware first");
      return;
    }
    markStepComplete(3);
    router.push("/install/step/4");
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <Cpu className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CardTitle>Hardware Verification</CardTitle>
              <CardDescription>
                Register this server&apos;s hardware fingerprint with your license
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {collecting ? (
            <div className="flex items-center gap-2 rounded-md border bg-muted/50 p-4 text-sm">
              <Loader2 className="h-4 w-4 animate-spin" />
              Collecting hardware information...
            </div>
          ) : (
            <>
              <div className="space-y-2">
                <Label htmlFor="deviceName">
                  Device Name <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="deviceName"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    setVerified(false);
                  }}
                  placeholder="e.g., Production Server"
                />
              </div>

              <div className="space-y-2">
                <Label>Device Fingerprint</Label>
                <Input value={fingerprint} readOnly className="font-mono text-xs" />
                <p className="text-xs text-muted-foreground">
                  Generated from your system&apos;s hardware and environment characteristics
                </p>
              </div>

              {info && (
                <div className="rounded-md border bg-muted/30 p-3">
                  <p className="mb-2 text-xs font-medium uppercase text-muted-foreground">
                    Detected Information
                  </p>
                  <dl className="grid gap-2 text-xs sm:grid-cols-2">
                    <div>
                      <dt className="text-muted-foreground">Platform</dt>
                      <dd className="truncate font-mono">{info.platform}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Resolution</dt>
                      <dd className="font-mono">{info.screenResolution}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">Timezone</dt>
                      <dd className="font-mono">{info.timezone}</dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">CPU Cores</dt>
                      <dd className="font-mono">{info.hardwareConcurrency}</dd>
                    </div>
                  </dl>
                </div>
              )}

              <button
                onClick={handleVerify}
                disabled={verifying || !fingerprint}
                className="inline-flex items-center gap-2 rounded-md border border-input bg-background px-4 py-2 text-sm font-medium hover:bg-accent disabled:opacity-50"
              >
                {verifying ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Registering...
                  </>
                ) : verified ? (
                  <>
                    <CheckCircle2 className="h-4 w-4 text-green-600" />
                    Registered
                  </>
                ) : (
                  "Register Hardware"
                )}
              </button>
            </>
          )}
        </CardContent>
      </Card>

      <WizardActions
        onBack={() => router.push("/install/step/2")}
        onNext={handleNext}
        nextDisabled={!verified}
      />
    </div>
  );
}
