/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { z } from "zod";
import { Building2, Plus, CheckCircle2, ShieldAlert, Key, Server, Laptop, Activity, AlertCircle } from "lucide-react";
import { ClientApplication } from "@/types";

const applicationSchema = z.object({
  name: z.string().min(1, "Application name is required").max(100, "Name must be less than 100 characters")
});

type ApplicationFormData = z.infer<typeof applicationSchema>;

export default function SettingsPage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<"tenant" | "apps" | "infra">("tenant");
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [appName, setAppName] = useState("");
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Queries
  const { data: applications, isLoading } = useQuery<ClientApplication[]>({
    queryKey: ["applications"],
    queryFn: () => api.getApplications()
  });

  // Mutations
  const createMutation = useMutation({
    mutationFn: (data: any) => api.createApplication(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["applications"] });
      closeModal();
    }
  });

  const openCreateModal = () => {
    setAppName("");
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setAppName("");
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors({});

    const result = applicationSchema.safeParse({ name: appName });

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

    createMutation.mutate({ name: appName });
  };

  return (
    <div className="space-y-6">
      {/* Header section */}
      <div className="space-y-1">
        <h1 className="text-2xl font-bold tracking-tight">System Settings</h1>
        <p className="text-sm text-muted-foreground">Configure tenant context parameters, client developer applications, and Azure Key Vault mappings.</p>
      </div>

      {/* Tabs list */}
      <div className="border-b border-border flex gap-4 text-xs font-semibold">
        <button
          onClick={() => setActiveTab("tenant")}
          className={`pb-3 border-b-2 font-bold uppercase tracking-wider cursor-pointer ${
            activeTab === "tenant" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"
          }`}
        >
          Tenant Workspace
        </button>
        <button
          onClick={() => setActiveTab("apps")}
          className={`pb-3 border-b-2 font-bold uppercase tracking-wider cursor-pointer ${
            activeTab === "apps" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"
          }`}
        >
          Client Applications ({applications?.length || 0})
        </button>
        <button
          onClick={() => setActiveTab("infra")}
          className={`pb-3 border-b-2 font-bold uppercase tracking-wider cursor-pointer ${
            activeTab === "infra" ? "border-primary text-primary" : "border-transparent text-muted-foreground hover:text-foreground"
          }`}
        >
          Vault Credentials Status
        </button>
      </div>

      {/* Tab Contents */}
      <div className="mt-6">
        
        {/* Tenant Tab */}
        {activeTab === "tenant" && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-6 max-w-3xl">
            <div className="flex items-center gap-3">
              <Building2 className="h-6 w-6 text-primary" />
              <div>
                <h3 className="font-bold text-base">Acme Enterprise</h3>
                <p className="text-xs text-muted-foreground mt-0.5">Primary SaaS Tenant Workspace Context</p>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2 text-xs">
              <div className="space-y-1">
                <span className="font-semibold text-muted-foreground">Tenant UUID Identifier</span>
                <p className="font-mono bg-secondary/50 p-2 border border-border rounded mt-1 text-foreground select-all break-all">
                  00000000-0000-0000-0000-000000000000
                </p>
              </div>

              <div className="space-y-1">
                <span className="font-semibold text-muted-foreground">Provisioning Environment</span>
                <p className="font-mono bg-secondary/50 p-2 border border-border rounded mt-1 text-foreground">
                  Azure Container Apps (Sandbox Sandbox-1)
                </p>
              </div>

              <div className="space-y-1">
                <span className="font-semibold text-muted-foreground">Default Regional Endpoint</span>
                <p className="font-mono bg-secondary/50 p-2 border border-border rounded mt-1 text-foreground select-all">
                  https://gateway.acme-tokenshield.com/v1
                </p>
              </div>

              <div className="space-y-1">
                <span className="font-semibold text-muted-foreground">Created Date</span>
                <p className="font-mono bg-secondary/50 p-2 border border-border rounded mt-1 text-foreground">
                  2026-06-01T00:00:00Z
                </p>
              </div>
            </div>

            <div className="p-4 bg-secondary/35 border border-border rounded flex gap-2.5 items-start">
              <ShieldAlert className="h-4.5 w-4.5 text-primary shrink-0 mt-0.5" />
              <div className="space-y-1">
                <h4 className="text-xs font-bold text-foreground uppercase tracking-wide">Tenant Isolation Enforced</h4>
                <p className="text-xs text-muted-foreground leading-relaxed">
                  All databases queries, routing logs, api key validations, and budget calculations are isolated by this tenant identifier automatically. Cross-tenant queries are blocked at the persistence layer.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Client Applications Tab */}
        {activeTab === "apps" && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <h3 className="font-bold text-base">Developer Client Applications</h3>
                <p className="text-xs text-muted-foreground">Applications registered under your tenant mapping to proxy keys and budgets.</p>
              </div>
              <button
                onClick={openCreateModal}
                className="inline-flex items-center justify-center px-3.5 py-1.5 text-xs font-medium text-primary-foreground bg-primary rounded shadow hover:bg-primary/95 transition-colors gap-1.5 cursor-pointer"
              >
                <Plus className="h-3.5 w-3.5" />
                Register App
              </button>
            </div>

            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {applications?.map((app) => (
                <div key={app.id} className="bg-card border border-border rounded-lg p-5 flex flex-col justify-between space-y-3">
                  <div className="flex items-center gap-2">
                    <Laptop className="h-5 w-5 text-primary" />
                    <span className="font-bold text-sm text-foreground">{app.name}</span>
                  </div>
                  <div className="space-y-1 text-[11px] text-muted-foreground">
                    <div>
                      <span className="font-semibold text-foreground">App ID:</span>
                      <p className="font-mono select-all truncate mt-0.5">{app.id}</p>
                    </div>
                    <div className="pt-1">
                      <span className="font-semibold text-foreground font-sans">Created:</span>
                      <span className="font-mono ml-1">{new Date(app.createdAtUtc).toLocaleDateString()}</span>
                    </div>
                  </div>
                </div>
              ))}
              {!applications?.length && (
                <div className="col-span-full border border-dashed border-border p-12 rounded-lg text-center space-y-2 bg-card">
                  <Laptop className="h-10 w-10 text-muted-foreground mx-auto" />
                  <h3 className="font-bold text-foreground">No applications registered</h3>
                  <p className="text-sm text-muted-foreground">Register an app to scope your API keys and monthly budgets.</p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Credentials Tab */}
        {activeTab === "infra" && (
          <div className="bg-card border border-border rounded-lg p-6 space-y-6 max-w-3xl">
            <div className="flex items-center gap-3">
              <Server className="h-6 w-6 text-primary" />
              <div>
                <h3 className="font-bold text-base">Azure Key Vault Integration</h3>
                <p className="text-xs text-muted-foreground mt-0.5">Secret references mappings status</p>
              </div>
            </div>

            <div className="p-4 border border-emerald-500/20 bg-emerald-500/5 rounded-lg flex items-start gap-3">
              <CheckCircle2 className="h-5 w-5 text-emerald-500 shrink-0 mt-0.5" />
              <div className="space-y-1">
                <h4 className="text-xs font-bold text-emerald-500 uppercase tracking-wide">Key Vault Connected</h4>
                <p className="text-xs text-muted-foreground leading-relaxed">
                  The API gateway container is authorized to retrieve secret reference values from Key Vault automatically.
                </p>
              </div>
            </div>

            <div className="space-y-3.5 text-xs text-muted-foreground">
              <div className="flex justify-between items-center py-2 border-b border-border/60">
                <span className="font-semibold text-foreground">Key Vault URI</span>
                <span className="font-mono text-foreground">https://kv-tokenshield-prod.vault.azure.net/</span>
              </div>
              <div className="flex justify-between items-center py-2 border-b border-border/60">
                <span className="font-semibold text-foreground">Authentication Principal</span>
                <span className="font-mono text-foreground">Managed System Identity (MSI)</span>
              </div>
              <div className="flex justify-between items-center py-2 border-b border-border/60">
                <span className="font-semibold text-foreground">Provider Reference Prefix</span>
                <span className="font-mono text-foreground">kv-secret-provider-*</span>
              </div>
              <div className="flex justify-between items-center py-2">
                <span className="font-semibold text-foreground">Last Telemetry Health Check</span>
                <span className="font-mono text-emerald-400 flex items-center gap-1">
                  <Activity className="h-3.5 w-3.5" />
                  Passed (0ms latency)
                </span>
              </div>
            </div>
          </div>
        )}

      </div>

      {/* Register App Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-md w-full overflow-hidden flex flex-col">
            <div className="px-6 py-4 border-b border-border">
              <h3 className="text-lg font-bold">Register Client Application</h3>
              <p className="text-xs text-muted-foreground mt-0.5">Define a new developer client app scope under your tenant.</p>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Application Name</label>
                <input
                  type="text"
                  value={appName}
                  onChange={(e) => setAppName(e.target.value)}
                  placeholder="e.g. Acme Customer Support Chatbot"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.name && (
                  <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                    <AlertCircle className="h-3.5 w-3.5" />
                    {formErrors.name}
                  </p>
                )}
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
                  {createMutation.isPending ? "Registering..." : "Register"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
