"use client";

import { useState } from "react";
import {
  Box,
  Download,
  Eye,
  MoreHorizontal,
  Pencil,
  Plus,
  Trash2,
} from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Search } from "lucide-react";
import { formatCurrency, formatDate } from "@/lib/utils";

const SAMPLE_PRODUCTS = Array.from({ length: 6 }).map((_, i) => ({
  id: `prod-${i + 1}`,
  productCode: `PROD-00${i + 1}`,
  name: [
    "Finance ERP System",
    "CRM Pro",
    "Inventory Manager",
    "HR Suite",
    "Project Tracker",
    "POS System",
  ][i],
  description: "Complete enterprise solution",
  version: `${i + 1}.0.0`,
  isActive: i % 4 !== 3,
  basePrice: [999, 599, 799, 449, 349, 549][i],
  currency: "USD",
  trialDays: 14,
  allowTrial: true,
  totalLicenses: [125, 89, 67, 45, 32, 28][i],
  activeLicenses: [110, 78, 60, 40, 30, 25][i],
  createdAt: new Date(Date.now() - i * 30 * 86400000).toISOString(),
}));

export default function ProductsPage() {
  const [search, setSearch] = useState("");

  const filtered = SAMPLE_PRODUCTS.filter(
    (p) =>
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      p.productCode.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div>
      <PageHeader
        title="Products"
        description="Manage products available for licensing"
        actions={
          <>
            <Button variant="outline">
              <Download className="mr-2 h-4 w-4" />
              Export
            </Button>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Add Product
            </Button>
          </>
        }
      />

      <div className="mb-6 max-w-sm">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search products..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {filtered.map((product) => (
          <Card key={product.id} className="overflow-hidden hover:shadow-md transition-shadow">
            <CardContent className="p-0">
              <div className="flex items-start justify-between border-b bg-muted/30 p-6">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
                    <Box className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <h3 className="font-semibold">{product.name}</h3>
                    <p className="text-xs text-muted-foreground">
                      {product.productCode} · v{product.version}
                    </p>
                  </div>
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-8 w-8">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem>
                      <Eye className="mr-2 h-4 w-4" />
                      View
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
              </div>

              <div className="space-y-4 p-6">
                <div className="flex items-center justify-between">
                  <Badge variant={product.isActive ? "success" : "outline"}>
                    {product.isActive ? "Active" : "Inactive"}
                  </Badge>
                  {product.allowTrial && (
                    <span className="text-xs text-muted-foreground">
                      {product.trialDays}-day trial
                    </span>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-xs uppercase text-muted-foreground">Base Price</p>
                    <p className="text-lg font-semibold">
                      {formatCurrency(product.basePrice, product.currency)}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs uppercase text-muted-foreground">Released</p>
                    <p className="font-medium">{formatDate(product.createdAt)}</p>
                  </div>
                </div>

                <div className="border-t pt-4">
                  <div className="grid grid-cols-2 gap-2 text-center text-sm">
                    <div>
                      <p className="text-2xl font-bold">{product.totalLicenses}</p>
                      <p className="text-xs text-muted-foreground">Total Licenses</p>
                    </div>
                    <div>
                      <p className="text-2xl font-bold text-green-600">
                        {product.activeLicenses}
                      </p>
                      <p className="text-xs text-muted-foreground">Active</p>
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
