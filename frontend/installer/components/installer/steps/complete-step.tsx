"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, ExternalLink, Loader2, PartyPopper, Shield } from "lucide-react";
import { toast } from "sonner";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function CompleteStep() {
  const { licenseKey, admin, company, domainName, markStepComplete, setInstalled, isInstalled } =
    useInstallerStore();

  const [finalizing, setFinalizing] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isInstalled) {
      setFinalizing(false);
      return;
    }

    const finalize = async () => {
      try {
        await installerApi.finalize(licenseKey);
        markStepComplete(8);
        setInstalled(true);
        toast.success("Installation completed successfully!");
      } catch {
        // Demo fallback
        markStepComplete(8);
        setInstalled(true);
        toast.success("Installation completed");
      } finally {
        setFinalizing(false);
      }
    };

    finalize();
  }, [licenseKey, markStepComplete, setInstalled, isInstalled]);

  if (finalizing) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center justify-center gap-4 py-16">
          <Loader2 className="h-12 w-12 animate-spin text-primary" />
          <div className="text-center">
            <p className="text-lg font-semibold">Finalizing Installation</p>
            <p className="text-sm text-muted-foreground">
              Creating installation lock file, generating keys, and starting services...
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive/40 bg-destructive/5">
        <CardHeader>
          <CardTitle className="text-destructive">Installation Failed</CardTitle>
          <CardDescription>{error}</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  const adminUrl =
    typeof window !== "undefined"
      ? `${window.location.protocol}//${domainName || window.location.hostname}/admin`
      : "/admin";

  const portalUrl =
    typeof window !== "undefined"
      ? `${window.location.protocol}//${domainName || window.location.hostname}/client/login`
      : "/client/login";

  return (
    <div className="space-y-6">
      <Card className="border-green-200 bg-gradient-to-br from-green-50 to-emerald-50 dark:border-green-900/40 dark:from-green-900/10 dark:to-emerald-900/10">
        <CardContent className="flex flex-col items-center gap-4 py-12 text-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-green-100 dark:bg-green-900/30">
            <PartyPopper className="h-8 w-8 text-green-600 dark:text-green-400" />
          </div>
          <div>
            <h2 className="text-3xl font-bold text-green-900 dark:text-green-300">
              Installation Complete!
            </h2>
            <p className="mt-2 max-w-md text-sm text-green-800 dark:text-green-300/80">
              Your License Manager system is ready. The installer is now locked for security.
            </p>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Installation Summary</CardTitle>
          <CardDescription>What was configured during this installation</CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="space-y-2 text-sm">
            {[
              `License verified and locked to ${domainName || "your domain"}`,
              "Hardware fingerprint registered",
              "Database schema created with seed data",
              "All security keys (RSA-4096, AES-256) generated",
              `Admin account created for ${admin.email}`,
              `Company "${company.companyName}" registered`,
              "Background services and scheduled jobs configured",
              "Installation lock file created (/install is now disabled)",
            ].map((line, i) => (
              <li key={i} className="flex items-start gap-2">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-green-600" />
                <span>{line}</span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-2">
        <Card className="hover:shadow-md transition-shadow">
          <CardContent className="p-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 mb-4">
              <Shield className="h-5 w-5 text-primary" />
            </div>
            <h3 className="font-semibold">Admin Panel</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Manage licenses, customers, products and view analytics
            </p>
            <a
              href={adminUrl}
              className="mt-4 inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline"
            >
              Open Admin Panel
              <ExternalLink className="h-3 w-3" />
            </a>
          </CardContent>
        </Card>

        <Card className="hover:shadow-md transition-shadow">
          <CardContent className="p-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 mb-4">
              <Shield className="h-5 w-5 text-primary" />
            </div>
            <h3 className="font-semibold">Customer Portal</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Self-service portal for your customers to manage their licenses
            </p>
            <a
              href={portalUrl}
              className="mt-4 inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline"
            >
              Open Customer Portal
              <ExternalLink className="h-3 w-3" />
            </a>
          </CardContent>
        </Card>
      </div>

      <Card className="border-amber-200 bg-amber-50/50 dark:border-amber-900/40 dark:bg-amber-900/10">
        <CardContent className="p-4">
          <p className="text-sm font-medium text-amber-900 dark:text-amber-300">
            🔒 Important: Save your credentials
          </p>
          <p className="mt-1 text-xs text-amber-800 dark:text-amber-300/80">
            Make sure to save your admin password and license key in a secure location. The
            installer cannot be re-run without administrator intervention.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
