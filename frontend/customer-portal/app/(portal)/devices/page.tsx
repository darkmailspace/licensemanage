"use client";

import { Cpu, MapPin, ShieldOff } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatRelativeTime } from "@/lib/utils";

const SAMPLE_DEVICES = [
  {
    id: "dv1",
    deviceName: "Production Server",
    deviceFingerprint: "FP-A1B2C3D4E5F6G7H8I9J0K1L2",
    operatingSystem: "Ubuntu 22.04",
    architecture: "x86_64",
    isVirtualMachine: false,
    ipAddress: "203.0.113.45",
    country: "USA",
    city: "New York",
    isActive: true,
    lastAccessedAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
    firstActivatedAt: new Date(Date.now() - 339 * 86400000).toISOString(),
  },
  {
    id: "dv2",
    deviceName: "Backup Server",
    deviceFingerprint: "FP-Z9Y8X7W6V5U4T3S2R1Q0P0O9",
    operatingSystem: "Ubuntu 22.04",
    architecture: "x86_64",
    isVirtualMachine: true,
    ipAddress: "203.0.113.46",
    country: "USA",
    city: "New York",
    isActive: true,
    lastAccessedAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    firstActivatedAt: new Date(Date.now() - 200 * 86400000).toISOString(),
  },
];

export default function DevicesPage() {
  const handleDeactivate = (id: string) => {
    toast.success(`Device ${id} deactivation requested`);
  };

  return (
    <div>
      <PageHeader title="Devices" description="View devices registered to your licenses" />

      <div className="grid gap-3">
        {SAMPLE_DEVICES.map((device) => (
          <Card key={device.id}>
            <CardContent className="flex flex-wrap items-start gap-4 p-4">
              <div className="flex flex-1 items-start gap-3">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10">
                  <Cpu className="h-5 w-5 text-primary" />
                </div>
                <div className="space-y-2">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-medium">{device.deviceName}</p>
                      {device.isVirtualMachine && (
                        <Badge variant="outline" className="text-xs">
                          VM
                        </Badge>
                      )}
                    </div>
                    <code className="text-xs text-muted-foreground">{device.deviceFingerprint}</code>
                  </div>
                  <dl className="flex flex-wrap gap-x-4 gap-y-1 text-xs">
                    <div>
                      <dt className="inline text-muted-foreground">OS: </dt>
                      <dd className="inline font-medium">{device.operatingSystem}</dd>
                    </div>
                    <div>
                      <dt className="inline text-muted-foreground">IP: </dt>
                      <dd className="inline font-mono">{device.ipAddress}</dd>
                    </div>
                    <div className="flex items-center gap-1">
                      <MapPin className="h-3 w-3 text-muted-foreground" />
                      <span className="font-medium">
                        {device.city}, {device.country}
                      </span>
                    </div>
                  </dl>
                </div>
              </div>

              <div className="flex flex-col items-end gap-2 text-right">
                <Badge variant={device.isActive ? "success" : "outline"}>
                  {device.isActive ? "Active" : "Inactive"}
                </Badge>
                <p className="text-xs text-muted-foreground">
                  Last access: {formatRelativeTime(device.lastAccessedAt)}
                </p>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleDeactivate(device.id)}
                  className="h-7 text-destructive hover:text-destructive"
                >
                  <ShieldOff className="mr-1.5 h-3.5 w-3.5" />
                  Deactivate
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="mt-6 border-blue-200 bg-blue-50 dark:border-blue-900/40 dark:bg-blue-900/10">
        <CardContent className="p-4 text-sm">
          <p className="font-medium text-blue-900 dark:text-blue-300">Device management</p>
          <p className="mt-1 text-blue-800 dark:text-blue-300/80">
            Deactivating a device frees up a slot in your license. Inactive devices can be
            reactivated by the device itself or by your administrator.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
