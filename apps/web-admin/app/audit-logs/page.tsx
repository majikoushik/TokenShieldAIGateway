/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { ShieldCheck, Search, ChevronDown, ChevronUp, User, Database, Code, Info } from "lucide-react";
import { AuditLog } from "@/types";

export default function AuditLogsPage() {
  const [expandedRowId, setExpandedRowId] = useState<string | null>(null);

  // Filters State
  const [actionName, setActionName] = useState("");
  const [actorEmail, setActorEmail] = useState("");

  // Queries
  const { data: logs, isLoading, error } = useQuery<AuditLog[]>({
    queryKey: ["auditLogs", { actionName, actorEmail }],
    queryFn: () => api.getAuditLogs({
      actionName: actionName || undefined,
      actorEmail: actorEmail || undefined
    })
  });

  const toggleRow = (id: string) => {
    if (expandedRowId === id) {
      setExpandedRowId(null);
    } else {
      setExpandedRowId(id);
    }
  };

  const formatJson = (jsonStr: string) => {
    try {
      const parsed = JSON.parse(jsonStr);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return jsonStr;
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header section */}
      <div className="space-y-1">
        <h1 className="text-2xl font-bold tracking-tight">System Audit Logs</h1>
        <p className="text-sm text-muted-foreground">Traceability logs recording configuration updates, policy mutations, and API key generations.</p>
      </div>

      {/* Filter Card */}
      <div className="bg-card border border-border rounded-lg p-5">
        <div className="flex flex-col sm:flex-row gap-4 text-xs">
          {/* Action filter */}
          <div className="flex-1 space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">Mutation Action</label>
            <div className="relative flex items-center">
              <Search className="absolute left-3 h-3.5 w-3.5 text-muted-foreground" />
              <input
                type="text"
                placeholder="e.g. CreateProvider, RevokeApiKey"
                value={actionName}
                onChange={(e) => setActionName(e.target.value)}
                className="w-full bg-secondary border border-border pl-9 pr-3 py-1.5 rounded text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
              />
            </div>
          </div>

          {/* Actor filter */}
          <div className="flex-1 space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">Actor Email</label>
            <div className="relative flex items-center">
              <User className="absolute left-3 h-3.5 w-3.5 text-muted-foreground" />
              <input
                type="text"
                placeholder="e.g. admin@acme.com"
                value={actorEmail}
                onChange={(e) => setActorEmail(e.target.value)}
                className="w-full bg-secondary border border-border pl-9 pr-3 py-1.5 rounded text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Table section */}
      <div className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-secondary/40 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground border-b border-border">
                <th className="py-3 px-6 w-8"></th>
                <th className="py-3 px-6">Timestamp (UTC)</th>
                <th className="py-3 px-6">Actor</th>
                <th className="py-3 px-6">Action</th>
                <th className="py-3 px-6">Entity Target</th>
                <th className="py-3 px-6">Entity ID Reference</th>
              </tr>
            </thead>
            <tbody className="text-xs">
              {logs?.map((log) => {
                const isExpanded = expandedRowId === log.id;
                
                // Color mapping for common actions
                let actionBadge = "bg-secondary text-foreground border-border";
                if (log.actionName.startsWith("Create")) {
                  actionBadge = "bg-emerald-500/10 text-emerald-400 border-emerald-500/20";
                } else if (log.actionName.startsWith("Update") || log.actionName.startsWith("Edit")) {
                  actionBadge = "bg-blue-500/10 text-blue-400 border-blue-500/20";
                } else if (log.actionName.startsWith("Delete") || log.actionName.startsWith("Revoke")) {
                  actionBadge = "bg-red-500/10 text-red-400 border-red-500/20";
                }

                return (
                  <React.Fragment key={log.id}>
                    {/* Primary Row */}
                    <tr 
                      onClick={() => toggleRow(log.id)}
                      className="hover:bg-secondary/10 transition-colors border-b border-border/60 cursor-pointer"
                    >
                      <td className="py-3.5 px-6">
                        {isExpanded ? <ChevronUp className="h-4 w-4 text-muted-foreground" /> : <ChevronDown className="h-4 w-4 text-muted-foreground" />}
                      </td>
                      <td className="py-3.5 px-6 font-mono text-muted-foreground">
                        {new Date(log.createdAtUtc).toLocaleString()}
                      </td>
                      <td className="py-3.5 px-6 font-semibold">{log.actorEmail}</td>
                      <td className="py-3.5 px-6">
                        <span className={`px-2 py-0.5 rounded border text-[10px] font-semibold uppercase tracking-wider ${actionBadge}`}>
                          {log.actionName}
                        </span>
                      </td>
                      <td className="py-3.5 px-6">
                        <span className="inline-flex items-center gap-1.5 text-muted-foreground">
                          <Database className="h-3.5 w-3.5 text-primary" />
                          {log.entityName}
                        </span>
                      </td>
                      <td className="py-3.5 px-6 font-mono text-muted-foreground/60">{log.entityId}</td>
                    </tr>

                    {/* Expandable Row */}
                    {isExpanded && (
                      <tr className="bg-secondary/20">
                        <td colSpan={6} className="px-6 py-4 border-b border-border/80">
                          <div className="space-y-3">
                            <div className="flex items-center gap-1 text-[10px] uppercase font-bold tracking-wider text-muted-foreground">
                              <Code className="h-3.5 w-3.5 text-primary" />
                              Mutation Diff Details (JSON)
                            </div>
                            <pre className="font-mono text-xs bg-secondary/80 border border-border p-4 rounded-lg overflow-x-auto text-foreground leading-relaxed max-h-80 select-all">
                              {formatJson(log.detailsJson)}
                            </pre>
                          </div>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                );
              })}
              {!logs?.length && (
                <tr>
                  <td colSpan={6} className="text-center py-12 text-muted-foreground">
                    <ShieldCheck className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                    <h3 className="font-bold text-foreground">No audit entries found</h3>
                    <p className="text-sm text-muted-foreground mt-1">Actions mutating gateway configurations populate this panel.</p>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// React.Fragment helper for compiling NextJS bundle safely
import React from "react";
