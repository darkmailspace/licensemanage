"use client";

import Link from "next/link";
import {
  AlertTriangle,
  CheckCircle2,
  Cpu,
  Download,
  FileText,
  Globe,
  HelpCircle,
  KeyRound,
  RefreshCcw,
} from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { LicenseStatusBadge } from "@/components/shared/license-status-badge";
import { useAuthStore } from "@/stores/auth-store";
import { formatDate } from "@/lib/utils";

const SAMPLE_LICENSES = [
  {
    id: "lic-1",
    productName: "Finance ERP System",
    productVersion: "1.0.0",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-I9J0K1L2-M3N4O5P6",
    status: 2,
    expiryDate: new Date(Date.now() + 25 * 86400000).toISOString(),
    daysUntilExpiry: 25,
  },
];

export default function DashboardPage() {
  const { user } = useAuthStore();

  return (
    <div>
      <PageHeader
        title={`Hi, ${user?.name?.split(" ")[0] || "there"} 👋`}
        description="Welcome to your customer portal. Manage all your licenses in one place."
      />

      {/* Quick stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-3 p-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300">
              <KeyRound className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Active Licenses</p>
              <p className="text-2xl font-bold">1</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300">
              <Globe className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Domains</p>
              <p className="text-2xl font-bold">2</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300">
              <Cpu className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Devices</p>
              <p className="text-2xl font-bold">3</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-6">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300">
              <HelpCircle className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Open Tickets</p>
              <p className="text-2xl font-bold">2</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Renewal alert */}
      <Card className="mt-6 border-orange-200 bg-orange-50 dark:border-orange-900/40 dark:bg-orange-900/10">
        <CardContent className="flex items-center justify-between gap-4 p-4">
          <div className="flex items-center gap-3">
            <AlertTriangle className="h-5 w-5 shrink-0 text-orange-600 dark:text-orange-400" />
            <div>
              <p className="font-medium text-orange-900 dark:text-orange-300">
                Your license expires in 25 days
              </p>
              <p className="text-xs text-orange-800 dark:text-orange-300/80">
                Renew now to avoid service interruption
              </p>
            </div>
          </div>
          <Link href="/renew">
            <Button size="sm" className="bg-orange-600 hover:bg-orange-700">
              <RefreshCcw className="mr-2 h-4 w-4" />
              Renew Now
            </Button>
          </Link>
        </CardContent>
      </Card>

      {/* Quick actions */}
      <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[
          { title: "View Licenses", icon: KeyRound, href: "/licenses" },
          { title: "Download Updates", icon: Download, href: "/updates" },
          { title: "Get Support", icon: HelpCircle, href: "/tickets" },
          { title: "View Invoices", icon: FileText, href: "/invoices" },
        ].map((action) => (
          <Link key={action.title} href={action.href}>
            <Card className="cursor-pointer transition-shadow hover:shadow-md">
              <CardContent className="flex items-center gap-3 p-6">
                <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10 text-primary">
                  <action.icon className="h-5 w-5" />
                </div>
                <p className="font-medium">{action.title}</p>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {/* Active license card */}
      <div className="mt-6">
        <h2 className="mb-3 text-lg font-semibold">Your Active Licenses</h2>
        <div className="space-y-3">
          {SAMPLE_LICENSES.map((license) => (
            <Card key={license.id}>
              <CardHeader>
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <CardTitle className="text-lg">{license.productName}</CardTitle>
                    <CardDescription>v{license.productVersion}</CardDescription>
                    <code className="mt-1 inline-block rounded bg-muted px-2 py-0.5 text-xs">
                      {license.licenseKey}
                    </code>
                  </div>
                  <div className="flex flex-col items-end gap-2">
                    <LicenseStatusBadge status={license.status} />
                    <p className="text-xs text-muted-foreground">
                      Expires {formatDate(license.expiryDate)}
                    </p>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  <Link href={`/licenses/${license.id}`}>
                    <Button variant="outline" size="sm">
                      View Details
                    </Button>
                  </Link>
                  <Link href="/renew">
                    <Button size="sm">
                      <RefreshCcw className="mr-2 h-4 w-4" />
                      Renew
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}
