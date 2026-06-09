/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { z } from "zod";
import { Key, Plus, Trash2, CheckCircle2, XCircle, AlertCircle, Clipboard, Copy, Info, Calendar, Ban } from "lucide-react";
import { ApiKey, ClientApplication } from "@/types";

const apiKeySchema = z.object({
  clientApplicationId: z.string().min(1, "Client Application is required"),
  name: z.string().min(1, "Key name is required").max(100, "Name must be less than 100 characters"),
  prefix: z.string().min(1, "Key prefix environment is required"),
  expiresAtUtc: z.string().optional()
});

type ApiKeyFormData = z.infer<typeof apiKeySchema>;

export default function ApiKeysPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Form States
  const [clientApplicationId, setClientApplicationId] = useState("");
  const [name, setName] = useState("");
  const [prefix, setPrefix] = useState("ts_live_");
  const [expiresAtUtc, setExpiresAtUtc] = useState("");

  // Result display state for single view of raw key
  const [generatedRawKey, setGeneratedRawKey] = useState<string | null>(null);
  const [copySuccess, setCopySuccess] = useState(false);

  // Queries
  const { data: apiKeys, isLoading, error } = useQuery<ApiKey[]>({
    queryKey: ["apiKeys"],
    queryFn: () => api.getApiKeys()
  });

  const { data: applications } = useQuery<ClientApplication[]>({
    queryKey: ["applications"],
    queryFn: () => api.getApplications(),
    enabled: isModalOpen
  });

  // Mutations
  const createMutation = useMutation({
    mutationFn: (data: any) => api.createApiKey(data),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["apiKeys"] });
      // Expose the raw generated key
      setGeneratedRawKey(data.rawKey || data.RawKey);
      setCopySuccess(false);
    }
  });

  const revokeMutation = useMutation({
    mutationFn: (id: string) => api.revokeApiKey(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["apiKeys"] });
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.deleteApiKey(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["apiKeys"] });
    }
  });

  const openCreateModal = () => {
    setClientApplicationId("");
    setName("");
    setPrefix("ts_live_");
    setExpiresAtUtc("");
    setGeneratedRawKey(null);
    setCopySuccess(false);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setGeneratedRawKey(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors({});

    const rawFormData = {
      clientApplicationId,
      name,
      prefix,
      expiresAtUtc: expiresAtUtc || undefined
    };

    const result = apiKeySchema.safeParse(rawFormData);

    if (!result.success) {
      const errors: Record<string, string> = {};
      result.error.issues.forEach((err) => {
        if (err.path[0]) {
          errors[err.path[0].toString()] = err.message;
        }
      });
      setFormErrors(errors);
      return;
    }

    createMutation.mutate({
      clientApplicationId,
      name,
      prefix,
      expiresAtUtc: expiresAtUtc ? new Date(expiresAtUtc).toISOString() : null
    });
  };

  const handleRevoke = (id: string) => {
    if (confirm("Are you sure you want to revoke this API key? Revoked keys will immediately reject gateway chat completion calls.")) {
      revokeMutation.mutate(id);
    }
  };

  const handleDelete = (id: string) => {
    if (confirm("Are you sure you want to permanently delete this key audit reference?")) {
      deleteMutation.mutate(id);
    }
  };

  const handleCopyToClipboard = () => {
    if (generatedRawKey) {
      navigator.clipboard.writeText(generatedRawKey);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
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
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">API Access Keys</h1>
          <p className="text-sm text-muted-foreground">Manage authentication credentials used by client applications to proxy requests through TokenShield.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-foreground bg-primary rounded-md shadow hover:bg-primary/95 transition-colors gap-2 cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          Generate Key
        </button>
      </div>

      {/* Table section */}
      <div className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-secondary/40 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground border-b border-border">
                <th className="py-3 px-6">Name / Scope</th>
                <th className="py-3 px-6">Client Application</th>
                <th className="py-3 px-6">Key Preview</th>
                <th className="py-3 px-6">Last Used</th>
                <th className="py-3 px-6">Expires</th>
                <th className="py-3 px-6">Status</th>
                <th className="py-3 px-6 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-xs">
              {apiKeys?.map((key) => {
                const isExpired = key.expiresAtUtc ? new Date(key.expiresAtUtc) < new Date() : false;
                const statusColor = key.isRevoked 
                  ? "bg-red-500/10 text-red-400 border-red-500/20" 
                  : isExpired 
                    ? "bg-zinc-500/10 text-zinc-400 border-zinc-500/20" 
                    : "bg-emerald-500/10 text-emerald-400 border-emerald-500/20";
                
                return (
                  <tr key={key.id} className="hover:bg-secondary/10 transition-colors">
                    <td className="py-3.5 px-6 font-semibold">{key.name}</td>
                    <td className="py-3.5 px-6 text-muted-foreground">{key.clientApplicationName}</td>
                    <td className="py-3.5 px-6 font-mono text-foreground font-medium">{key.prefix}******</td>
                    <td className="py-3.5 px-6 text-muted-foreground font-mono">
                      {key.lastUsedAtUtc ? new Date(key.lastUsedAtUtc).toLocaleDateString() : "Never"}
                    </td>
                    <td className="py-3.5 px-6 text-muted-foreground font-mono">
                      {key.expiresAtUtc ? new Date(key.expiresAtUtc).toLocaleDateString() : "Never Expires"}
                    </td>
                    <td className="py-3.5 px-6">
                      <span className={`px-2 py-0.5 rounded-full border text-[10px] font-semibold uppercase tracking-wider ${statusColor}`}>
                        {key.isRevoked ? "Revoked" : isExpired ? "Expired" : "Active"}
                      </span>
                    </td>
                    <td className="py-3.5 px-6 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {!key.isRevoked && !isExpired && (
                          <button
                            onClick={() => handleRevoke(key.id)}
                            className="inline-flex items-center gap-1.5 px-2.5 py-1 text-[11px] font-medium border border-red-500/20 text-red-400 bg-red-500/5 hover:bg-red-500/10 rounded transition-colors cursor-pointer"
                            title="Revoke Key"
                          >
                            <Ban className="h-3 w-3" />
                            Revoke
                          </button>
                        )}
                        <button
                          onClick={() => handleDelete(key.id)}
                          className="p-1 hover:bg-red-500/10 rounded text-muted-foreground hover:text-red-400 transition-colors cursor-pointer"
                          title="Delete reference"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
              {!apiKeys?.length && (
                <tr>
                  <td colSpan={7} className="text-center py-12 text-muted-foreground">
                    <Key className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                    <h3 className="font-bold text-foreground">No API keys generated</h3>
                    <p className="text-sm text-muted-foreground mt-1">Generate a key to authorize clients using the proxy.</p>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Creation Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-lg w-full overflow-hidden flex flex-col">
            <div className="px-6 py-4 border-b border-border flex items-center gap-2">
              <Key className="h-5 w-5 text-primary" />
              <div>
                <h3 className="text-lg font-bold">Generate Access Key</h3>
                <p className="text-xs text-muted-foreground mt-0.5">Authorizes developer app requests using gateway routers.</p>
              </div>
            </div>

            {/* If key is generated, show the raw key precisely once */}
            {generatedRawKey ? (
              <div className="p-6 space-y-6">
                <div className="p-4 border border-amber-500/20 bg-amber-500/5 rounded-lg space-y-3">
                  <div className="flex items-start gap-2.5">
                    <AlertCircle className="h-5 w-5 text-amber-500 shrink-0 mt-0.5" />
                    <div>
                      <h4 className="text-xs font-bold text-amber-500 uppercase tracking-wide">Copy this key now!</h4>
                      <p className="text-xs text-muted-foreground mt-1 leading-relaxed">
                        For security reasons, this raw API key is shown **only once**. Once you close this modal, it cannot be displayed again.
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 bg-secondary border border-border p-2.5 rounded font-mono text-sm text-foreground overflow-x-auto select-all">
                    <span className="flex-1 select-all break-all">{generatedRawKey}</span>
                    <button
                      onClick={handleCopyToClipboard}
                      className="p-1.5 hover:bg-card border border-border rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
                      title="Copy Key"
                    >
                      {copySuccess ? <CheckCircle2 className="h-4 w-4 text-emerald-500" /> : <Copy className="h-4 w-4" />}
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-end">
                  <button
                    onClick={closeModal}
                    className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/95 rounded shadow cursor-pointer"
                  >
                    I Have Copied the Key
                  </button>
                </div>
              </div>
            ) : (
              <form onSubmit={handleSubmit} className="p-6 space-y-4 flex-1">
                {/* Client Application */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Client Application</label>
                  <select
                    value={clientApplicationId}
                    onChange={(e) => setClientApplicationId(e.target.value)}
                    required
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  >
                    <option value="">-- Choose Client App --</option>
                    {applications?.map((app) => (
                      <option key={app.id} value={app.id}>{app.name}</option>
                    ))}
                  </select>
                  {formErrors.clientApplicationId && (
                    <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                      <AlertCircle className="h-3.5 w-3.5" />
                      {formErrors.clientApplicationId}
                    </p>
                  )}
                </div>

                {/* Friendly name */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Friendly Key Name</label>
                  <input
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="e.g. Fraud Investigation Bot Key"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
                  />
                  {formErrors.name && (
                    <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                      <AlertCircle className="h-3.5 w-3.5" />
                      {formErrors.name}
                    </p>
                  )}
                </div>

                {/* Environment Prefix Selector */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Environment prefix</label>
                  <select
                    value={prefix}
                    onChange={(e) => setPrefix(e.target.value)}
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  >
                    <option value="ts_live_">Live (ts_live_xxxxxx)</option>
                    <option value="ts_dev_">Development (ts_dev_xxxxxx)</option>
                  </select>
                </div>

                {/* Expiration date */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                    <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                    Expiration Date (Optional)
                  </label>
                  <input
                    type="date"
                    value={expiresAtUtc}
                    onChange={(e) => setExpiresAtUtc(e.target.value)}
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                  <p className="text-[10px] text-muted-foreground">Leave empty to configure a persistent non-expiring credential reference.</p>
                </div>

                {/* Action Buttons */}
                <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
                  <button
                    type="button"
                    onClick={closeModal}
                    className="px-4 py-2 text-sm font-medium text-foreground bg-secondary hover:bg-secondary/80 rounded border border-border cursor-pointer"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={createMutation.isPending}
                    className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/95 rounded shadow cursor-pointer disabled:opacity-50"
                  >
                    {createMutation.isPending ? "Generating..." : "Generate Key"}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
