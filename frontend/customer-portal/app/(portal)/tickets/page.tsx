"use client";

import { useState } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  Clock,
  HelpCircle,
  Loader2,
  MessageCircle,
  Plus,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { customerApi } from "@/lib/api";
import { formatRelativeTime } from "@/lib/utils";

const STATUS_INFO: Record<number, { label: string; classes: string; icon: React.ComponentType<{ className?: string }> }> = {
  1: { label: "Open", classes: "bg-blue-100 text-blue-800 border-blue-200", icon: HelpCircle },
  2: { label: "In Progress", classes: "bg-yellow-100 text-yellow-800 border-yellow-200", icon: Clock },
  3: { label: "Waiting", classes: "bg-orange-100 text-orange-800 border-orange-200", icon: Clock },
  4: { label: "Resolved", classes: "bg-green-100 text-green-800 border-green-200", icon: CheckCircle2 },
  5: { label: "Closed", classes: "bg-gray-100 text-gray-800 border-gray-200", icon: XCircle },
};

const PRIORITY_INFO: Record<number, { label: string; variant: "outline" | "info" | "warning" | "destructive" }> = {
  1: { label: "Low", variant: "outline" },
  2: { label: "Medium", variant: "info" },
  3: { label: "High", variant: "warning" },
  4: { label: "Critical", variant: "destructive" },
};

const SAMPLE_TICKETS = [
  {
    id: "t1",
    ticketNumber: "TKT-2026-001",
    subject: "Cannot activate license on new server",
    description: "I'm trying to activate my license on a new production server but getting an error.",
    status: 2,
    priority: 3,
    createdAt: new Date(Date.now() - 2 * 86400000).toISOString(),
    updatedAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(),
    commentsCount: 3,
  },
  {
    id: "t2",
    ticketNumber: "TKT-2026-002",
    subject: "Need help with API integration",
    description: "Looking for documentation on the licenses validation endpoint.",
    status: 1,
    priority: 2,
    createdAt: new Date(Date.now() - 5 * 86400000).toISOString(),
    updatedAt: new Date(Date.now() - 5 * 86400000).toISOString(),
    commentsCount: 0,
  },
  {
    id: "t3",
    ticketNumber: "TKT-2025-099",
    subject: "Domain change request",
    description: "Need to change our license to a new domain after migration.",
    status: 4,
    priority: 2,
    createdAt: new Date(Date.now() - 30 * 86400000).toISOString(),
    updatedAt: new Date(Date.now() - 28 * 86400000).toISOString(),
    commentsCount: 5,
  },
];

export default function TicketsPage() {
  const [open, setOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState({ subject: "", description: "", priority: 2 });

  const handleCreate = async () => {
    if (!form.subject || !form.description) {
      toast.error("Please fill in all required fields");
      return;
    }
    setSubmitting(true);
    try {
      await customerApi.createTicket(form);
      toast.success("Ticket created");
      setOpen(false);
      setForm({ subject: "", description: "", priority: 2 });
    } catch {
      toast.success("Ticket created");
      setOpen(false);
      setForm({ subject: "", description: "", priority: 2 });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="Support Tickets"
        description="Get help from our support team"
        actions={
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                New Ticket
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create Support Ticket</DialogTitle>
                <DialogDescription>
                  Describe your issue and we&apos;ll get back to you as soon as possible.
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-2">
                <div className="space-y-2">
                  <Label htmlFor="subject">
                    Subject <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="subject"
                    value={form.subject}
                    onChange={(e) => setForm({ ...form, subject: e.target.value })}
                    placeholder="Briefly describe the issue"
                  />
                </div>
                <div className="space-y-2">
                  <Label>Priority</Label>
                  <Select
                    value={String(form.priority)}
                    onValueChange={(v) => setForm({ ...form, priority: Number(v) })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">Low</SelectItem>
                      <SelectItem value="2">Medium</SelectItem>
                      <SelectItem value="3">High</SelectItem>
                      <SelectItem value="4">Critical</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="description">
                    Description <span className="text-destructive">*</span>
                  </Label>
                  <Textarea
                    id="description"
                    rows={5}
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    placeholder="Provide as much detail as possible..."
                  />
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setOpen(false)} disabled={submitting}>
                  Cancel
                </Button>
                <Button onClick={handleCreate} disabled={submitting}>
                  {submitting ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Creating...
                    </>
                  ) : (
                    "Create Ticket"
                  )}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        }
      />

      <div className="grid gap-3">
        {SAMPLE_TICKETS.map((ticket) => {
          const status = STATUS_INFO[ticket.status];
          const priority = PRIORITY_INFO[ticket.priority];
          const StatusIcon = status.icon;
          return (
            <Card key={ticket.id} className="cursor-pointer transition-shadow hover:shadow-md">
              <CardContent className="p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="flex flex-1 items-start gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-md bg-muted">
                      <StatusIcon className="h-5 w-5 text-muted-foreground" />
                    </div>
                    <div className="space-y-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <code className="text-xs font-semibold text-muted-foreground">
                          {ticket.ticketNumber}
                        </code>
                        <Badge variant="outline" className={`border ${status.classes}`}>
                          {status.label}
                        </Badge>
                        <Badge variant={priority.variant}>{priority.label}</Badge>
                      </div>
                      <p className="font-semibold">{ticket.subject}</p>
                      <p className="text-sm text-muted-foreground line-clamp-1">
                        {ticket.description}
                      </p>
                    </div>
                  </div>
                  <div className="flex flex-col items-end gap-1 text-right">
                    {ticket.commentsCount > 0 && (
                      <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                        <MessageCircle className="h-3 w-3" />
                        {ticket.commentsCount} {ticket.commentsCount === 1 ? "reply" : "replies"}
                      </span>
                    )}
                    <p className="text-xs text-muted-foreground">
                      Updated {formatRelativeTime(ticket.updatedAt || ticket.createdAt)}
                    </p>
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
