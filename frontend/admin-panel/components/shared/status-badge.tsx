import { Badge } from "@/components/ui/badge";
import { LICENSE_STATUSES, VALIDATION_RESULTS, TICKET_STATUSES } from "@/lib/constants";
import { cn } from "@/lib/utils";

interface StatusBadgeProps {
  status: number;
  type?: "license" | "validation" | "ticket";
  className?: string;
}

const colorMap: Record<string, string> = {
  green: "bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-400",
  red: "bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-400",
  yellow: "bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-400",
  orange: "bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400",
  blue: "bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400",
  gray: "bg-gray-100 text-gray-800 border-gray-200 dark:bg-gray-900/30 dark:text-gray-400",
  purple: "bg-purple-100 text-purple-800 border-purple-200 dark:bg-purple-900/30 dark:text-purple-400",
};

export function StatusBadge({ status, type = "license", className }: StatusBadgeProps) {
  const lookup =
    type === "validation" ? VALIDATION_RESULTS : type === "ticket" ? TICKET_STATUSES : LICENSE_STATUSES;
  const item = lookup.find((s) => s.value === status);

  if (!item) {
    return (
      <Badge variant="outline" className={className}>
        Unknown
      </Badge>
    );
  }

  return (
    <Badge variant="outline" className={cn(colorMap[item.color], "border", className)}>
      {item.label}
    </Badge>
  );
}
