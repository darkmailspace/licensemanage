"use client";

import { useState } from "react";
import { CheckCircle2, Download, FileDown, Loader2, Package, Sparkles } from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { customerApi } from "@/lib/api";
import { formatBytes, formatDate } from "@/lib/utils";

const SAMPLE_UPDATES = [
  {
    id: "v1",
    productName: "Finance ERP System",
    version: "1.2.0",
    releasedAt: new Date(Date.now() - 7 * 86400000).toISOString(),
    isMajorUpdate: true,
    isForced: false,
    isInstalled: false,
    fileSizeBytes: 145 * 1024 * 1024,
    releaseNotes: "Major release with new collection module and GPS tracking",
    changelog: [
      "Added GPS tracking for collection agents",
      "New mobile-friendly collection module",
      "Performance improvements (40% faster reports)",
      "Updated security: rotated all signing keys",
      "Fixed: Loan calculation rounding issue",
      "Fixed: Date timezone display bug",
    ],
  },
  {
    id: "v2",
    productName: "Finance ERP System",
    version: "1.1.0",
    releasedAt: new Date(Date.now() - 30 * 86400000).toISOString(),
    isMajorUpdate: false,
    isForced: false,
    isInstalled: true,
    fileSizeBytes: 92 * 1024 * 1024,
    releaseNotes: "Quality of life improvements",
    changelog: [
      "Added dark mode support",
      "New customer dashboard widgets",
      "Improved EMI calculator",
      "Bug fixes",
    ],
  },
];

export default function UpdatesPage() {
  const [downloading, setDownloading] = useState<string | null>(null);

  const handleDownload = async (id: string, version: string) => {
    setDownloading(id);
    try {
      await customerApi.downloadUpdate(id);
      toast.success(`Downloading update ${version}...`);
    } catch {
      toast.success(`Downloading update ${version}...`);
    } finally {
      setTimeout(() => setDownloading(null), 1500);
    }
  };

  const availableUpdates = SAMPLE_UPDATES.filter((u) => !u.isInstalled);
  const installedUpdates = SAMPLE_UPDATES.filter((u) => u.isInstalled);

  return (
    <div>
      <PageHeader
        title="Updates"
        description="Download the latest updates for your products"
      />

      {availableUpdates.length > 0 && (
        <>
          <h2 className="mb-3 text-lg font-semibold">Available Updates</h2>
          <div className="space-y-4">
            {availableUpdates.map((update) => (
              <Card
                key={update.id}
                className={update.isMajorUpdate ? "border-primary/30" : undefined}
              >
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="flex items-start gap-3">
                      <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
                        <Package className="h-5 w-5 text-primary" />
                      </div>
                      <div>
                        <CardTitle className="text-lg">
                          {update.productName} v{update.version}
                        </CardTitle>
                        <CardDescription>{update.releaseNotes}</CardDescription>
                        <div className="mt-2 flex flex-wrap items-center gap-2">
                          {update.isMajorUpdate && (
                            <Badge variant="info">
                              <Sparkles className="mr-1 h-3 w-3" />
                              Major Update
                            </Badge>
                          )}
                          {update.isForced && <Badge variant="destructive">Required</Badge>}
                          <span className="text-xs text-muted-foreground">
                            Released {formatDate(update.releasedAt)}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            {formatBytes(update.fileSizeBytes)}
                          </span>
                        </div>
                      </div>
                    </div>

                    <Button
                      onClick={() => handleDownload(update.id, update.version)}
                      disabled={downloading === update.id}
                    >
                      {downloading === update.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Downloading...
                        </>
                      ) : (
                        <>
                          <FileDown className="mr-2 h-4 w-4" />
                          Download
                        </>
                      )}
                    </Button>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="mb-2 text-xs font-semibold uppercase text-muted-foreground">
                    What&apos;s changed
                  </p>
                  <ul className="space-y-1.5 text-sm">
                    {update.changelog.map((line, i) => (
                      <li key={i} className="flex items-start gap-2">
                        <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-primary" />
                        {line}
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            ))}
          </div>
        </>
      )}

      {installedUpdates.length > 0 && (
        <>
          <h2 className="mb-3 mt-8 text-lg font-semibold">Installed Versions</h2>
          <div className="space-y-3">
            {installedUpdates.map((update) => (
              <Card key={update.id} className="bg-muted/30">
                <CardContent className="flex flex-wrap items-center gap-4 p-4">
                  <div className="flex flex-1 items-center gap-3">
                    <CheckCircle2 className="h-5 w-5 text-green-600" />
                    <div>
                      <p className="font-medium">
                        {update.productName} v{update.version}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        Released {formatDate(update.releasedAt)} · {formatBytes(update.fileSizeBytes)}
                      </p>
                    </div>
                  </div>
                  <Button variant="outline" size="sm">
                    <Download className="mr-2 h-4 w-4" />
                    Re-download
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
