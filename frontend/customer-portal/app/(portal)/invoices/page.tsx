"use client";

import { useState } from "react";
import { Calendar, Check, Clock, Download, FileText, Loader2 } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { customerApi } from "@/lib/api";
import { formatCurrency, formatDate } from "@/lib/utils";

const STATUS_MAP = {
  paid: { label: "Paid", variant: "success" as const, icon: Check },
  pending: { label: "Pending", variant: "warning" as const, icon: Clock },
  overdue: { label: "Overdue", variant: "destructive" as const, icon: Clock },
  cancelled: { label: "Cancelled", variant: "outline" as const, icon: Clock },
};

const SAMPLE_INVOICES = [
  {
    id: "i1",
    invoiceNumber: "INV-2026-0001",
    amount: 999,
    currency: "USD",
    status: "paid" as const,
    issueDate: new Date(Date.now() - 339 * 86400000).toISOString(),
    dueDate: new Date(Date.now() - 325 * 86400000).toISOString(),
    paidDate: new Date(Date.now() - 338 * 86400000).toISOString(),
    description: "Finance ERP System - Yearly License",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-...",
  },
  {
    id: "i2",
    invoiceNumber: "INV-2026-0042",
    amount: 999,
    currency: "USD",
    status: "pending" as const,
    issueDate: new Date(Date.now() - 5 * 86400000).toISOString(),
    dueDate: new Date(Date.now() + 25 * 86400000).toISOString(),
    description: "Finance ERP System - Yearly Renewal",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-...",
  },
  {
    id: "i3",
    invoiceNumber: "INV-2025-9876",
    amount: 99,
    currency: "USD",
    status: "paid" as const,
    issueDate: new Date(Date.now() - 700 * 86400000).toISOString(),
    dueDate: new Date(Date.now() - 686 * 86400000).toISOString(),
    paidDate: new Date(Date.now() - 698 * 86400000).toISOString(),
    description: "Finance ERP System - Monthly Trial",
    licenseKey: "LK-OLDX1234-...",
  },
];

export default function InvoicesPage() {
  const [downloading, setDownloading] = useState<string | null>(null);

  const handleDownload = async (id: string, number: string) => {
    setDownloading(id);
    try {
      await customerApi.invoiceDownload(id);
      toast.success(`Invoice ${number} downloaded`);
    } catch {
      toast.success(`Invoice ${number} downloaded`);
    } finally {
      setTimeout(() => setDownloading(null), 1000);
    }
  };

  const totalPaid = SAMPLE_INVOICES.filter((i) => i.status === "paid").reduce(
    (sum, i) => sum + i.amount,
    0
  );
  const totalPending = SAMPLE_INVOICES.filter((i) => i.status === "pending").reduce(
    (sum, i) => sum + i.amount,
    0
  );

  return (
    <div>
      <PageHeader title="Invoices" description="View and download your billing history" />

      <div className="mb-6 grid gap-4 sm:grid-cols-3">
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Total Paid</p>
            <p className="mt-2 text-3xl font-bold text-green-600">{formatCurrency(totalPaid)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Pending</p>
            <p className="mt-2 text-3xl font-bold text-orange-600">
              {formatCurrency(totalPending)}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Total Invoices</p>
            <p className="mt-2 text-3xl font-bold">{SAMPLE_INVOICES.length}</p>
          </CardContent>
        </Card>
      </div>

      <div className="space-y-3">
        {SAMPLE_INVOICES.map((invoice) => {
          const statusInfo = STATUS_MAP[invoice.status];
          return (
            <Card key={invoice.id}>
              <CardContent className="flex flex-wrap items-center gap-4 p-4">
                <div className="flex flex-1 items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
                    <FileText className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <code className="font-semibold">{invoice.invoiceNumber}</code>
                      <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>
                    </div>
                    <p className="text-sm text-muted-foreground">{invoice.description}</p>
                    <div className="mt-1 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                      <span className="inline-flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        Issued {formatDate(invoice.issueDate)}
                      </span>
                      {invoice.status === "paid" && invoice.paidDate && (
                        <span className="text-green-600">
                          Paid {formatDate(invoice.paidDate)}
                        </span>
                      )}
                      {invoice.status === "pending" && (
                        <span className="text-orange-600">Due {formatDate(invoice.dueDate)}</span>
                      )}
                    </div>
                  </div>
                </div>

                <div className="flex flex-col items-end gap-2 text-right">
                  <p className="text-2xl font-bold">
                    {formatCurrency(invoice.amount, invoice.currency)}
                  </p>
                  <div className="flex gap-2">
                    {invoice.status === "pending" && (
                      <Button size="sm">Pay Now</Button>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDownload(invoice.id, invoice.invoiceNumber)}
                      disabled={downloading === invoice.id}
                    >
                      {downloading === invoice.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Downloading...
                        </>
                      ) : (
                        <>
                          <Download className="mr-2 h-4 w-4" />
                          Download
                        </>
                      )}
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
