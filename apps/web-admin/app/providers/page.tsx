/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { z } from "zod";
import { Server, Plus, Edit2, Trash2, CheckCircle2, XCircle, AlertCircle } from "lucide-react";
import { Provider } from "@/types";

const providerSchema = z.object({
  name: z.string().min(1, "Provider name is required"),
  apiUrl: z.string().min(1, "API URL is required").url("Must be a valid URL (starting with http/https)"),
  apiKeySecretRef: z.string().min(1, "API Key Secret Reference is required")
});

type ProviderFormData = z.infer<typeof providerSchema>;

export default function ProvidersPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProvider, setEditingProvider] = useState<Provider | null>(null);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  
  // Form States
  const [name, setName] = useState("");
  const [apiUrl, setApiUrl] = useState("");
  const [apiKeySecretRef, setApiKeySecretRef] = useState("");
  const [isActive, setIsActive] = useState(true);

  const { data: providers, isLoading, error } = useQuery<Provider[]>({
    queryKey: ["providers"],
    queryFn: () => api.getProviders()
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => api.createProvider(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["providers"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => api.updateProvider(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["providers"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.deleteProvider(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["providers"] });
    }
  });

  const openCreateModal = () => {
    setEditingProvider(null);
    setName("");
    setApiUrl("");
    setApiKeySecretRef("");
    setIsActive(true);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const openEditModal = (provider: Provider) => {
    setEditingProvider(provider);
    setName(provider.name);
    setApiUrl(provider.apiUrl);
    setApiKeySecretRef(provider.apiKeySecretRef);
    setIsActive(provider.isActive);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingProvider(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors({});

    const formData: ProviderFormData = { name, apiUrl, apiKeySecretRef };
    const result = providerSchema.safeParse(formData);

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

    const payload = {
      name,
      apiUrl,
      apiKeySecretRef,
      isActive
    };

    if (editingProvider) {
      updateMutation.mutate({ id: editingProvider.id, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleDelete = (id: string) => {
    if (confirm("Are you sure you want to delete this provider? This will deactivate all associated models.")) {
      deleteMutation.mutate(id);
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
          <h1 className="text-2xl font-bold tracking-tight">Model Providers</h1>
          <p className="text-sm text-muted-foreground">Configure the endpoint URLs and Key Vault secret references for external providers.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-foreground bg-primary rounded-md shadow hover:bg-primary/95 transition-colors gap-2 cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          Add Provider
        </button>
      </div>

      {/* Grid of providers */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {providers?.map((provider) => (
          <div key={provider.id} className="bg-card border border-border rounded-lg p-6 flex flex-col justify-between space-y-4">
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Server className="h-5 w-5 text-primary" />
                  <h3 className="font-bold text-base">{provider.name}</h3>
                </div>
                <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-semibold uppercase tracking-wider border ${
                  provider.isActive 
                    ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20" 
                    : "bg-zinc-500/10 text-zinc-400 border-zinc-500/20"
                }`}>
                  {provider.isActive ? (
                    <>
                      <CheckCircle2 className="h-3 w-3" />
                      Active
                    </>
                  ) : (
                    <>
                      <XCircle className="h-3 w-3" />
                      Disabled
                    </>
                  )}
                </span>
              </div>
              <div className="space-y-1.5 text-xs text-muted-foreground">
                <div>
                  <span className="font-semibold text-foreground">API Url:</span>
                  <p className="font-mono bg-secondary/50 p-1.5 rounded border border-border mt-1 truncate">{provider.apiUrl}</p>
                </div>
                <div className="pt-1.5">
                  <span className="font-semibold text-foreground">Key Vault Ref:</span>
                  <p className="font-mono text-foreground mt-0.5 truncate">{provider.apiKeySecretRef}</p>
                </div>
              </div>
            </div>
            
            <div className="flex items-center justify-end gap-2 pt-4 border-t border-border">
              <button
                onClick={() => openEditModal(provider)}
                className="p-1.5 hover:bg-secondary rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
                title="Edit Provider"
              >
                <Edit2 className="h-4 w-4" />
              </button>
              <button
                onClick={() => handleDelete(provider.id)}
                className="p-1.5 hover:bg-red-500/10 rounded text-muted-foreground hover:text-red-400 transition-colors cursor-pointer"
                title="Delete Provider"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          </div>
        ))}
        {!providers?.length && (
          <div className="col-span-full border border-dashed border-border p-12 rounded-lg text-center space-y-2">
            <Server className="h-10 w-10 text-muted-foreground mx-auto" />
            <h3 className="font-bold text-foreground">No providers found</h3>
            <p className="text-sm text-muted-foreground">Create a new provider to integrate your gateway with LLM models.</p>
          </div>
        )}
      </div>

      {/* Create/Edit Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-lg w-full overflow-hidden flex flex-col">
            <div className="px-6 py-4 border-b border-border">
              <h3 className="text-lg font-bold">{editingProvider ? "Edit Provider" : "Create Provider"}</h3>
              <p className="text-xs text-muted-foreground mt-0.5">Define provider details and credentials secret keys references.</p>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4 flex-1">
              {/* Name field */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Provider Name</label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. OpenAI, Anthropic, Azure OpenAI"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.name && (
                  <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                    <AlertCircle className="h-3.5 w-3.5" />
                    {formErrors.name}
                  </p>
                )}
              </div>

              {/* API Url field */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">API Base Endpoint Url</label>
                <input
                  type="text"
                  value={apiUrl}
                  onChange={(e) => setApiUrl(e.target.value)}
                  placeholder="e.g. https://api.openai.com/v1"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.apiUrl && (
                  <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                    <AlertCircle className="h-3.5 w-3.5" />
                    {formErrors.apiUrl}
                  </p>
                )}
              </div>

              {/* Key Vault Reference */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">API Key Secret Reference</label>
                <input
                  type="text"
                  value={apiKeySecretRef}
                  onChange={(e) => setApiKeySecretRef(e.target.value)}
                  placeholder="e.g. kv-secret-openai"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
                />
                <p className="text-[10px] text-muted-foreground">This is the key vault configuration name, never the raw credential value itself.</p>
                {formErrors.apiKeySecretRef && (
                  <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                    <AlertCircle className="h-3.5 w-3.5" />
                    {formErrors.apiKeySecretRef}
                  </p>
                )}
              </div>

              {/* Active Toggle */}
              <div className="flex items-center gap-2 py-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="h-4 w-4 bg-secondary border border-border rounded text-primary focus:ring-primary focus:ring-opacity-50"
                />
                <label htmlFor="isActive" className="text-xs font-semibold uppercase tracking-wider text-foreground cursor-pointer select-none">Active & Available for Gateway Calls</label>
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
                  disabled={createMutation.isPending || updateMutation.isPending}
                  className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/95 rounded shadow cursor-pointer disabled:opacity-50"
                >
                  {editingProvider ? "Save Changes" : "Create"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
