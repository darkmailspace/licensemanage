"use client";

import {
  Activity,
  AlertTriangle,
  BarChart3,
  Calendar,
  DollarSign,
  Download,
  FileText,
  KeyRound,
  TrendingUp,
  Users,
} from "lucide-react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { formatCurrency, formatNumber } from "@/lib/utils";

const REVENUE_DATA = Array.from({ length: 12 }).map((_, i) => ({
  month: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"][i],
  revenue: Math.floor(20000 + Math.random() * 30000),
  newSales: Math.floor(15 + Math.random() * 30),
  renewals: Math.floor(10 + Math.random() * 20),
}));

const LICENSE_DATA = Array.from({ length: 12 }).map((_, i) => ({
  month: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"][i],
  active: Math.floor(800 + i * 40 + Math.random() * 50),
  expired: Math.floor(50 + Math.random() * 30),
  revoked: Math.floor(5 + Math.random() * 10),
}));

const REPORT_CARDS = [
  {
    title: "Revenue Report",
    description: "Total earnings, sales trends, MRR",
    icon: DollarSign,
    color: "bg-green-100 text-green-600 dark:bg-green-900/30",
  },
  {
    title: "License Report",
    description: "All licenses by status and type",
    icon: KeyRound,
    color: "bg-blue-100 text-blue-600 dark:bg-blue-900/30",
  },
  {
    title: "Expiry Report",
    description: "Upcoming and recent expirations",
    icon: AlertTriangle,
    color: "bg-orange-100 text-orange-600 dark:bg-orange-900/30",
  },
  {
    title: "Activation Report",
    description: "Activation success, failure rates",
    icon: Activity,
    color: "bg-purple-100 text-purple-600 dark:bg-purple-900/30",
  },
  {
    title: "Customer Report",
    description: "Customer growth, demographics",
    icon: Users,
    color: "bg-cyan-100 text-cyan-600 dark:bg-cyan-900/30",
  },
  {
    title: "Performance Report",
    description: "API performance, validation times",
    icon: TrendingUp,
    color: "bg-pink-100 text-pink-600 dark:bg-pink-900/30",
  },
];

export default function ReportsPage() {
  return (
    <div>
      <PageHeader
        title="Reports & Analytics"
        description="Comprehensive insights into your license business"
        actions={
          <>
            <Select defaultValue="last30days">
              <SelectTrigger className="w-[180px]">
                <Calendar className="mr-2 h-4 w-4" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="today">Today</SelectItem>
                <SelectItem value="last7days">Last 7 days</SelectItem>
                <SelectItem value="last30days">Last 30 days</SelectItem>
                <SelectItem value="last90days">Last 90 days</SelectItem>
                <SelectItem value="thisYear">This year</SelectItem>
                <SelectItem value="custom">Custom range</SelectItem>
              </SelectContent>
            </Select>
            <Button variant="outline">
              <Download className="mr-2 h-4 w-4" />
              Export
            </Button>
          </>
        }
      />

      {/* Quick Stats */}
      <div className="mb-6 grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Total Revenue</p>
            <p className="mt-2 text-3xl font-bold">{formatCurrency(348290)}</p>
            <p className="mt-1 text-xs text-green-600">+15.3% from last month</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Total Licenses</p>
            <p className="mt-2 text-3xl font-bold">{formatNumber(1284)}</p>
            <p className="mt-1 text-xs text-green-600">+12.5% from last month</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Avg License Value</p>
            <p className="mt-2 text-3xl font-bold">$271</p>
            <p className="mt-1 text-xs text-green-600">+2.5% from last month</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground">Renewal Rate</p>
            <p className="mt-2 text-3xl font-bold">87.3%</p>
            <p className="mt-1 text-xs text-green-600">+1.8% from last month</p>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="revenue">Revenue</TabsTrigger>
          <TabsTrigger value="licenses">Licenses</TabsTrigger>
          <TabsTrigger value="reports">All Reports</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Revenue Trend</CardTitle>
                <CardDescription>Monthly revenue over the past year</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={REVENUE_DATA}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis dataKey="month" className="text-xs" />
                    <YAxis className="text-xs" tickFormatter={(v) => `$${v / 1000}k`} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "hsl(var(--background))",
                        border: "1px solid hsl(var(--border))",
                        borderRadius: "0.375rem",
                      }}
                      formatter={(value: number) => formatCurrency(value)}
                    />
                    <Line
                      type="monotone"
                      dataKey="revenue"
                      stroke="#3b82f6"
                      strokeWidth={2}
                      dot={{ fill: "#3b82f6" }}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>License Trends</CardTitle>
                <CardDescription>Active vs expired licenses</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={LICENSE_DATA}>
                    <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                    <XAxis dataKey="month" className="text-xs" />
                    <YAxis className="text-xs" />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "hsl(var(--background))",
                        border: "1px solid hsl(var(--border))",
                        borderRadius: "0.375rem",
                      }}
                    />
                    <Bar dataKey="active" stackId="a" fill="#10b981" />
                    <Bar dataKey="expired" stackId="a" fill="#f59e0b" />
                    <Bar dataKey="revoked" stackId="a" fill="#ef4444" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="revenue">
          <Card>
            <CardHeader>
              <CardTitle>Revenue Analytics</CardTitle>
              <CardDescription>Detailed revenue breakdown</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <BarChart data={REVENUE_DATA}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="month" className="text-xs" />
                  <YAxis className="text-xs" />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "hsl(var(--background))",
                      border: "1px solid hsl(var(--border))",
                      borderRadius: "0.375rem",
                    }}
                  />
                  <Bar dataKey="newSales" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="renewals" fill="#10b981" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="licenses">
          <Card>
            <CardHeader>
              <CardTitle>License Statistics</CardTitle>
              <CardDescription>Detailed license metrics</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <LineChart data={LICENSE_DATA}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="month" className="text-xs" />
                  <YAxis className="text-xs" />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "hsl(var(--background))",
                      border: "1px solid hsl(var(--border))",
                      borderRadius: "0.375rem",
                    }}
                  />
                  <Line type="monotone" dataKey="active" stroke="#10b981" strokeWidth={2} />
                  <Line type="monotone" dataKey="expired" stroke="#f59e0b" strokeWidth={2} />
                  <Line type="monotone" dataKey="revoked" stroke="#ef4444" strokeWidth={2} />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="reports">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {REPORT_CARDS.map((report) => (
              <Card
                key={report.title}
                className="cursor-pointer transition-shadow hover:shadow-md"
              >
                <CardContent className="p-6">
                  <div
                    className={`mb-4 flex h-12 w-12 items-center justify-center rounded-lg ${report.color}`}
                  >
                    <report.icon className="h-6 w-6" />
                  </div>
                  <h3 className="font-semibold">{report.title}</h3>
                  <p className="mt-1 text-sm text-muted-foreground">{report.description}</p>
                  <Button variant="outline" size="sm" className="mt-4 w-full">
                    <FileText className="mr-2 h-4 w-4" />
                    Generate Report
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
