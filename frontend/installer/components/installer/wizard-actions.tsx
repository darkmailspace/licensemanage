"use client";

import { ArrowLeft, ArrowRight, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";

interface WizardActionsProps {
  onBack?: () => void;
  onNext: () => void;
  backDisabled?: boolean;
  nextDisabled?: boolean;
  loading?: boolean;
  nextLabel?: string;
  backLabel?: string;
  hideBack?: boolean;
}

export function WizardActions({
  onBack,
  onNext,
  backDisabled,
  nextDisabled,
  loading,
  nextLabel = "Next",
  backLabel = "Back",
  hideBack,
}: WizardActionsProps) {
  return (
    <div className="flex justify-between gap-3 border-t pt-6">
      {!hideBack ? (
        <Button variant="outline" onClick={onBack} disabled={backDisabled || loading}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          {backLabel}
        </Button>
      ) : (
        <div />
      )}
      <Button onClick={onNext} disabled={nextDisabled || loading}>
        {loading ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Processing...
          </>
        ) : (
          <>
            {nextLabel}
            <ArrowRight className="ml-2 h-4 w-4" />
          </>
        )}
      </Button>
    </div>
  );
}
