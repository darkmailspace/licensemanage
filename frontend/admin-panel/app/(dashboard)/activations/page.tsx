"use client";

import { useState } from "react";
import { Activity, CheckCircle2, Download, Globe, Wifi, WifiOff, XCircle } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { ACTIVATION_TYPES } from "@/lib/constants";
import { formatDateTime, truncate } from "@/lib/utils";
import type { LicenseActivation } from "@/types";

const SAMPLE_ACTIVATIONS: (LicenseActivation & { customerName: string; licenseKey: string })[] =
  Array.from({ length: 10 }).map((_, i) => ({
    id: `act-${i + 1}`,
    licenseId: `lic-${i + 1}`,
    activationType: ((i % 4) + 1) as 1 | 2 | 3 | 4,
    success: i % 4 !== 3,
    failureReason: i % 4 === 3 ? "Invalid activation token" : undefined,
    domainName: `app${i + 1}.example.com`,
    deviceFingerprint: `FP-${Math.random().toString(36).substring(2, 18).toUpperCase()}`,
    ipAddress: `192.168.1.${100 + i}`,
    country: ["USA", "UK", "India", "Germany", "Singapore"][i % 5],
    userAgent: "Mozilla/5.0",
    createdAt: new Date(Date.now() - i * 60 * 60 * 1000).toISOString(),
    customerName: ["ABC Tech", "XYZ Corp", "Innovate Ltd"][i % 3],
    licenseKey: `LK-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
  }));

export default function ActivationsPage() {
  const [page, setPage] = useState(1);

  const columns: Column<(typeof SAMPLE_ACTIVATIONS)[0]>[] = [
    {
      key: "result",
      header: "Result",
      render: (a) =>
        a.success ? (
          <span className="inline-flex items-center gap-1.5 text-sm font-medium text-green-600 dark:text-green-400">
            <CheckCircle2 className="h-4 w-4" />
            Success
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 text-sm font-medium text-destructive">
            <XCircle className="h-4 w-4" />
            Failed
          </span>
        ),
    },
    {
      key: "type",
      header: "Type",
      render: (a) => {
        const t = ACTIVATION_TYPES.find((at) => at.value === a.activationType);
        return (
          <Badge variant="outline" className="gap-1">
            {a.activationType === 1 ? (
              <Wifi className="h-3 w-3" />
            ) : (
              <WifiOff className="h-3 w-3" />
            )}
            {t?.label || "Unknown"}
          </Badge>
        );
      },
    },
    {
      key: "license",
      header: "License",
      render: (a) => (
        <div>
          <p className="text-sm">{a.customerName}</p>
          <code className="text-xs text-muted-foreground">{truncate(a.licenseKey, 25)}</code>
        </div>
      ),
    },
    {
      key: "domain",
      header: "Domain",
      render: (a) => (
        <span className="inline-flex items-center gap-1.5 text-sm">
          <Globe className="h-3.5 w-3.5 text-muted-foreground" />
          {a.domainName || "—"}
        </span>
      ),
    },
    {
      key: "ip",
      header: "IP / Country",
      render: (a) => (
        <div>
          <code className="text-xs">{a.ipAddress}</code>
          <p className="text-xs text-muted-foreground">{a.country}</p>
        </div>
      ),
    },
    {
      key: "failureReason",
      header: "Reason",
      render: (a) => (
        <span className="text-sm text-muted-foreground">
          {a.failureReason || (a.success ? "—" : "Unknown")}
        </span>
      ),
    },
    {
      key: "createdAt",
      header: "Time",
      render: (a) => (
        <span className="text-sm text-muted-foreground">{formatDateTime(a.createdAt)}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Activation Logs"
        description="View all license activation attempts"
        actions={
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export
          </Button>
        }
      />

      <div className="mb-6 grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
              <Activity className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Total (24h)</p>
              <p className="text-2xl font-bold">247</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-green-100 dark:bg-green-900/30">
              <CheckCircle2 className="h-5 w-5 text-green-600 dark:text-green-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Successful</p>
              <p className="text-2xl font-bold">235</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-red-100 dark:bg-red-900/30">
              <XCircle className="h-5 w-5 text-red-600 dark:text-red-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Failed</p>
              <p className="text-2xl font-bold">12</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-blue-100 dark:bg-blue-900/30">
              <Activity className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Success Rate</p>
              <p className="text-2xl font-bold">95.1%</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <DataTable
        columns={columns}
        data={SAMPLE_ACTIVATIONS}
        searchable
        searchPlaceholder="Search activations..."
        pagination={{ page, pageSize: 10, total: 8472, onPageChange: setPage }}
      />
    </div>
  );
}
