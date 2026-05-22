"use client";

import { useState } from "react";
import {
  CheckCircle2,
  Download,
  Eye,
  Mail,
  MoreHorizontal,
  Pencil,
  Phone,
  Plus,
  Trash2,
} from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { DataTable, Column } from "@/components/shared/data-table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { formatDate, getInitials } from "@/lib/utils";
import type { Customer } from "@/types";

const SAMPLE_CUSTOMERS: Customer[] = Array.from({ length: 10 }).map((_, i) => ({
  id: `cust-${i + 1}`,
  customerCode: `CUST00${i + 1}`,
  name: ["John Doe", "Jane Smith", "Robert Brown", "Emily Davis", "Michael Wilson"][i % 5],
  email: `customer${i + 1}@example.com`,
  phone: `+1 234 567 89${String(i).padStart(2, "0")}`,
  companyName: ["ABC Tech", "XYZ Corp", "Innovate Pvt", "Global Inc", "Smart Sys"][i % 5],
  city: ["Mumbai", "New York", "London", "Singapore", "Dubai"][i % 5],
  country: ["India", "USA", "UK", "Singapore", "UAE"][i % 5],
  isActive: i % 4 !== 0,
  isVerified: i % 3 !== 0,
  createdAt: new Date(Date.now() - i * 86400000 * 10).toISOString(),
}));

export default function CustomersPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const columns: Column<Customer>[] = [
    {
      key: "name",
      header: "Customer",
      render: (c) => (
        <div className="flex items-center gap-3">
          <Avatar className="h-9 w-9">
            <AvatarFallback className="bg-primary/10 text-primary text-sm">
              {getInitials(c.name)}
            </AvatarFallback>
          </Avatar>
          <div>
            <p className="font-medium">{c.name}</p>
            <p className="text-xs text-muted-foreground">{c.customerCode}</p>
          </div>
        </div>
      ),
    },
    {
      key: "company",
      header: "Company",
      render: (c) => (
        <div>
          <p>{c.companyName || "—"}</p>
          <p className="text-xs text-muted-foreground">
            {c.city}, {c.country}
          </p>
        </div>
      ),
    },
    {
      key: "contact",
      header: "Contact",
      render: (c) => (
        <div className="space-y-0.5">
          <p className="flex items-center gap-1.5 text-sm">
            <Mail className="h-3 w-3 text-muted-foreground" />
            {c.email}
          </p>
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Phone className="h-3 w-3" />
            {c.phone}
          </p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (c) => (
        <div className="flex flex-col gap-1">
          <Badge variant={c.isActive ? "success" : "outline"}>
            {c.isActive ? "Active" : "Inactive"}
          </Badge>
          {c.isVerified && (
            <span className="inline-flex items-center gap-1 text-xs text-green-600">
              <CheckCircle2 className="h-3 w-3" />
              Verified
            </span>
          )}
        </div>
      ),
    },
    {
      key: "createdAt",
      header: "Joined",
      render: (c) => formatDate(c.createdAt),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Customers"
        description="Manage your customer accounts and information"
        actions={
          <>
            <Button variant="outline">
              <Download className="mr-2 h-4 w-4" />
              Export
            </Button>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Add Customer
            </Button>
          </>
        }
      />

      <DataTable
        columns={columns}
        data={SAMPLE_CUSTOMERS}
        searchable
        searchPlaceholder="Search customers by name, email, company..."
        pagination={{
          page,
          pageSize,
          total: 426,
          onPageChange: setPage,
        }}
        rowActions={(customer) => (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>
                <Eye className="mr-2 h-4 w-4" />
                View Details
              </DropdownMenuItem>
              <DropdownMenuItem>
                <Pencil className="mr-2 h-4 w-4" />
                Edit
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem className="text-destructive">
                <Trash2 className="mr-2 h-4 w-4" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      />
    </div>
  );
}
