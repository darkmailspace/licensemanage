"use client";

import { useState } from "react";
import { Download, Filter, ScrollText } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { formatDateTime } from "@/lib/utils";
import type { AuditLog } from "@/types";

const ACTION_LABELS: Record<number, { label: string; color: string }> = {
  1: { label: "Create", color: "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400" },
  2: { label: "Read", color: "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400" },
  3: { label: "Update", color: "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400" },
  4: { label: "Delete", color: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" },
  5: { label: "Login", color: "bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400" },
  6: { label: "Logout", color: "bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400" },
  7: { label: "Failed", color: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" },
  8: { label: "Export", color: "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400" },
  9: { label: "Import", color: "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400" },
};

const SAMPLE_LOGS: AuditLog[] = Array.from({ length: 12 }).map((_, i) => ({
  id: `log-${i + 1}`,
  entityName: ["License", "Customer", "Product", "User", "AdminUser"][i % 5],
  entityId: `entity-${i + 1}`,
  action: ((i % 9) + 1) as AuditLog["action"],
  userId: "user-1",
  userName: "Admin User",
  userEmail: "admin@licensemanager.com",
  ipAddress: `192.168.1.${100 + i}`,
  userAgent: "Mozilla/5.0",
  description: [
    "License created for ABC Tech",
    "Customer updated email address",
    "Product price modified",
    "User logged in successfully",
    "License revoked due to violation",
    "Failed login attempt",
    "Customer data exported",
  ][i % 7],
  createdAt: new Date(Date.now() - i * 30 * 60 * 1000).toISOString(),
}));

export default function AuditLogsPage() {
  const [page, setPage] = useState(1);
  const [filterAction, setFilterAction] = useState("all");
  const [filterEntity, setFilterEntity] = useState("all");

  const columns: Column<AuditLog>[] = [
    {
      key: "action",
      header: "Action",
      render: (log) => {
        const action = ACTION_LABELS[log.action];
        return (
          <Badge variant="outline" className={action?.color}>
            {action?.label || "Unknown"}
          </Badge>
        );
      },
    },
    {
      key: "entity",
      header: "Entity",
      render: (log) => (
        <div>
          <p className="font-medium text-sm">{log.entityName}</p>
          <code className="text-xs text-muted-foreground">{log.entityId}</code>
        </div>
      ),
    },
    {
      key: "user",
      header: "User",
      render: (log) => (
        <div>
          <p className="text-sm">{log.userName || "System"}</p>
          <p className="text-xs text-muted-foreground">{log.userEmail}</p>
        </div>
      ),
    },
    {
      key: "description",
      header: "Description",
      render: (log) => (
        <p className="text-sm text-muted-foreground">{log.description || "—"}</p>
      ),
    },
    {
      key: "ip",
      header: "IP Address",
      render: (log) => <code className="text-xs">{log.ipAddress}</code>,
    },
    {
      key: "time",
      header: "Time",
      render: (log) => (
        <span className="text-sm text-muted-foreground">{formatDateTime(log.createdAt)}</span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Audit Logs"
        description="Complete system audit trail of all activities"
        actions={
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export Logs
          </Button>
        }
      />

      <Card className="mb-6">
        <CardContent className="flex flex-wrap items-center gap-4 p-4">
          <Filter className="h-5 w-5 text-muted-foreground" />
          <span className="text-sm font-medium">Filters:</span>

          <Select value={filterAction} onValueChange={setFilterAction}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Action" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Actions</SelectItem>
              {Object.entries(ACTION_LABELS).map(([k, v]) => (
                <SelectItem key={k} value={k}>
                  {v.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={filterEntity} onValueChange={setFilterEntity}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Entity" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Entities</SelectItem>
              <SelectItem value="License">License</SelectItem>
              <SelectItem value="Customer">Customer</SelectItem>
              <SelectItem value="Product">Product</SelectItem>
              <SelectItem value="AdminUser">Admin User</SelectItem>
            </SelectContent>
          </Select>

          <Select defaultValue="24h">
            <SelectTrigger className="w-[180px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="1h">Last hour</SelectItem>
              <SelectItem value="24h">Last 24 hours</SelectItem>
              <SelectItem value="7d">Last 7 days</SelectItem>
              <SelectItem value="30d">Last 30 days</SelectItem>
              <SelectItem value="all">All time</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <DataTable
        columns={columns}
        data={SAMPLE_LOGS}
        searchable
        searchPlaceholder="Search audit logs..."
        pagination={{ page, pageSize: 12, total: 5847, onPageChange: setPage }}
      />
    </div>
  );
}
