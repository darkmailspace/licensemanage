"use client";

import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

export interface Step {
  id: number;
  title: string;
  description: string;
}

interface StepperProps {
  steps: Step[];
  currentStep: number;
  completedSteps: number[];
}

export function Stepper({ steps, currentStep, completedSteps }: StepperProps) {
  return (
    <div className="hidden lg:block">
      <ol className="space-y-4">
        {steps.map((step) => {
          const isCompleted = completedSteps.includes(step.id);
          const isCurrent = step.id === currentStep;
          const isUpcoming = !isCompleted && !isCurrent;

          return (
            <li key={step.id} className="relative">
              <div className="flex items-start gap-4">
                <div
                  className={cn(
                    "flex h-10 w-10 shrink-0 items-center justify-center rounded-full border-2 transition-colors",
                    isCompleted && "border-primary bg-primary text-primary-foreground",
                    isCurrent && "border-primary bg-primary/10 text-primary",
                    isUpcoming && "border-muted bg-background text-muted-foreground"
                  )}
                >
                  {isCompleted ? (
                    <Check className="h-5 w-5" />
                  ) : (
                    <span className="text-sm font-semibold">{step.id}</span>
                  )}
                </div>
                <div className="min-w-0 flex-1 pb-4">
                  <p
                    className={cn(
                      "text-sm font-semibold",
                      isCurrent && "text-foreground",
                      isCompleted && "text-foreground",
                      isUpcoming && "text-muted-foreground"
                    )}
                  >
                    {step.title}
                  </p>
                  <p className="mt-0.5 text-xs text-muted-foreground">{step.description}</p>
                </div>
              </div>
              {step.id < steps.length && (
                <div
                  className={cn(
                    "absolute left-5 top-10 h-full w-0.5 -translate-x-1/2",
                    isCompleted ? "bg-primary" : "bg-border"
                  )}
                  aria-hidden="true"
                />
              )}
            </li>
          );
        })}
      </ol>
    </div>
  );
}

export function MobileStepper({ steps, currentStep, completedSteps }: StepperProps) {
  const current = steps.find((s) => s.id === currentStep);
  return (
    <div className="lg:hidden">
      <div className="flex items-center justify-between mb-2">
        <p className="text-xs font-semibold text-muted-foreground">
          Step {currentStep} of {steps.length}
        </p>
        <p className="text-xs font-medium">
          {Math.round((completedSteps.length / steps.length) * 100)}% complete
        </p>
      </div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
        <div
          className="h-full bg-primary transition-all"
          style={{ width: `${(completedSteps.length / steps.length) * 100}%` }}
        />
      </div>
      <div className="mt-3">
        <p className="text-base font-semibold">{current?.title}</p>
        <p className="text-xs text-muted-foreground">{current?.description}</p>
      </div>
    </div>
  );
}
