"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { AlertTriangle, ArrowLeft, Loader2, XCircle } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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

const REVOKE_REASONS = [
  "License piracy detected",
  "Terms of service violation",
  "Fraud detected",
  "Multiple chargebacks",
  "Customer terminated",
  "Court order",
  "Other",
];

export default function RevokeLicensePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [details, setDetails] = useState("");
  const [confirmText, setConfirmText] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (!reason) {
      toast.error("Please select a reason");
      return;
    }
    if (confirmText !== "REVOKE") {
      toast.error('Please type "REVOKE" to confirm');
      return;
    }

    setLoading(true);
    try {
      const fullReason = details ? `${reason}: ${details}` : reason;
      await new Promise((res) => setTimeout(res, 800));
      toast.success("License revoked");
      console.log("Revoking with reason:", fullReason);
      router.push(`/licenses/${id}`);
    } catch {
      toast.error("Failed to revoke license");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader
        title="Revoke License"
        description="Permanently disable this license"
        actions={
          <Link href={`/licenses/${id}`}>
            <Button variant="outline">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Button>
          </Link>
        }
      />

      <Card className="mb-6 border-destructive/30 bg-destructive/5">
        <CardContent className="flex gap-3 p-4">
          <AlertTriangle className="h-5 w-5 shrink-0 text-destructive" />
          <div className="space-y-1 text-sm">
            <p className="font-medium text-destructive">
              ⚠️ This action is permanent and cannot be undone
            </p>
            <p className="text-destructive/90">
              Revoking this license will:
            </p>
            <ul className="ml-4 list-disc space-y-1 text-destructive/90">
              <li>Permanently disable all functionality</li>
              <li>Invalidate all activations and devices</li>
              <li>Block any future validation attempts</li>
              <li>Create a permanent audit log entry</li>
            </ul>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Revocation Reason</CardTitle>
          <CardDescription>
            This reason will be permanently logged in audit trails
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
                {REVOKE_REASONS.map((r) => (
                  <SelectItem key={r} value={r}>
                    {r}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="details">Additional Details</Label>
            <Textarea
              id="details"
              rows={4}
              value={details}
              onChange={(e) => setDetails(e.target.value)}
              placeholder="Provide detailed reasoning for this revocation..."
            />
          </div>

          <div className="rounded-md border-2 border-destructive/30 bg-destructive/5 p-4">
            <Label htmlFor="confirm" className="text-destructive">
              Type <strong>REVOKE</strong> to confirm{" "}
              <span className="text-destructive">*</span>
            </Label>
            <Input
              id="confirm"
              type="text"
              className="mt-2"
              value={confirmText}
              onChange={(e) => setConfirmText(e.target.value)}
              placeholder="Type REVOKE here"
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
          variant="destructive"
          onClick={handleSubmit}
          disabled={loading || !reason || confirmText !== "REVOKE"}
        >
          {loading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Revoking...
            </>
          ) : (
            <>
              <XCircle className="mr-2 h-4 w-4" />
              Permanently Revoke
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
