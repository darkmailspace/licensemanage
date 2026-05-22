"use client";

import { useEffect } from "react";
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  DollarSign,
  KeyRound,
  TrendingUp,
  Users,
  XCircle,
} from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { StatCard } from "@/components/dashboard/stat-card";
import {
  ActivationsChart,
  LicenseTypeChart,
  RevenueChart,
} from "@/components/dashboard/charts";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { StatusBadge } from "@/components/shared/status-badge";
import { formatRelativeTime } from "@/lib/utils";
import { useAuth } from "@/hooks/use-auth";

const RECENT_ACTIVITY = [
  {
    id: "1",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-I9J0K1L2-M3N4O5P6",
    customer: "ABC Technologies",
    status: 2,
    action: "License Activated",
    time: new Date(Date.now() - 2 * 60 * 1000).toISOString(),
  },
  {
    id: "2",
    licenseKey: "LK-B2C3D4E5-F6G7H8I9-J0K1L2M3-N4O5P6Q7",
    customer: "XYZ Corporation",
    status: 2,
    action: "License Validated",
    time: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
  },
  {
    id: "3",
    licenseKey: "LK-C3D4E5F6-G7H8I9J0-K1L2M3N4-O5P6Q7R8",
    customer: "Tech Solutions Ltd",
    status: 6,
    action: "License Renewed",
    time: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
  },
  {
    id: "4",
    licenseKey: "LK-D4E5F6G7-H8I9J0K1-L2M3N4O5-P6Q7R8S9",
    customer: "Global Systems Inc",
    status: 3,
    action: "License Suspended",
    time: new Date(Date.now() - 30 * 60 * 1000).toISOString(),
  },
  {
    id: "5",
    licenseKey: "LK-E5F6G7H8-I9J0K1L2-M3N4O5P6-Q7R8S9T0",
    customer: "InnovateTech Pvt Ltd",
    status: 2,
    action: "License Created",
    time: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
  },
];

export default function DashboardPage() {
  const { user } = useAuth();

  useEffect(() => {
    // initialize/load dashboard data
  }, []);

  return (
    <div>
      <PageHeader
        title={`Welcome back, ${user?.fullName?.split(" ")[0] || "Admin"} 👋`}
        description="Here's what's happening with your license management system today."
      />

      {/* Stats Grid */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total Licenses"
          value="1,284"
          icon={KeyRound}
          trend={{ value: 12.5, direction: "up", label: "from last month" }}
          description="from last month"
        />
        <StatCard
          title="Active Customers"
          value="426"
          icon={Users}
          trend={{ value: 8.2, direction: "up", label: "from last month" }}
          description="from last month"
          iconClassName="bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400"
        />
        <StatCard
          title="Total Revenue"
          value="$348,290"
          icon={DollarSign}
          trend={{ value: 15.3, direction: "up", label: "from last month" }}
          description="from last month"
          iconClassName="bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400"
        />
        <StatCard
          title="Expiring Soon"
          value="42"
          icon={AlertTriangle}
          trend={{ value: 3.1, direction: "down", label: "from last week" }}
          description="next 30 days"
          iconClassName="bg-orange-100 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400"
        />
      </div>

      {/* Secondary Stats */}
      <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Active Licenses"
          value="1,156"
          icon={CheckCircle2}
          description="90.0% of total"
          iconClassName="bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400"
        />
        <StatCard
          title="Expired"
          value="86"
          icon={XCircle}
          description="6.7% of total"
          iconClassName="bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400"
        />
        <StatCard
          title="Activations (24h)"
          value="247"
          icon={Activity}
          trend={{ value: 24.0, direction: "up" }}
          description="vs yesterday"
          iconClassName="bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400"
        />
        <StatCard
          title="Success Rate"
          value="98.5%"
          icon={TrendingUp}
          description="last 7 days"
          iconClassName="bg-cyan-100 text-cyan-600 dark:bg-cyan-900/30 dark:text-cyan-400"
        />
      </div>

      {/* Charts */}
      <div className="mt-6 grid gap-4 lg:grid-cols-7">
        <RevenueChart />
        <ActivationsChart />
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-7">
        <LicenseTypeChart />

        <Card className="col-span-full lg:col-span-4">
          <CardHeader>
            <CardTitle>Recent Activity</CardTitle>
            <CardDescription>Latest license events from your system</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {RECENT_ACTIVITY.map((activity) => (
                <div
                  key={activity.id}
                  className="flex items-center justify-between border-b pb-4 last:border-0 last:pb-0"
                >
                  <div className="space-y-1">
                    <p className="text-sm font-medium">{activity.action}</p>
                    <p className="text-xs text-muted-foreground">
                      {activity.customer} · {activity.licenseKey.substring(0, 25)}...
                    </p>
                  </div>
                  <div className="flex flex-col items-end gap-1">
                    <StatusBadge status={activity.status} />
                    <p className="text-xs text-muted-foreground">
                      {formatRelativeTime(activity.time)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
