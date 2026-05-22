"use client";

import { useState } from "react";
import { Activity, CheckCircle2, Download, Heart, XCircle } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { StatusBadge } from "@/components/shared/status-badge";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { formatDateTime, truncate } from "@/lib/utils";
import type { LicenseValidation } from "@/types";

const SAMPLE_VALIDATIONS: (LicenseValidation & { customerName: string; licenseKey: string })[] =
  Array.from({ length: 10 }).map((_, i) => ({
    id: `val-${i + 1}`,
    licenseId: `lic-${i + 1}`,
    validationResult: ((i % 7) + 1) as LicenseValidation["validationResult"],
    isValid: i % 7 === 0,
    validationMessage: i % 7 === 0 ? "License valid" : "Validation failed",
    domainName: `app${i + 1}.example.com`,
    deviceFingerprint: `FP-${Math.random().toString(36).substring(2, 18).toUpperCase()}`,
    ipAddress: `192.168.1.${100 + i}`,
    country: ["USA", "UK", "India", "Germany", "Singapore"][i % 5],
    productVersion: "1.0.0",
    isHeartbeat: i % 3 === 0,
    responseTimeMs: Math.floor(Math.random() * 200) + 50,
    customerName: ["ABC Tech", "XYZ Corp", "Innovate Ltd"][i % 3],
    licenseKey: `LK-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
    createdAt: new Date(Date.now() - i * 5 * 60 * 1000).toISOString(),
  }));

export default function ValidationsPage() {
  const [page, setPage] = useState(1);

  const columns: Column<(typeof SAMPLE_VALIDATIONS)[0]>[] = [
    {
      key: "result",
      header: "Result",
      render: (v) => <StatusBadge status={v.validationResult} type="validation" />,
    },
    {
      key: "license",
      header: "License",
      render: (v) => (
        <div>
          <p className="text-sm">{v.customerName}</p>
          <code className="text-xs text-muted-foreground">{truncate(v.licenseKey, 25)}</code>
        </div>
      ),
    },
    {
      key: "domain",
      header: "Domain",
      render: (v) => v.domainName || "—",
    },
    {
      key: "ip",
      header: "IP / Country",
      render: (v) => (
        <div>
          <code className="text-xs">{v.ipAddress}</code>
          <p className="text-xs text-muted-foreground">{v.country}</p>
        </div>
      ),
    },
    {
      key: "type",
      header: "Type",
      render: (v) =>
        v.isHeartbeat ? (
          <Badge variant="outline" className="gap-1">
            <Heart className="h-3 w-3 fill-current" />
            Heartbeat
          </Badge>
        ) : (
          <Badge variant="outline">Manual</Badge>
        ),
    },
    {
      key: "version",
      header: "Version",
      render: (v) => <code className="text-xs">v{v.productVersion}</code>,
    },
    {
      key: "responseTime",
      header: "Response",
      render: (v) => (
        <span
          className={`text-xs ${
            v.responseTimeMs > 200
              ? "text-orange-600"
              : v.responseTimeMs > 100
                ? "text-yellow-600"
                : "text-green-600"
          }`}
        >
          {v.responseTimeMs}ms
        </span>
      ),
    },
    {
      key: "time",
      header: "Time",
      render: (v) => (
        <span className="text-sm text-muted-foreground">{formatDateTime(v.createdAt)}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Validation Logs"
        description="Real-time license validation tracking and heartbeat monitoring"
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
              <p className="text-2xl font-bold">12,847</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-green-100 dark:bg-green-900/30">
              <CheckCircle2 className="h-5 w-5 text-green-600 dark:text-green-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Valid</p>
              <p className="text-2xl font-bold">12,659</p>
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
              <p className="text-2xl font-bold">188</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-blue-100 dark:bg-blue-900/30">
              <Heart className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Avg Response</p>
              <p className="text-2xl font-bold">82ms</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <DataTable
        columns={columns}
        data={SAMPLE_VALIDATIONS}
        searchable
        searchPlaceholder="Search validations..."
        pagination={{ page, pageSize: 10, total: 28492, onPageChange: setPage }}
      />
    </div>
  );
}
