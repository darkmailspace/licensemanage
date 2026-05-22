"use client";

import { useState } from "react";
import { CheckCircle2, Globe, Shield, Trash2, XCircle } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { formatDate, formatRelativeTime, truncate } from "@/lib/utils";
import type { LicenseDomain } from "@/types";

const SAMPLE_DOMAINS: (LicenseDomain & { customerName: string; licenseKey: string })[] =
  Array.from({ length: 10 }).map((_, i) => ({
    id: `dom-${i + 1}`,
    licenseId: `lic-${i + 1}`,
    domainName: i % 3 === 0 ? `*.example${i + 1}.com` : `app${i + 1}.example.com`,
    isWildcard: i % 3 === 0,
    isActive: i % 5 !== 0,
    isPrimary: i % 4 === 0,
    isVerified: i % 6 !== 0,
    verifiedAt:
      i % 6 !== 0 ? new Date(Date.now() - i * 86400000).toISOString() : undefined,
    lastAccessedAt: new Date(Date.now() - i * 60 * 60 * 1000).toISOString(),
    changeRequested: false,
    changeApproved: false,
    customerName: ["ABC Tech", "XYZ Corp", "Innovate Ltd"][i % 3],
    licenseKey: `LK-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
    createdAt: new Date(Date.now() - i * 86400000).toISOString(),
  }));

export default function DomainsPage() {
  const [page, setPage] = useState(1);

  const columns: Column<(typeof SAMPLE_DOMAINS)[0]>[] = [
    {
      key: "domain",
      header: "Domain",
      render: (d) => (
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-md bg-muted">
            <Globe className="h-4 w-4 text-muted-foreground" />
          </div>
          <div>
            <p className="font-medium">{d.domainName}</p>
            <div className="mt-1 flex gap-1">
              {d.isWildcard && (
                <Badge variant="outline" className="text-xs">
                  Wildcard
                </Badge>
              )}
              {d.isPrimary && (
                <Badge variant="info" className="text-xs">
                  Primary
                </Badge>
              )}
            </div>
          </div>
        </div>
      ),
    },
    {
      key: "license",
      header: "License",
      render: (d) => (
        <div>
          <p className="text-sm">{d.customerName}</p>
          <code className="text-xs text-muted-foreground">{truncate(d.licenseKey, 25)}</code>
        </div>
      ),
    },
    {
      key: "verified",
      header: "Verification",
      render: (d) =>
        d.isVerified ? (
          <span className="inline-flex items-center gap-1.5 text-sm text-green-600 dark:text-green-400">
            <CheckCircle2 className="h-4 w-4" />
            Verified {d.verifiedAt && formatDate(d.verifiedAt)}
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 text-sm text-muted-foreground">
            <XCircle className="h-4 w-4" />
            Pending
          </span>
        ),
    },
    {
      key: "status",
      header: "Status",
      render: (d) => (
        <Badge variant={d.isActive ? "success" : "outline"}>
          {d.isActive ? "Active" : "Inactive"}
        </Badge>
      ),
    },
    {
      key: "lastAccessed",
      header: "Last Access",
      render: (d) => (
        <span className="text-sm text-muted-foreground">
          {d.lastAccessedAt ? formatRelativeTime(d.lastAccessedAt) : "—"}
        </span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Domain Management"
        description="View and manage all domains registered to licenses"
      />

      <DataTable
        columns={columns}
        data={SAMPLE_DOMAINS}
        searchable
        searchPlaceholder="Search domains..."
        pagination={{ page, pageSize: 10, total: 426, onPageChange: setPage }}
        rowActions={(d) => (
          <div className="flex gap-1 justify-end">
            {!d.isVerified && (
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <Shield className="h-4 w-4" />
              </Button>
            )}
            <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive">
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        )}
      />
    </div>
  );
}
