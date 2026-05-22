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
  RefreshCcw,
  Shield,
  TrendingUp,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { LicenseStatusBadge } from "@/components/shared/license-status-badge";
import { copyToClipboard, formatDate, formatDateTime } from "@/lib/utils";

export default function LicenseDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);

  const license = {
    id,
    productName: "Finance ERP System",
    productVersion: "1.0.0",
    licenseKey: "LK-A1B2C3D4-E5F6G7H8-I9J0K1L2-M3N4O5P6",
    status: 2,
    licenseTypeLabel: "Yearly",
    startDate: new Date(Date.now() - 340 * 86400000).toISOString(),
    expiryDate: new Date(Date.now() + 25 * 86400000).toISOString(),
    activatedAt: new Date(Date.now() - 339 * 86400000).toISOString(),
    lastValidatedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    maxUsers: 10,
    maxBranches: 3,
    maxDomains: 3,
    maxDevices: 5,
    maxApiCalls: 100000,
    maxStorageGB: 50,
    domainLockEnabled: true,
    hardwareLockEnabled: false,
    autoRenewal: false,
  };

  const copy = (text: string) => {
    copyToClipboard(text);
    toast.success("Copied to clipboard");
  };

  return (
    <div>
      <PageHeader
        title={license.productName}
        description={`v${license.productVersion}`}
        actions={
          <>
            <Link href="/licenses">
              <Button variant="outline">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back
              </Button>
            </Link>
            <Link href="/renew">
              <Button>
                <RefreshCcw className="mr-2 h-4 w-4" />
                Renew
              </Button>
            </Link>
          </>
        }
      />

      <Card className="mb-6">
        <CardContent className="flex flex-wrap items-center gap-4 p-6">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
              <KeyRound className="h-6 w-6 text-primary" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <code className="rounded bg-muted px-2 py-1 text-sm">{license.licenseKey}</code>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  onClick={() => copy(license.licenseKey)}
                >
                  <Copy className="h-3 w-3" />
                </Button>
              </div>
              <div className="mt-1 flex items-center gap-2">
                <LicenseStatusBadge status={license.status} />
                <Badge variant="outline">{license.licenseTypeLabel}</Badge>
                {license.autoRenewal && <Badge variant="info">Auto-renewal</Badge>}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="security">Security</TabsTrigger>
          <TabsTrigger value="usage">Usage</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Validity</CardTitle>
              <CardDescription>License duration and validation</CardDescription>
            </CardHeader>
            <CardContent>
              <dl className="grid gap-4 sm:grid-cols-2">
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Start Date</dt>
                  <dd className="mt-1 flex items-center gap-1.5 font-medium">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    {formatDate(license.startDate)}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Expiry Date</dt>
                  <dd className="mt-1 flex items-center gap-1.5 font-medium">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    {formatDate(license.expiryDate)}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Activated</dt>
                  <dd className="mt-1 font-medium">{formatDateTime(license.activatedAt)}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Last Validated</dt>
                  <dd className="mt-1 font-medium">{formatDateTime(license.lastValidatedAt)}</dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="security">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Security Settings</CardTitle>
              <CardDescription>License protection settings</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3 sm:grid-cols-2">
              {[
                { label: "Domain Lock", enabled: license.domainLockEnabled, icon: Globe },
                { label: "Hardware Lock", enabled: license.hardwareLockEnabled, icon: Cpu },
                { label: "RSA-4096 Signature", enabled: true, icon: Shield },
                { label: "AES-256 Encryption", enabled: true, icon: Shield },
              ].map((f) => (
                <div key={f.label} className="flex items-center gap-3 rounded-lg border p-3">
                  <div
                    className={`flex h-9 w-9 items-center justify-center rounded-md ${
                      f.enabled ? "bg-green-100 text-green-600" : "bg-muted text-muted-foreground"
                    }`}
                  >
                    <f.icon className="h-4 w-4" />
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium">{f.label}</p>
                    <p className="text-xs text-muted-foreground">
                      {f.enabled ? "Enabled" : "Disabled"}
                    </p>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="usage">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Usage Limits</CardTitle>
              <CardDescription>Resource and quota limits for this license</CardDescription>
            </CardHeader>
            <CardContent>
              <dl className="grid gap-4 sm:grid-cols-2">
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Max Users</dt>
                  <dd className="mt-1 text-lg font-semibold">{license.maxUsers}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Max Branches</dt>
                  <dd className="mt-1 text-lg font-semibold">{license.maxBranches}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Max Domains</dt>
                  <dd className="mt-1 text-lg font-semibold">{license.maxDomains}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Max Devices</dt>
                  <dd className="mt-1 text-lg font-semibold">{license.maxDevices}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">API Calls</dt>
                  <dd className="mt-1 text-lg font-semibold">
                    {license.maxApiCalls.toLocaleString()}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs uppercase text-muted-foreground">Storage</dt>
                  <dd className="mt-1 text-lg font-semibold">{license.maxStorageGB} GB</dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <div className="mt-6 flex justify-end gap-3">
        <Link href="/upgrade">
          <Button variant="outline">
            <TrendingUp className="mr-2 h-4 w-4" />
            Upgrade Plan
          </Button>
        </Link>
        <Link href="/renew">
          <Button>
            <RefreshCcw className="mr-2 h-4 w-4" />
            Renew License
          </Button>
        </Link>
      </div>
    </div>
  );
}
