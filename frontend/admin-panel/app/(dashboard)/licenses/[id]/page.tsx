"use client";

import { use } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  Calendar,
  Copy,
  Cpu,
  Globe,
  KeyRound,
  Pencil,
  RefreshCcw,
  Shield,
  Slash,
  Users,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { StatusBadge } from "@/components/shared/status-badge";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { LICENSE_TYPES } from "@/lib/constants";
import { copyToClipboard, formatDate, formatDateTime } from "@/lib/utils";

interface DetailRow {
  label: string;
  value: React.ReactNode;
}

function DetailItem({ label, value }: DetailRow) {
  return (
    <div className="space-y-1">
      <dt className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
        {label}
      </dt>
      <dd className="text-sm">{value || "—"}</dd>
    </div>
  );
}

export default function LicenseDetailsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);

  // Sample license data
  const license = {
    id,
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-I9J0K1L2-M3N4O5P6",
    activationToken: "AT-A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6",
    status: 2,
    licenseType: 5,
    customer: {
      name: "John Doe",
      companyName: "ABC Technologies",
      email: "john@abctech.com",
      phone: "+1 234 567 8900",
    },
    product: {
      name: "Finance ERP System",
      version: "1.0.0",
    },
    startDate: new Date(Date.now() - 30 * 86400000).toISOString(),
    expiryDate: new Date(Date.now() + 335 * 86400000).toISOString(),
    activatedAt: new Date(Date.now() - 25 * 86400000).toISOString(),
    lastValidatedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    maxUsers: 10,
    maxBranches: 3,
    maxDomains: 1,
    maxDevices: 5,
    maxApiCalls: 100000,
    maxStorageGB: 50,
    domainLockEnabled: true,
    hardwareLockEnabled: false,
    ipLockEnabled: false,
    countryLockEnabled: false,
    price: 999.0,
    currency: "USD",
    autoRenewal: false,
    domains: [
      { id: "d1", domainName: "abctech.com", isVerified: true, isPrimary: true },
      { id: "d2", domainName: "*.abctech.com", isVerified: true, isPrimary: false },
    ],
    devices: [
      { id: "dv1", deviceName: "Production Server", os: "Ubuntu 22.04", lastSeen: new Date().toISOString() },
      { id: "dv2", deviceName: "Backup Server", os: "Ubuntu 22.04", lastSeen: new Date(Date.now() - 86400000).toISOString() },
    ],
  };

  const licenseType = LICENSE_TYPES.find((t) => t.value === license.licenseType);

  const handleCopy = (text: string, label: string) => {
    copyToClipboard(text);
    toast.success(`${label} copied to clipboard`);
  };

  return (
    <div>
      <PageHeader
        title="License Details"
        description={license.customer.companyName}
        actions={
          <>
            <Link href="/licenses">
              <Button variant="outline">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back
              </Button>
            </Link>
            <Link href={`/licenses/${id}/edit`}>
              <Button variant="outline">
                <Pencil className="mr-2 h-4 w-4" />
                Edit
              </Button>
            </Link>
            <Link href={`/licenses/${id}/renew`}>
              <Button>
                <RefreshCcw className="mr-2 h-4 w-4" />
                Renew
              </Button>
            </Link>
          </>
        }
      />

      {/* Quick Actions Bar */}
      <Card className="mb-6">
        <CardContent className="flex flex-wrap items-center gap-4 p-6">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
              <KeyRound className="h-6 w-6 text-primary" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <code className="rounded bg-muted px-2 py-1 text-sm font-mono">
                  {license.licenseKey}
                </code>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  onClick={() => handleCopy(license.licenseKey, "License key")}
                >
                  <Copy className="h-3 w-3" />
                </Button>
              </div>
              <div className="mt-1 flex items-center gap-2">
                <StatusBadge status={license.status} />
                <Badge variant="outline">{licenseType?.label}</Badge>
                {license.autoRenewal && <Badge variant="info">Auto-renewal</Badge>}
              </div>
            </div>
          </div>

          <Separator orientation="vertical" className="hidden h-12 lg:block" />

          <div className="flex flex-1 flex-wrap gap-2 lg:justify-end">
            <Link href={`/licenses/${id}/suspend`}>
              <Button variant="outline" className="text-orange-600 hover:text-orange-700">
                <Slash className="mr-2 h-4 w-4" />
                Suspend
              </Button>
            </Link>
            <Link href={`/licenses/${id}/revoke`}>
              <Button variant="outline" className="text-destructive hover:text-destructive">
                <XCircle className="mr-2 h-4 w-4" />
                Revoke
              </Button>
            </Link>
          </div>
        </CardContent>
      </Card>

      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="domains">Domains ({license.domains.length})</TabsTrigger>
          <TabsTrigger value="devices">Devices ({license.devices.length})</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Customer Information</CardTitle>
              </CardHeader>
              <CardContent>
                <dl className="grid gap-4 sm:grid-cols-2">
                  <DetailItem label="Customer Name" value={license.customer.name} />
                  <DetailItem label="Company" value={license.customer.companyName} />
                  <DetailItem label="Email" value={license.customer.email} />
                  <DetailItem label="Phone" value={license.customer.phone} />
                </dl>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Product & Pricing</CardTitle>
              </CardHeader>
              <CardContent>
                <dl className="grid gap-4 sm:grid-cols-2">
                  <DetailItem label="Product" value={license.product.name} />
                  <DetailItem label="Version" value={license.product.version} />
                  <DetailItem
                    label="Price"
                    value={`$${license.price.toFixed(2)} ${license.currency}`}
                  />
                  <DetailItem
                    label="Auto Renewal"
                    value={license.autoRenewal ? "Enabled" : "Disabled"}
                  />
                </dl>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Validity Period</CardTitle>
                <CardDescription>License duration and validation</CardDescription>
              </CardHeader>
              <CardContent>
                <dl className="grid gap-4 sm:grid-cols-2">
                  <DetailItem
                    label="Start Date"
                    value={
                      <span className="inline-flex items-center gap-1.5">
                        <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                        {formatDate(license.startDate)}
                      </span>
                    }
                  />
                  <DetailItem
                    label="Expiry Date"
                    value={
                      <span className="inline-flex items-center gap-1.5">
                        <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                        {formatDate(license.expiryDate)}
                      </span>
                    }
                  />
                  <DetailItem
                    label="Activated At"
                    value={license.activatedAt ? formatDateTime(license.activatedAt) : "—"}
                  />
                  <DetailItem
                    label="Last Validated"
                    value={
                      license.lastValidatedAt ? formatDateTime(license.lastValidatedAt) : "—"
                    }
                  />
                </dl>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Usage Limits</CardTitle>
              </CardHeader>
              <CardContent>
                <dl className="grid gap-4 sm:grid-cols-2">
                  <DetailItem
                    label="Users"
                    value={
                      <span className="inline-flex items-center gap-1.5">
                        <Users className="h-3.5 w-3.5 text-muted-foreground" />
                        {license.maxUsers}
                      </span>
                    }
                  />
                  <DetailItem label="Branches" value={license.maxBranches} />
                  <DetailItem
                    label="Domains"
                    value={
                      <span>
                        {license.domains.length} / {license.maxDomains}
                      </span>
                    }
                  />
                  <DetailItem
                    label="Devices"
                    value={
                      <span>
                        {license.devices.length} / {license.maxDevices}
                      </span>
                    }
                  />
                  <DetailItem
                    label="API Calls"
                    value={license.maxApiCalls.toLocaleString()}
                  />
                  <DetailItem label="Storage" value={`${license.maxStorageGB} GB`} />
                </dl>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Security Configuration</CardTitle>
              <CardDescription>License lock and protection settings</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              {[
                { label: "Domain Lock", enabled: license.domainLockEnabled, icon: Globe },
                { label: "Hardware Lock", enabled: license.hardwareLockEnabled, icon: Cpu },
                { label: "IP Whitelist", enabled: license.ipLockEnabled, icon: Shield },
                {
                  label: "Country Restriction",
                  enabled: license.countryLockEnabled,
                  icon: Shield,
                },
              ].map((feature) => (
                <div
                  key={feature.label}
                  className="flex items-center gap-3 rounded-lg border p-3"
                >
                  <div
                    className={`flex h-9 w-9 items-center justify-center rounded-md ${
                      feature.enabled
                        ? "bg-green-100 text-green-600 dark:bg-green-900/30"
                        : "bg-muted text-muted-foreground"
                    }`}
                  >
                    <feature.icon className="h-4 w-4" />
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium">{feature.label}</p>
                    <p className="text-xs text-muted-foreground">
                      {feature.enabled ? "Enabled" : "Disabled"}
                    </p>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="domains">
          <Card>
            <CardHeader>
              <CardTitle>Registered Domains</CardTitle>
              <CardDescription>
                Domains authorized to use this license ({license.domains.length}/
                {license.maxDomains})
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {license.domains.map((d) => (
                  <div
                    key={d.id}
                    className="flex items-center justify-between rounded-lg border p-4"
                  >
                    <div className="flex items-center gap-3">
                      <Globe className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <p className="font-medium">{d.domainName}</p>
                        <div className="mt-1 flex gap-1">
                          {d.isPrimary && <Badge variant="info">Primary</Badge>}
                          {d.isVerified && <Badge variant="success">Verified</Badge>}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="devices">
          <Card>
            <CardHeader>
              <CardTitle>Registered Devices</CardTitle>
              <CardDescription>
                Devices authorized to use this license ({license.devices.length}/
                {license.maxDevices})
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {license.devices.map((dv) => (
                  <div
                    key={dv.id}
                    className="flex items-center justify-between rounded-lg border p-4"
                  >
                    <div className="flex items-center gap-3">
                      <Cpu className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <p className="font-medium">{dv.deviceName}</p>
                        <p className="text-xs text-muted-foreground">{dv.os}</p>
                      </div>
                    </div>
                    <div className="text-right text-xs text-muted-foreground">
                      Last seen: {formatDateTime(dv.lastSeen)}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="history">
          <Card>
            <CardHeader>
              <CardTitle>License History</CardTitle>
              <CardDescription>All actions performed on this license</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {[
                  { action: "License Activated", time: license.activatedAt, by: "System" },
                  { action: "License Created", time: license.startDate, by: "Admin User" },
                ].map((item, idx) => (
                  <div key={idx} className="flex items-start gap-3 border-b pb-4 last:border-0">
                    <div className="mt-1 h-2 w-2 rounded-full bg-primary" />
                    <div className="flex-1">
                      <p className="text-sm font-medium">{item.action}</p>
                      <p className="text-xs text-muted-foreground">
                        {formatDateTime(item.time)} · by {item.by}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
