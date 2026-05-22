"use client";

import { useState } from "react";
import Link from "next/link";
import {
  CheckCircle2,
  Copy,
  Download,
  Eye,
  MoreHorizontal,
  Pencil,
  Plus,
  RefreshCcw,
  Slash,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/shared/status-badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { LICENSE_TYPES } from "@/lib/constants";
import { copyToClipboard, formatDate, truncate } from "@/lib/utils";
import type { License } from "@/types";

// Sample data for UI demonstration
const SAMPLE_LICENSES: License[] = Array.from({ length: 10 }).map((_, i) => ({
  id: `lic-${i + 1}`,
  licenseKey: `LK-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}-${Math.random().toString(36).substring(2, 10).toUpperCase()}`,
  activationToken: `AT-${Math.random().toString(36).substring(2, 18).toUpperCase()}`,
  customerId: `cust-${i + 1}`,
  productId: "prod-1",
  licenseType: ((i % 7) + 1) as License["licenseType"],
  status: ((i % 5) + 1) as License["status"],
  maxUsers: 10,
  maxBranches: 3,
  maxDomains: 1,
  maxDevices: 5,
  maxConcurrentLogins: 5,
  maxApiCalls: 100000,
  maxStorageGB: 50,
  maxEmployees: 100,
  maxCustomers: 1000,
  maxLoans: 10000,
  maxCollections: 10000,
  startDate: new Date(Date.now() - 30 * 86400000).toISOString(),
  expiryDate: new Date(Date.now() + 335 * 86400000).toISOString(),
  domainLockEnabled: true,
  hardwareLockEnabled: false,
  ipLockEnabled: false,
  countryLockEnabled: false,
  inGracePeriod: false,
  gracePeriodDays: 7,
  price: 999.0,
  currency: "USD",
  autoRenewal: false,
  customer: {
    id: `cust-${i + 1}`,
    customerCode: `CUST00${i + 1}`,
    name: ["ABC Tech", "XYZ Corp", "Innovate Ltd", "Global Inc", "Smart Systems"][i % 5],
    email: `customer${i + 1}@example.com`,
    phone: "+1234567890",
    companyName: ["ABC Technologies", "XYZ Corporation", "Innovate Pvt Ltd", "Global Inc", "Smart Systems Ltd"][i % 5],
    isActive: true,
    isVerified: true,
    createdAt: new Date().toISOString(),
  },
  createdAt: new Date(Date.now() - i * 86400000).toISOString(),
}));

export default function LicensesPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const handleCopyKey = (key: string) => {
    copyToClipboard(key);
    toast.success("License key copied to clipboard");
  };

  const columns: Column<License>[] = [
    {
      key: "licenseKey",
      header: "License Key",
      render: (license) => (
        <div className="flex items-center gap-2">
          <code className="rounded bg-muted px-1.5 py-0.5 text-xs font-mono">
            {truncate(license.licenseKey, 28)}
          </code>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={() => handleCopyKey(license.licenseKey)}
          >
            <Copy className="h-3 w-3" />
          </Button>
        </div>
      ),
    },
    {
      key: "customer",
      header: "Customer",
      render: (license) => (
        <div>
          <p className="font-medium">{license.customer?.name || "—"}</p>
          <p className="text-xs text-muted-foreground">{license.customer?.companyName}</p>
        </div>
      ),
    },
    {
      key: "licenseType",
      header: "Type",
      render: (license) => {
        const type = LICENSE_TYPES.find((t) => t.value === license.licenseType);
        return <Badge variant="outline">{type?.label || "Unknown"}</Badge>;
      },
    },
    {
      key: "status",
      header: "Status",
      render: (license) => <StatusBadge status={license.status} />,
    },
    {
      key: "expiryDate",
      header: "Expires",
      render: (license) => formatDate(license.expiryDate),
    },
    {
      key: "price",
      header: "Price",
      render: (license) => `$${license.price.toFixed(2)} ${license.currency}`,
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader
        title="Licenses"
        description="Manage all licenses, activations, and validations"
        actions={
          <>
            <Button variant="outline">
              <Download className="mr-2 h-4 w-4" />
              Export
            </Button>
            <Link href="/licenses/create">
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                Create License
              </Button>
            </Link>
          </>
        }
      />

      <DataTable
        columns={columns}
        data={SAMPLE_LICENSES}
        searchable
        searchPlaceholder="Search by license key, customer, email..."
        pagination={{
          page,
          pageSize,
          total: 1284,
          onPageChange: setPage,
        }}
        rowActions={(license) => (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`/licenses/${license.id}`}>
                  <Eye className="mr-2 h-4 w-4" />
                  View details
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={`/licenses/${license.id}/edit`}>
                  <Pencil className="mr-2 h-4 w-4" />
                  Edit
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link href={`/licenses/${license.id}/renew`}>
                  <RefreshCcw className="mr-2 h-4 w-4" />
                  Renew
                </Link>
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem asChild>
                <Link
                  href={`/licenses/${license.id}/suspend`}
                  className="text-orange-600"
                >
                  <Slash className="mr-2 h-4 w-4" />
                  Suspend
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem asChild>
                <Link
                  href={`/licenses/${license.id}/revoke`}
                  className="text-destructive"
                >
                  <XCircle className="mr-2 h-4 w-4" />
                  Revoke
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleCopyKey(license.licenseKey)}>
                <Copy className="mr-2 h-4 w-4" />
                Copy key
              </DropdownMenuItem>
              {license.status === 1 && (
                <DropdownMenuItem className="text-green-600">
                  <CheckCircle2 className="mr-2 h-4 w-4" />
                  Activate
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      />
    </div>
  );
}
