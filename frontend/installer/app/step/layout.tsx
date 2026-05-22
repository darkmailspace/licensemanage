"use client";

import { useEffect } from "react";
import { Shield } from "lucide-react";
import { useInstallerStore } from "@/stores/installer-store";
import { Stepper, MobileStepper } from "@/components/installer/stepper";

const STEPS = [
  { id: 1, title: "License Verification", description: "Verify your license key" },
  { id: 2, title: "Domain Verification", description: "Confirm installation domain" },
  { id: 3, title: "Hardware Verification", description: "Register this server" },
  { id: 4, title: "Database Configuration", description: "Connect to PostgreSQL" },
  { id: 5, title: "Admin Account", description: "Create admin user" },
  { id: 6, title: "Company Setup", description: "Organization details" },
  { id: 7, title: "API Configuration", description: "Email, SMS, integrations" },
  { id: 8, title: "Installation Complete", description: "Finalize setup" },
];

export default function StepLayout({ children }: { children: React.ReactNode }) {
  const { currentStep, completedSteps } = useInstallerStore();

  useEffect(() => {
    document.title = `Step ${currentStep} of ${STEPS.length} · License Manager Installer`;
  }, [currentStep]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100">
      <header className="border-b bg-white">
        <div className="container flex h-16 items-center gap-3 px-4">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <Shield className="h-4 w-4" />
          </div>
          <div>
            <h1 className="text-base font-semibold">License Manager Installer</h1>
            <p className="text-xs text-muted-foreground">Production-grade installation wizard</p>
          </div>
        </div>
      </header>

      <main className="container px-4 py-8">
        <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
          <aside>
            <Stepper steps={STEPS} currentStep={currentStep} completedSteps={completedSteps} />
            <MobileStepper steps={STEPS} currentStep={currentStep} completedSteps={completedSteps} />
          </aside>
          <section>{children}</section>
        </div>
      </main>
    </div>
  );
}
