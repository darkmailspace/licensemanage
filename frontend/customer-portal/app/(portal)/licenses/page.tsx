"use client";

import Link from "next/link";
import { Calendar, Cpu, Eye, Globe, KeyRound, RefreshCcw, TrendingUp } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { LicenseStatusBadge } from "@/components/shared/license-status-badge";
import { formatDate } from "@/lib/utils";

const SAMPLE_LICENSES = [
  {
    id: "lic-1",
    productName: "Finance ERP System",
    productVersion: "1.0.0",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-I9J0K1L2-M3N4O5P6",
    status: 2,
    licenseTypeLabel: "Yearly",
    startDate: new Date(Date.now() - 340 * 86400000).toISOString(),
    expiryDate: new Date(Date.now() + 25 * 86400000).toISOString(),
    daysUntilExpiry: 25,
    activeDomains: 2,
    maxDomains: 3,
    activeDevices: 3,
    maxDevices: 5,
  },
];

export default function LicensesPage() {
  return (
    <div>
      <PageHeader title="My Licenses" description="View and manage all your licenses" />

      <div className="grid gap-4">
        {SAMPLE_LICENSES.map((license) => {
          const daysWarning = license.daysUntilExpiry < 30;
          return (
            <Card key={license.id} className="overflow-hidden">
              <CardContent className="p-6">
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
                      <KeyRound className="h-6 w-6 text-primary" />
                    </div>
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <h3 className="text-lg font-semibold">{license.productName}</h3>
                        <span className="text-xs text-muted-foreground">v{license.productVersion}</span>
                      </div>
                      <code className="block rounded bg-muted px-2 py-0.5 text-xs">
                        {license.licenseKey}
                      </code>
                      <div className="flex flex-wrap items-center gap-2 pt-1">
                        <LicenseStatusBadge status={license.status} />
                        <span className="rounded-md border px-2 py-0.5 text-xs">{license.licenseTypeLabel}</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-col items-end gap-2 text-sm">
                    <div className="flex items-center gap-1.5">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <span className="text-muted-foreground">Expires:</span>
                      <span className={daysWarning ? "font-medium text-orange-600" : "font-medium"}>
                        {formatDate(license.expiryDate)}
                      </span>
                    </div>
                    {daysWarning && (
                      <span className="text-xs text-orange-600">
                        {license.daysUntilExpiry} days remaining
                      </span>
                    )}
                  </div>
                </div>

                <div className="mt-4 grid gap-3 border-t pt-4 sm:grid-cols-3">
                  <div className="flex items-center gap-2 text-sm">
                    <Globe className="h-4 w-4 text-muted-foreground" />
                    <span>
                      <strong>{license.activeDomains}</strong>
                      <span className="text-muted-foreground"> / {license.maxDomains} domains</span>
                    </span>
                  </div>
                  <div className="flex items-center gap-2 text-sm">
                    <Cpu className="h-4 w-4 text-muted-foreground" />
                    <span>
                      <strong>{license.activeDevices}</strong>
                      <span className="text-muted-foreground"> / {license.maxDevices} devices</span>
                    </span>
                  </div>
                  <div className="flex items-center gap-2 text-sm">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    <span>
                      <span className="text-muted-foreground">Started </span>
                      <strong>{formatDate(license.startDate)}</strong>
                    </span>
                  </div>
                </div>

                <div className="mt-4 flex flex-wrap gap-2 border-t pt-4">
                  <Link href={`/licenses/${license.id}`}>
                    <Button variant="outline" size="sm">
                      <Eye className="mr-2 h-4 w-4" />
                      View Details
                    </Button>
                  </Link>
                  <Link href="/renew">
                    <Button size="sm">
                      <RefreshCcw className="mr-2 h-4 w-4" />
                      Renew
                    </Button>
                  </Link>
                  <Link href="/upgrade">
                    <Button variant="outline" size="sm">
                      <TrendingUp className="mr-2 h-4 w-4" />
                      Upgrade
                    </Button>
                  </Link>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
