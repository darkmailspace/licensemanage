"use client";

import { CheckCircle2, Clock, Globe, Plus } from "lucide-react";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { formatDate, formatRelativeTime } from "@/lib/utils";

const SAMPLE_DOMAINS = [
  {
    id: "d1",
    domainName: "app.acme-tech.com",
    isWildcard: false,
    isPrimary: true,
    isVerified: true,
    isActive: true,
    verifiedAt: new Date(Date.now() - 339 * 86400000).toISOString(),
    lastAccessedAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
  },
  {
    id: "d2",
    domainName: "*.acme-tech.com",
    isWildcard: true,
    isPrimary: false,
    isVerified: true,
    isActive: true,
    verifiedAt: new Date(Date.now() - 200 * 86400000).toISOString(),
    lastAccessedAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(),
  },
];

export default function DomainsPage() {
  return (
    <div>
      <PageHeader
        title="Domains"
        description="View domains registered to your licenses"
        actions={
          <Button>
            <Plus className="mr-2 h-4 w-4" />
            Request New Domain
          </Button>
        }
      />

      <div className="grid gap-3">
        {SAMPLE_DOMAINS.map((domain) => (
          <Card key={domain.id}>
            <CardContent className="flex flex-wrap items-center gap-4 p-4">
              <div className="flex flex-1 items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary/10">
                  <Globe className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="font-medium">{domain.domainName}</p>
                  <div className="mt-0.5 flex flex-wrap items-center gap-1.5">
                    {domain.isWildcard && (
                      <Badge variant="outline" className="text-xs">
                        Wildcard
                      </Badge>
                    )}
                    {domain.isPrimary && (
                      <Badge variant="info" className="text-xs">
                        Primary
                      </Badge>
                    )}
                    {domain.isVerified ? (
                      <span className="inline-flex items-center gap-1 text-xs text-green-700 dark:text-green-400">
                        <CheckCircle2 className="h-3 w-3" />
                        Verified {formatDate(domain.verifiedAt)}
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-xs text-orange-700">
                        <Clock className="h-3 w-3" />
                        Pending verification
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <div className="flex flex-col items-end gap-1 text-right">
                <Badge variant={domain.isActive ? "success" : "outline"}>
                  {domain.isActive ? "Active" : "Inactive"}
                </Badge>
                <p className="text-xs text-muted-foreground">
                  Last access: {domain.lastAccessedAt ? formatRelativeTime(domain.lastAccessedAt) : "—"}
                </p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="mt-6 border-blue-200 bg-blue-50 dark:border-blue-900/40 dark:bg-blue-900/10">
        <CardContent className="p-4 text-sm">
          <p className="font-medium text-blue-900 dark:text-blue-300">Need to change your domain?</p>
          <p className="mt-1 text-blue-800 dark:text-blue-300/80">
            Domain changes require admin approval. Click &quot;Request New Domain&quot; above to
            submit a change request.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
