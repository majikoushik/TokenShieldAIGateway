/* eslint-disable @typescript-eslint/no-unused-vars */
"use client";

import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { History, Filter, Clock, Coins, Layers, ShieldCheck, Eye, X, HelpCircle, ShieldAlert } from "lucide-react";
import { UsageLog, ClientApplication, AiModel, Provider } from "@/types";

export default function UsageLogsPage() {
  const [selectedLog, setSelectedLog] = useState<UsageLog | null>(null);

  // Filters State
  const [applicationId, setApplicationId] = useState("");
  const [selectedProvider, setSelectedProvider] = useState("");
  const [selectedModel, setSelectedModel] = useState("");
  const [selectedTier, setSelectedTier] = useState("");
  const [requestStatus, setRequestStatus] = useState("");

  // Queries
  const { data: logs, isLoading, error } = useQuery<UsageLog[]>({
    queryKey: ["usageLogs", { applicationId, selectedProvider, selectedModel, selectedTier, requestStatus }],
    queryFn: () => api.getUsageLogs({
      applicationId,
      selectedProvider,
      selectedModel,
      selectedTier,
      requestStatus
    })
  });

  const { data: applications } = useQuery<ClientApplication[]>({
    queryKey: ["applications"],
    queryFn: () => api.getApplications()
  });

  const { data: models } = useQuery<AiModel[]>({
    queryKey: ["models"],
    queryFn: () => api.getModels()
  });

  const { data: providers } = useQuery<Provider[]>({
    queryKey: ["providers"],
    queryFn: () => api.getProviders()
  });

  const handleResetFilters = () => {
    setApplicationId("");
    setSelectedProvider("");
    setSelectedModel("");
    setSelectedTier("");
    setRequestStatus("");
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
        <h1 className="text-2xl font-bold tracking-tight">Proxy Request Logs</h1>
        <p className="text-sm text-muted-foreground">Privacy-preserving audit trail of chat completions routed through the gateway.</p>
      </div>

      {/* Filter Row card */}
      <div className="bg-card border border-border rounded-lg p-5">
        <div className="flex items-center gap-2 mb-4 text-xs font-bold uppercase tracking-wider text-muted-foreground">
          <Filter className="h-4 w-4 text-primary" />
          Filter Requests
        </div>
        
        <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-5 text-xs">
          {/* Application */}
          <div className="space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">Client Application</label>
            <select
              value={applicationId}
              onChange={(e) => setApplicationId(e.target.value)}
              className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-foreground focus:outline-none focus:border-primary cursor-pointer"
            >
              <option value="">All Applications</option>
              {applications?.map((app) => (
                <option key={app.id} value={app.id}>{app.name}</option>
              ))}
            </select>
          </div>

          {/* Provider */}
          <div className="space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">Model Provider</label>
            <select
              value={selectedProvider}
              onChange={(e) => setSelectedProvider(e.target.value)}
              className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-foreground focus:outline-none focus:border-primary cursor-pointer"
            >
              <option value="">All Providers</option>
              {providers?.map((p) => (
                <option key={p.id} value={p.name}>{p.name}</option>
              ))}
            </select>
          </div>

          {/* Model */}
          <div className="space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">AI Model</label>
            <select
              value={selectedModel}
              onChange={(e) => setSelectedModel(e.target.value)}
              className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-foreground focus:outline-none focus:border-primary cursor-pointer"
            >
              <option value="">All Models</option>
              {models?.map((m) => (
                <option key={m.id} value={m.name}>{m.name}</option>
              ))}
            </select>
          </div>

          {/* Tier */}
          <div className="space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">Model Cost Tier</label>
            <select
              value={selectedTier}
              onChange={(e) => setSelectedTier(e.target.value)}
              className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-foreground focus:outline-none focus:border-primary cursor-pointer"
            >
              <option value="">All Tiers</option>
              <option value="cheap">Cheap</option>
              <option value="standard">Standard</option>
              <option value="premium">Premium</option>
            </select>
          </div>

          {/* Request Status */}
          <div className="space-y-1.5">
            <label className="font-semibold text-muted-foreground uppercase tracking-wide">Gateway Status</label>
            <select
              value={requestStatus}
              onChange={(e) => setRequestStatus(e.target.value)}
              className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-foreground focus:outline-none focus:border-primary cursor-pointer"
            >
              <option value="">All Statuses</option>
              <option value="Success">Success</option>
              <option value="Failed">Failed</option>
              <option value="Blocked">Blocked</option>
            </select>
          </div>
        </div>

        <div className="flex justify-end gap-3 mt-4 pt-3 border-t border-border/40">
          <button
            onClick={handleResetFilters}
            className="px-3.5 py-1.5 bg-secondary text-foreground border border-border hover:bg-secondary/80 rounded text-xs font-semibold cursor-pointer"
          >
            Clear Filters
          </button>
        </div>
      </div>

      {/* Logs Table Card */}
      <div className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-secondary/40 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground border-b border-border">
                <th className="py-3 px-6">Timestamp</th>
                <th className="py-3 px-6">Client App</th>
                <th className="py-3 px-6">Routed Model</th>
                <th className="py-3 px-6">Tokens</th>
                <th className="py-3 px-6">Cost (USD)</th>
                <th className="py-3 px-6">Latency</th>
                <th className="py-3 px-6">Status</th>
                <th className="py-3 px-6 text-right">Inspect</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-xs">
              {logs?.map((log) => {
                const isSuccess = log.requestStatus === "Success";
                const isBlocked = log.requestStatus === "Blocked";
                const statusColor = isSuccess 
                  ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20" 
                  : isBlocked
                    ? "bg-red-500/10 text-red-400 border-red-500/20"
                    : "bg-amber-500/10 text-amber-400 border-amber-500/20";
                
                const tierColor = log.selectedTier === "cheap" 
                  ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20" 
                  : log.selectedTier === "premium" 
                    ? "bg-purple-500/10 text-purple-400 border-purple-500/20" 
                    : "bg-blue-500/10 text-blue-400 border-blue-500/20";

                return (
                  <tr key={log.id} className="hover:bg-secondary/10 transition-colors">
                    <td className="py-3.5 px-6 font-mono text-muted-foreground">
                      {new Date(log.createdAtUtc).toLocaleString()}
                    </td>
                    <td className="py-3.5 px-6 font-semibold">{log.applicationName}</td>
                    <td className="py-3.5 px-6">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-foreground">{log.selectedModel}</span>
                        <span className={`text-[9px] px-1.5 py-0.5 rounded font-semibold border uppercase tracking-wider ${tierColor}`}>
                          {log.selectedTier}
                        </span>
                      </div>
                    </td>
                    <td className="py-3.5 px-6 font-mono text-muted-foreground">
                      {log.inputTokens + log.outputTokens}
                      <span className="text-[10px] text-muted-foreground/60 ml-1">({log.inputTokens} / {log.outputTokens})</span>
                    </td>
                    <td className="py-3.5 px-6 font-mono font-bold text-foreground">
                      ${log.estimatedCost.toLocaleString(undefined, { minimumFractionDigits: 5, maximumFractionDigits: 5 })}
                    </td>
                    <td className="py-3.5 px-6 font-mono text-muted-foreground">{log.latencyMs}ms</td>
                    <td className="py-3.5 px-6">
                      <span className={`px-2 py-0.5 rounded-full border text-[10px] font-semibold uppercase tracking-wider ${statusColor}`}>
                        {log.requestStatus}
                      </span>
                    </td>
                    <td className="py-3.5 px-6 text-right">
                      <button
                        onClick={() => setSelectedLog(log)}
                        className="p-1 hover:bg-secondary rounded text-primary transition-colors cursor-pointer"
                        title="Inspect Request Metadata"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                );
              })}
              {!logs?.length && (
                <tr>
                  <td colSpan={8} className="text-center py-12 text-muted-foreground">
                    <History className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                    <h3 className="font-bold text-foreground">No logs found</h3>
                    <p className="text-sm text-muted-foreground mt-1">Adjust filters or call completions through the API gateway proxy.</p>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Inspect Side Drawer Panel / Modal */}
      {selectedLog && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-end z-50">
          <div className="bg-card border-l border-border h-full max-w-2xl w-full flex flex-col shadow-2xl animate-in slide-in-from-right duration-250">
            {/* Drawer Header */}
            <div className="px-6 py-5 border-b border-border flex items-center justify-between">
              <div className="flex items-center gap-2">
                <History className="h-5 w-5 text-primary" />
                <div>
                  <h3 className="text-base font-bold">Proxy Completion Metadata</h3>
                  <p className="text-[11px] font-mono text-muted-foreground mt-0.5">Correlation ID: {selectedLog.correlationId}</p>
                </div>
              </div>
              <button
                onClick={() => setSelectedLog(null)}
                className="p-1.5 hover:bg-secondary rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            {/* Drawer Content */}
            <div className="flex-1 overflow-y-auto p-6 space-y-6">
              
              {/* Privacy Warning Banner */}
              <div className="p-3 bg-secondary/80 border border-border rounded flex gap-2.5 items-start">
                <HelpCircle className="h-4 w-4 text-primary shrink-0 mt-0.5" />
                <div className="space-y-0.5">
                  <h4 className="text-[11px] font-bold text-foreground uppercase tracking-wider">Privacy By Default</h4>
                  <p className="text-[11px] text-muted-foreground leading-relaxed">
                    To comply with enterprise auditing guidelines, TokenShield never persists clear-text chat completion request prompts or response payloads. Only semantic signatures (SHA-256 hashes) are logged.
                  </p>
                </div>
              </div>

              {/* Request hashes */}
              <div className="space-y-3">
                <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Message Signatures</h4>
                <div className="grid gap-3 sm:grid-cols-2 text-xs">
                  <div className="bg-secondary/40 border border-border rounded p-3 space-y-1">
                    <span className="font-semibold text-muted-foreground">Prompt Payload Hash</span>
                    <p className="font-mono text-foreground font-medium select-all break-all">{selectedLog.promptHash}</p>
                  </div>
                  <div className="bg-secondary/40 border border-border rounded p-3 space-y-1">
                    <span className="font-semibold text-muted-foreground">Response Payload Hash</span>
                    <p className="font-mono text-foreground font-medium select-all break-all">{selectedLog.responseHash}</p>
                  </div>
                </div>
              </div>

              {/* Gateway decisioning metadata */}
              <div className="space-y-3">
                <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Routing Governance</h4>
                <div className="bg-card border border-border rounded-lg overflow-hidden divide-y divide-border text-xs">
                  
                  {/* Selected Provider */}
                  <div className="p-3.5 flex justify-between items-center">
                    <span className="text-muted-foreground">Model Provider</span>
                    <span className="font-semibold">{selectedLog.selectedProvider}</span>
                  </div>

                  {/* Selected Model */}
                  <div className="p-3.5 flex justify-between items-center">
                    <span className="text-muted-foreground">AI Model</span>
                    <span className="font-semibold font-mono">{selectedLog.selectedModel}</span>
                  </div>

                  {/* Selected Cost Tier */}
                  <div className="p-3.5 flex justify-between items-center">
                    <span className="text-muted-foreground">Routed Tier</span>
                    <span className="font-semibold uppercase text-primary font-mono">{selectedLog.selectedTier}</span>
                  </div>

                  {/* Matched Routing Rule */}
                  <div className="p-3.5 flex justify-between items-center">
                    <span className="text-muted-foreground">Matched Policy Rule</span>
                    <span className="font-semibold text-foreground">
                      {selectedLog.matchedRuleName || (
                        <span className="text-muted-foreground italic font-normal">Default Routing (None Matched)</span>
                      )}
                    </span>
                  </div>

                  {/* Fallback status */}
                  <div className="p-3.5 flex justify-between items-center">
                    <span className="text-muted-foreground">Polly Resilience Fallback</span>
                    <span className={`px-2 py-0.5 rounded font-semibold text-[10px] uppercase border ${
                      selectedLog.fallbackUsed
                        ? "bg-amber-500/10 text-amber-400 border-amber-500/20"
                        : "bg-emerald-500/10 text-emerald-400 border-emerald-500/20"
                    }`}>
                      {selectedLog.fallbackUsed ? "Fallback Activated" : "Standard Route"}
                    </span>
                  </div>

                  {/* Budget state */}
                  <div className="p-3.5 flex justify-between items-center">
                    <span className="text-muted-foreground">Pre-Call Budget Status</span>
                    <span className={`px-2 py-0.5 rounded font-semibold text-[10px] uppercase border ${
                      selectedLog.budgetStatus === "Blocked"
                        ? "bg-red-500/10 text-red-400 border-red-500/20"
                        : selectedLog.budgetStatus === "Warning"
                          ? "bg-amber-500/10 text-amber-400 border-amber-500/20"
                          : "bg-emerald-500/10 text-emerald-400 border-emerald-500/20"
                    }`}>
                      {selectedLog.budgetStatus}
                    </span>
                  </div>

                </div>
              </div>

              {/* Performance / Usage Stats */}
              <div className="space-y-3">
                <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Proxy Execution Stats</h4>
                <div className="grid gap-4 grid-cols-3 text-xs">
                  
                  {/* Tokens card */}
                  <div className="bg-secondary/30 border border-border p-4 rounded-lg flex flex-col gap-1 items-center justify-center text-center">
                    <Layers className="h-5 w-5 text-amber-500" />
                    <span className="text-[10px] text-muted-foreground uppercase font-semibold mt-1">Total Tokens</span>
                    <span className="text-base font-bold font-mono mt-0.5">{selectedLog.inputTokens + selectedLog.outputTokens}</span>
                    <span className="text-[9px] text-muted-foreground/60 font-mono">({selectedLog.inputTokens} In / {selectedLog.outputTokens} Out)</span>
                  </div>

                  {/* Latency card */}
                  <div className="bg-secondary/30 border border-border p-4 rounded-lg flex flex-col gap-1 items-center justify-center text-center">
                    <Clock className="h-5 w-5 text-purple-500" />
                    <span className="text-[10px] text-muted-foreground uppercase font-semibold mt-1">Latency</span>
                    <span className="text-base font-bold font-mono mt-0.5">{selectedLog.latencyMs} ms</span>
                    <span className="text-[9px] text-muted-foreground/60">Proxy end-to-end</span>
                  </div>

                  {/* Cost card */}
                  <div className="bg-secondary/30 border border-border p-4 rounded-lg flex flex-col gap-1 items-center justify-center text-center">
                    <Coins className="h-5 w-5 text-emerald-500" />
                    <span className="text-[10px] text-muted-foreground uppercase font-semibold mt-1">Calculated Cost</span>
                    <span className="text-base font-bold font-mono mt-0.5">${selectedLog.estimatedCost.toLocaleString(undefined, { minimumFractionDigits: 5 })}</span>
                    <span className="text-[9px] text-muted-foreground/60">Decimal precision</span>
                  </div>

                </div>
              </div>

            </div>
          </div>
        </div>
      )}
    </div>
  );
}
