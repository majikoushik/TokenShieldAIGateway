/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import {
  Coins,
  History,
  Clock,
  Activity,
  Layers,
  ArrowRight,
  TrendingUp,
  Cpu
} from "lucide-react";
import Link from "next/link";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend
} from "recharts";
import { UsageLog } from "@/types";

const COLORS = ["#10b981", "#3b82f6", "#8b5cf6", "#f59e0b", "#ef4444"];

export default function DashboardPage() {
  const { data: summary, isLoading, error } = useQuery<any>({
    queryKey: ["usageSummary"],
    queryFn: () => api.getUsageSummary()
  });

  const { data: recentLogs } = useQuery<UsageLog[]>({
    queryKey: ["recentLogs"],
    queryFn: () => api.getUsageLogs({ pageSize: 5 })
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error || !summary) {
    return (
      <div className="p-6 border border-red-500/20 bg-red-500/5 rounded-lg text-red-500">
        <h3 className="font-bold">Error loading dashboard summary</h3>
        <p className="text-sm text-red-400 mt-1">{error?.message || "Failed to load summary analytics."}</p>
      </div>
    );
  }

  // Format charts data
  const providerData = summary.costByProvider?.map((x: any) => ({ name: x.groupKey || x.GroupKey, value: Number((x.cost ?? x.Cost ?? 0).toFixed(4)) })) || [];
  const modelData = summary.costByModel?.map((x: any) => ({ name: x.groupKey || x.GroupKey, value: Number((x.cost ?? x.Cost ?? 0).toFixed(4)) })) || [];
  const statusData = summary.requestByStatus?.map((x: any) => ({ name: x.groupKey || x.GroupKey, value: x.count ?? x.Count ?? 0 })) || [];
  const budgetData = summary.requestByBudgetState?.map((x: any) => ({ name: x.groupKey || x.GroupKey, value: x.count ?? x.Count ?? 0 })) || [];


  return (
    <div className="space-y-8">
      {/* Top Banner */}
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold tracking-tight">FinOps Spending Dashboard</h1>
        <p className="text-sm text-muted-foreground">Real-time spend tracking, provider metrics, and gateway health logs.</p>
      </div>

      {/* KPI Cards Grid */}
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {/* Cost Card */}
        <div className="bg-card border border-border p-6 rounded-lg flex items-center justify-between">
          <div className="space-y-1">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Estimated Monthly Cost</p>
            <h3 className="text-2xl font-bold tracking-tight">${summary.totalCost.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</h3>
            <p className="text-[10px] text-emerald-500 flex items-center gap-1">
              <TrendingUp className="h-3 w-3" />
              Within overall threshold
            </p>
          </div>
          <div className="p-3 bg-emerald-500/10 rounded-lg text-emerald-500">
            <Coins className="h-5 w-5" />
          </div>
        </div>

        {/* Requests Card */}
        <div className="bg-card border border-border p-6 rounded-lg flex items-center justify-between">
          <div className="space-y-1">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Total Proxy Requests</p>
            <h3 className="text-2xl font-bold tracking-tight">{summary.totalRequests.toLocaleString()}</h3>
            <p className="text-[10px] text-muted-foreground">Interventions active</p>
          </div>
          <div className="p-3 bg-blue-500/10 rounded-lg text-blue-500">
            <Activity className="h-5 w-5" />
          </div>
        </div>

        {/* Latency Card */}
        <div className="bg-card border border-border p-6 rounded-lg flex items-center justify-between">
          <div className="space-y-1">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Avg Gateway Latency</p>
            <h3 className="text-2xl font-bold tracking-tight">{summary.averageLatencyMs.toFixed(1)} ms</h3>
            <p className="text-[10px] text-muted-foreground">Includes model adapter overhead</p>
          </div>
          <div className="p-3 bg-purple-500/10 rounded-lg text-purple-500">
            <Clock className="h-5 w-5" />
          </div>
        </div>

        {/* Tokens Card */}
        <div className="bg-card border border-border p-6 rounded-lg flex items-center justify-between">
          <div className="space-y-1">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Estimated Total Tokens</p>
            <h3 className="text-2xl font-bold tracking-tight">{(summary.totalInputTokens + summary.totalOutputTokens).toLocaleString()}</h3>
            <p className="text-[10px] text-muted-foreground">
              Input: {summary.totalInputTokens.toLocaleString()} | Output: {summary.totalOutputTokens.toLocaleString()}
            </p>
          </div>
          <div className="p-3 bg-amber-500/10 rounded-lg text-amber-500">
            <Layers className="h-5 w-5" />
          </div>
        </div>
      </div>

      {/* Analytics Charts Grid */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Spend by Provider */}
        <div className="bg-card border border-border p-6 rounded-lg space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold uppercase tracking-wider text-muted-foreground">Spend by Model Provider</h3>
            <span className="text-xs font-medium bg-secondary px-2.5 py-0.5 rounded-full border border-border">USD ($)</span>
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={providerData} layout="vertical">
                <XAxis type="number" />
                <YAxis dataKey="name" type="category" width={100} tick={{ fontSize: 11 }} />
                <Tooltip formatter={(value) => [`$${value}`, "Cost"]} />
                <Bar dataKey="value" fill="#3b82f6" radius={[0, 4, 4, 0]}>
                  {providerData.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Spend by Model */}
        <div className="bg-card border border-border p-6 rounded-lg space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold uppercase tracking-wider text-muted-foreground">Top Models by Spend</h3>
            <span className="text-xs font-medium bg-secondary px-2.5 py-0.5 rounded-full border border-border">USD ($)</span>
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={modelData}>
                <XAxis dataKey="name" tick={{ fontSize: 10 }} />
                <YAxis />
                <Tooltip formatter={(value) => [`$${value}`, "Cost"]} />
                <Bar dataKey="value" fill="#10b981" radius={[4, 4, 0, 0]}>
                  {modelData.map((entry: any, index: number) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Request Status Breakdown */}
        <div className="bg-card border border-border p-6 rounded-lg flex flex-col justify-between">
          <h3 className="text-sm font-bold uppercase tracking-wider text-muted-foreground mb-4">Requests Status Rate</h3>
          <div className="flex flex-col sm:flex-row items-center justify-around gap-4 h-64">
            <div className="w-44 h-44 shrink-0">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={statusData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={70}
                    paddingAngle={3}
                    dataKey="value"
                  >
                    {statusData.map((entry: any, index: number) => (
                      <Cell key={`cell-${index}`} fill={entry.name === "Success" ? "#10b981" : entry.name === "Blocked" ? "#ef4444" : "#f59e0b"} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="space-y-3 shrink-0">
              {statusData.map((entry: any, index: number) => {
                const color = entry.name === "Success" ? "bg-emerald-500" : entry.name === "Blocked" ? "bg-red-500" : "bg-amber-500";
                return (
                  <div key={entry.name} className="flex items-center gap-3">
                    <span className={`h-3 w-3 rounded-full ${color}`}></span>
                    <span className="text-xs font-medium">{entry.name}:</span>
                    <span className="text-xs font-bold text-muted-foreground">{entry.value.toLocaleString()} ({((entry.value / summary.totalRequests) * 100).toFixed(2)}%)</span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* Requests by Budget Status */}
        <div className="bg-card border border-border p-6 rounded-lg flex flex-col justify-between">
          <h3 className="text-sm font-bold uppercase tracking-wider text-muted-foreground mb-4">Requests by Budget State</h3>
          <div className="flex flex-col sm:flex-row items-center justify-around gap-4 h-64">
            <div className="w-44 h-44 shrink-0">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={budgetData}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={70}
                    paddingAngle={3}
                    dataKey="value"
                  >
                    {budgetData.map((entry: any, index: number) => (
                      <Cell key={`cell-${index}`} fill={entry.name === "Within Limits" ? "#10b981" : entry.name === "Warning" ? "#f59e0b" : "#ef4444"} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="space-y-3 shrink-0">
              {budgetData.map((entry: any, index: number) => {
                const color = entry.name === "Within Limits" ? "bg-emerald-500" : entry.name === "Warning" ? "bg-amber-500" : "bg-red-500";
                return (
                  <div key={entry.name} className="flex items-center gap-3">
                    <span className={`h-3 w-3 rounded-full ${color}`}></span>
                    <span className="text-xs font-medium">{entry.name}:</span>
                    <span className="text-xs font-bold text-muted-foreground">{entry.value.toLocaleString()}</span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>

      {/* Recent Requests Section */}
      <section className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="p-6 border-b border-border flex items-center justify-between">
          <h3 className="text-sm font-bold uppercase tracking-wider text-foreground flex items-center gap-2">
            <History className="h-4 w-4 text-primary" />
            Recent Routed Requests Logs
          </h3>
          <Link href="/usage-logs" className="inline-flex items-center gap-1.5 text-xs text-primary hover:underline">
            View All Logs
            <ArrowRight className="h-3 w-3" />
          </Link>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-secondary/40 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground border-b border-border">
                <th className="py-3 px-6">Timestamp</th>
                <th className="py-3 px-6">Application</th>
                <th className="py-3 px-6">Model / Tier</th>
                <th className="py-3 px-6">Tokens</th>
                <th className="py-3 px-6">Cost</th>
                <th className="py-3 px-6">Latency</th>
                <th className="py-3 px-6">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-xs">
              {recentLogs?.map((log) => {
                const statusColor = log.requestStatus === "Success" ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20" : "bg-red-500/10 text-red-400 border-red-500/20";
                const tierColor = log.selectedTier === "cheap" ? "bg-emerald-500/10 text-emerald-400" : log.selectedTier === "premium" ? "bg-purple-500/10 text-purple-400" : "bg-blue-500/10 text-blue-400";
                return (
                  <tr key={log.id} className="hover:bg-secondary/20 transition-colors">
                    <td className="py-3.5 px-6 font-mono text-muted-foreground">{new Date(log.createdAtUtc).toLocaleTimeString()}</td>
                    <td className="py-3.5 px-6 font-semibold">{log.applicationName}</td>
                    <td className="py-3.5 px-6">
                      <div className="flex items-center gap-2">
                        <span>{log.selectedModel}</span>
                        <span className={`text-[10px] px-1.5 py-0.5 rounded font-medium ${tierColor}`}>
                          {log.selectedTier}
                        </span>
                      </div>
                    </td>
                    <td className="py-3.5 px-6 font-mono">{log.inputTokens + log.outputTokens}</td>
                    <td className="py-3.5 px-6 font-mono font-semibold">${log.estimatedCost.toFixed(5)}</td>
                    <td className="py-3.5 px-6 font-mono text-muted-foreground">{log.latencyMs}ms</td>
                    <td className="py-3.5 px-6">
                      <span className={`px-2 py-0.5 rounded-full border text-[10px] font-semibold uppercase tracking-wider ${statusColor}`}>
                        {log.requestStatus}
                      </span>
                    </td>
                  </tr>
                );
              })}
              {!recentLogs?.length && (
                <tr>
                  <td colSpan={7} className="text-center py-6 text-muted-foreground">No recent proxy completion requests processed.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
