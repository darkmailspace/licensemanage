"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Shield } from "lucide-react";
import { useInstallerStore } from "@/stores/installer-store";
import { installerApi } from "@/lib/api";

export default function InstallerEntryPage() {
  const router = useRouter();
  const { isInstalled, currentStep } = useInstallerStore();
  const [checking, setChecking] = useState(true);
  const [alreadyInstalled, setAlreadyInstalled] = useState(false);

  useEffect(() => {
    let mounted = true;

    const check = async () => {
      try {
        const res = await installerApi.status();
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const installed = (res?.data as any)?.data?.isInstalled === true;
        if (!mounted) return;
        if (installed || isInstalled) {
          setAlreadyInstalled(true);
        } else {
          router.replace(`/install/step/${currentStep}`);
        }
      } catch {
        if (mounted) router.replace(`/install/step/${currentStep}`);
      } finally {
        if (mounted) setChecking(false);
      }
    };

    check();
    return () => {
      mounted = false;
    };
  }, [router, isInstalled, currentStep]);

  if (checking) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-slate-200">
        <div className="flex flex-col items-center gap-3 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
          <p className="text-sm">Checking installation status...</p>
        </div>
      </div>
    );
  }

  if (alreadyInstalled) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-50 to-slate-200 p-6">
        <div className="max-w-md rounded-lg border bg-card p-8 text-center shadow-lg">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
            <Shield className="h-6 w-6 text-primary" />
          </div>
          <h1 className="text-2xl font-bold">System Already Installed</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            This License Manager system has already been installed and configured.
            For security reasons, the installer is locked.
          </p>
          <p className="mt-4 text-xs text-muted-foreground">
            To reinstall, please remove the lock file and clear the database.
          </p>
        </div>
      </div>
    );
  }

  return null;
}
