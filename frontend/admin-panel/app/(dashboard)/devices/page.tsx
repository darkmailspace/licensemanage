"use client";

import { useState } from "react";
import { Cpu, Download, MapPin, MoreHorizontal, ShieldOff } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { formatRelativeTime, truncate } from "@/lib/utils";
import type { LicenseDevice } from "@/types";

const SAMPLE_DEVICES: (LicenseDevice & { customerName: string; licenseKey: string })[] =
  Array.from({ length: 10 }).map((_, i) => ({
    id: `dev-${i + 1}`,
    licenseId: `lic-${i + 1}`,
    deviceName: ["Production Server", "Backup Server", "Dev Machine", "QA Server", "DR Server"][i % 5],
    deviceFingerprint: `FP-${Math.random().toString(36).substring(2, 18).toUpperCase()}`,
    operatingSystem: ["Ubuntu 22.04", "Windows Server 2022", "CentOS 8", "macOS 14"][i % 4],
    osVersion: "5.15.0-91-generic",
    architecture: "x86_64",
    isVirtualMachine: i % 3 === 0,
    vmPlatform: i % 3 === 0 ? "VMware ESXi" : undefined,
    ipAddress: `192.168.1.${100 + i}`,
    country: ["USA", "UK", "India", "Germany", "Singapore"][i % 5],
    city: ["New York", "London", "Mumbai", "Berlin", "Singapore"][i % 5],
    isActive: i % 4 !== 0,
    isDeactivated: i % 5 === 0,
    accessCount: Math.floor(Math.random() * 1000),
    lastAccessedAt: new Date(Date.now() - i * 60 * 60 * 1000).toISOString(),
    firstActivatedAt: new Date(Date.now() - i * 30 * 86400000).toISOString(),
    customerName: ["ABC Tech", "XYZ Corp", "Innovate Ltd"][i % 3],
    licenseKey: `LK-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
    createdAt: new Date().toISOString(),
  }));

export default function DevicesPage() {
  const [page, setPage] = useState(1);

  const columns: Column<(typeof SAMPLE_DEVICES)[0]>[] = [
    {
      key: "device",
      header: "Device",
      render: (d) => (
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-md bg-muted">
            <Cpu className="h-4 w-4 text-muted-foreground" />
          </div>
          <div>
            <p className="font-medium">{d.deviceName}</p>
            <code className="text-xs text-muted-foreground">
              {truncate(d.deviceFingerprint, 25)}
            </code>
          </div>
        </div>
      ),
    },
    {
      key: "system",
      header: "System",
      render: (d) => (
        <div>
          <p className="text-sm">{d.operatingSystem}</p>
          <div className="flex items-center gap-1">
            <p className="text-xs text-muted-foreground">{d.architecture}</p>
            {d.isVirtualMachine && (
              <Badge variant="outline" className="text-xs">
                VM
              </Badge>
            )}
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
          <code className="text-xs text-muted-foreground">{truncate(d.licenseKey, 20)}</code>
        </div>
      ),
    },
    {
      key: "location",
      header: "Location",
      render: (d) => (
        <div className="flex items-center gap-1.5 text-sm">
          <MapPin className="h-3.5 w-3.5 text-muted-foreground" />
          <span>
            {d.city}, {d.country}
          </span>
        </div>
      ),
    },
    {
      key: "ip",
      header: "IP Address",
      render: (d) => <code className="text-xs">{d.ipAddress}</code>,
    },
    {
      key: "status",
      header: "Status",
      render: (d) => {
        if (d.isDeactivated) return <Badge variant="destructive">Deactivated</Badge>;
        if (d.isActive) return <Badge variant="success">Active</Badge>;
        return <Badge variant="outline">Inactive</Badge>;
      },
    },
    {
      key: "lastAccess",
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
        title="Device Management"
        description="View and manage devices registered to licenses"
        actions={
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export
          </Button>
        }
      />

      <DataTable
        columns={columns}
        data={SAMPLE_DEVICES}
        searchable
        searchPlaceholder="Search devices..."
        pagination={{ page, pageSize: 10, total: 1842, onPageChange: setPage }}
        rowActions={(d) => (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>View Details</DropdownMenuItem>
              {!d.isDeactivated && (
                <DropdownMenuItem className="text-destructive">
                  <ShieldOff className="mr-2 h-4 w-4" />
                  Deactivate
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      />
    </div>
  );
}
