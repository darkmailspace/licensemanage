"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { AlertTriangle, ArrowLeft, Loader2, Slash } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const SUSPEND_REASONS = [
  "Payment overdue",
  "Customer request",
  "Suspected misuse",
  "Investigation pending",
  "Trial expired",
  "Compliance violation",
  "Other",
];

export default function SuspendLicensePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [details, setDetails] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (!reason) {
      toast.error("Please select a reason");
      return;
    }
    setLoading(true);
    try {
      const fullReason = details ? `${reason}: ${details}` : reason;
      await new Promise((res) => setTimeout(res, 800));
      toast.success("License suspended");
      console.log("Suspending with reason:", fullReason);
      router.push(`/licenses/${id}`);
    } catch {
      toast.error("Failed to suspend license");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader
        title="Suspend License"
        description="Temporarily disable this license"
        actions={
          <Link href={`/licenses/${id}`}>
            <Button variant="outline">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Button>
          </Link>
        }
      />

      <Card className="mb-6 border-orange-200 bg-orange-50 dark:border-orange-900/30 dark:bg-orange-900/10">
        <CardContent className="flex gap-3 p-4">
          <AlertTriangle className="h-5 w-5 shrink-0 text-orange-600 dark:text-orange-400" />
          <div className="space-y-1 text-sm">
            <p className="font-medium text-orange-900 dark:text-orange-300">
              Suspending will immediately disable this license
            </p>
            <p className="text-orange-800 dark:text-orange-300/80">
              The license can be reactivated at any time. The customer&apos;s services
              that depend on this license will stop working until the license is unsuspended.
            </p>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Suspension Reason</CardTitle>
          <CardDescription>
            Provide a reason for suspending this license. This will be logged for audit purposes.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label>
              Reason <span className="text-destructive">*</span>
            </Label>
            <Select value={reason} onValueChange={setReason}>
              <SelectTrigger>
                <SelectValue placeholder="Select a reason" />
              </SelectTrigger>
              <SelectContent>
                {SUSPEND_REASONS.map((r) => (
                  <SelectItem key={r} value={r}>
                    {r}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="details">Additional Details (optional)</Label>
            <Textarea
              id="details"
              rows={4}
              value={details}
              onChange={(e) => setDetails(e.target.value)}
              placeholder="Provide more context about the suspension..."
            />
          </div>
        </CardContent>
      </Card>

      <div className="mt-6 flex justify-end gap-3">
        <Link href={`/licenses/${id}`}>
          <Button variant="outline" disabled={loading}>
            Cancel
          </Button>
        </Link>
        <Button
          onClick={handleSubmit}
          disabled={loading || !reason}
          className="bg-orange-600 hover:bg-orange-700"
        >
          {loading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Suspending...
            </>
          ) : (
            <>
              <Slash className="mr-2 h-4 w-4" />
              Suspend License
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
