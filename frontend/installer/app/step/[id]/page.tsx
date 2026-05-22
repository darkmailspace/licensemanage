"use client";

import { use, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useInstallerStore } from "@/stores/installer-store";

import LicenseStep from "@/components/installer/steps/license-step";
import DomainStep from "@/components/installer/steps/domain-step";
import HardwareStep from "@/components/installer/steps/hardware-step";
import DatabaseStep from "@/components/installer/steps/database-step";
import AdminStep from "@/components/installer/steps/admin-step";
import CompanyStep from "@/components/installer/steps/company-step";
import ApiStep from "@/components/installer/steps/api-step";
import CompleteStep from "@/components/installer/steps/complete-step";

const STEP_COMPONENTS: Record<number, React.ComponentType> = {
  1: LicenseStep,
  2: DomainStep,
  3: HardwareStep,
  4: DatabaseStep,
  5: AdminStep,
  6: CompanyStep,
  7: ApiStep,
  8: CompleteStep,
};

export default function InstallerStepPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const stepNumber = parseInt(id, 10);
  const router = useRouter();
  const { currentStep, setStep, completedSteps } = useInstallerStore();

  useEffect(() => {
    if (Number.isNaN(stepNumber) || stepNumber < 1 || stepNumber > 8) {
      router.replace("/install/step/1");
      return;
    }

    // Allow going to current, completed, or the next step (current + 1)
    const maxAllowed = Math.max(...completedSteps, 0) + 1;
    if (stepNumber > maxAllowed) {
      router.replace(`/install/step/${maxAllowed}`);
      return;
    }

    if (stepNumber !== currentStep) {
      setStep(stepNumber);
    }
  }, [stepNumber, currentStep, completedSteps, setStep, router]);

  const StepComponent = STEP_COMPONENTS[stepNumber];
  if (!StepComponent) return null;

  return <StepComponent />;
}
