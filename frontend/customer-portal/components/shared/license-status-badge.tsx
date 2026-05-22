import { Badge } from "@/components/ui/badge";
import { LicenseStatus } from "@/types";
import { cn } from "@/lib/utils";

const STATUS_MAP: Record<number, { label: string; classes: string }> = {
  [LicenseStatus.PendingActivation]: {
    label: "Pending Activation",
    classes: "bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-300",
  },
  [LicenseStatus.Active]: {
    label: "Active",
    classes: "bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-300",
  },
  [LicenseStatus.Suspended]: {
    label: "Suspended",
    classes: "bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/30 dark:text-orange-300",
  },
  [LicenseStatus.Expired]: {
    label: "Expired",
    classes: "bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300",
  },
  [LicenseStatus.Revoked]: {
    label: "Revoked",
    classes: "bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300",
  },
  [LicenseStatus.GracePeriod]: {
    label: "Grace Period",
    classes: "bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-300",
  },
  [LicenseStatus.PendingRenewal]: {
    label: "Pending Renewal",
    classes: "bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300",
  },
};

export function LicenseStatusBadge({ status }: { status: number }) {
  const item = STATUS_MAP[status] ?? { label: "Unknown", classes: "bg-gray-100 text-gray-800" };
  return (
    <Badge variant="outline" className={cn("border", item.classes)}>
      {item.label}
    </Badge>
  );
}
