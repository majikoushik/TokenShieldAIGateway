/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { z } from "zod";
import { Cpu, Plus, Edit2, Trash2, CheckCircle2, XCircle, AlertCircle } from "lucide-react";
import { AiModel, Provider, ModelTier } from "@/types";

const modelSchema = z.object({
  providerId: z.string().min(1, "Provider selection is required"),
  name: z.string().min(1, "Model name is required"),
  deploymentName: z.string().min(1, "Deployment name is required"),
  tier: z.coerce.number().int().min(0).max(2),
  inputTokenPricePerMillion: z.coerce.number().min(0, "Price must be non-negative"),
  outputTokenPricePerMillion: z.coerce.number().min(0, "Price must be non-negative"),
  contextWindow: z.coerce.number().int().positive("Context size must be positive")
});

type ModelFormData = z.infer<typeof modelSchema>;

export default function ModelsPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingModel, setEditingModel] = useState<AiModel | null>(null);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Form States
  const [providerId, setProviderId] = useState("");
  const [name, setName] = useState("");
  const [deploymentName, setDeploymentName] = useState("");
  const [tier, setTier] = useState<ModelTier>(ModelTier.Standard);
  const [inputTokenPricePerMillion, setInputTokenPricePerMillion] = useState("0");
  const [outputTokenPricePerMillion, setOutputTokenPricePerMillion] = useState("0");
  const [contextWindow, setContextWindow] = useState("8192");
  const [isActive, setIsActive] = useState(true);

  const { data: models, isLoading: modelsLoading } = useQuery<AiModel[]>({
    queryKey: ["models"],
    queryFn: () => api.getModels()
  });

  const { data: providers, isLoading: providersLoading } = useQuery<Provider[]>({
    queryKey: ["providers"],
    queryFn: () => api.getProviders()
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => api.createModel(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["models"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => api.updateModel(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["models"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.deleteModel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["models"] });
    }
  });

  const openCreateModal = () => {
    setEditingModel(null);
    setProviderId(providers?.[0]?.id || "");
    setName("");
    setDeploymentName("");
    setTier(ModelTier.Standard);
    setInputTokenPricePerMillion("0");
    setOutputTokenPricePerMillion("0");
    setContextWindow("8192");
    setIsActive(true);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const openEditModal = (model: AiModel) => {
    setEditingModel(model);
    setProviderId(model.providerId);
    setName(model.name);
    setDeploymentName(model.deploymentName);
    setTier(model.tier);
    setInputTokenPricePerMillion(model.inputTokenPricePerMillion.toString());
    setOutputTokenPricePerMillion(model.outputTokenPricePerMillion.toString());
    setContextWindow(model.contextWindow.toString());
    setIsActive(model.isActive);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingModel(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors({});

    const formData = {
      providerId,
      name,
      deploymentName,
      tier: Number(tier),
      inputTokenPricePerMillion: Number(inputTokenPricePerMillion),
      outputTokenPricePerMillion: Number(outputTokenPricePerMillion),
      contextWindow: Number(contextWindow)
    };

    const result = modelSchema.safeParse(formData);

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
      providerId,
      name,
      deploymentName,
      tier: Number(tier),
      inputTokenPricePerMillion: Number(inputTokenPricePerMillion),
      outputTokenPricePerMillion: Number(outputTokenPricePerMillion),
      contextWindow: Number(contextWindow),
      isActive
    };

    if (editingModel) {
      updateMutation.mutate({ id: editingModel.id, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleDelete = (id: string) => {
    if (confirm("Are you sure you want to delete this model? This will prevent requests from routing to it.")) {
      deleteMutation.mutate(id);
    }
  };

  const getTierName = (t: ModelTier) => {
    return t === ModelTier.Cheap ? "cheap" : t === ModelTier.Premium ? "premium" : "standard";
  };

  const getTierColor = (t: ModelTier) => {
    return t === ModelTier.Cheap 
      ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20" 
      : t === ModelTier.Premium 
        ? "bg-purple-500/10 text-purple-400 border-purple-500/20" 
        : "bg-blue-500/10 text-blue-400 border-blue-500/20";
  };

  if (modelsLoading || providersLoading) {
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
          <h1 className="text-2xl font-bold tracking-tight">Models Catalog</h1>
          <p className="text-sm text-muted-foreground">Register specific provider models, configure model tier parameters, and specify token pricing.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-foreground bg-primary rounded-md shadow hover:bg-primary/95 transition-colors gap-2 cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          Add Model
        </button>
      </div>

      {/* Models List Table */}
      <div className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-secondary/40 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground border-b border-border">
                <th className="py-3 px-6">Model</th>
                <th className="py-3 px-6">Provider</th>
                <th className="py-3 px-6">Deployment Name</th>
                <th className="py-3 px-6">Tier</th>
                <th className="py-3 px-6">Input / Output Spend Rate (per M)</th>
                <th className="py-3 px-6">Context Window</th>
                <th className="py-3 px-6">Status</th>
                <th className="py-3 px-6 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border text-xs">
              {models?.map((model) => (
                <tr key={model.id} className="hover:bg-secondary/15 transition-colors">
                  <td className="py-3.5 px-6 font-semibold flex items-center gap-2">
                    <Cpu className="h-4 w-4 text-primary shrink-0" />
                    <span>{model.name}</span>
                  </td>
                  <td className="py-3.5 px-6">{model.providerName}</td>
                  <td className="py-3.5 px-6 font-mono text-muted-foreground">{model.deploymentName}</td>
                  <td className="py-3.5 px-6">
                    <span className={`px-2.5 py-0.5 rounded-full border text-[10px] font-semibold uppercase tracking-wider ${getTierColor(model.tier)}`}>
                      {getTierName(model.tier)}
                    </span>
                  </td>
                  <td className="py-3.5 px-6 font-mono">
                    <span className="font-semibold">${model.inputTokenPricePerMillion.toFixed(3)}</span>
                    <span className="text-muted-foreground text-[10px]"> / </span>
                    <span className="font-semibold">${model.outputTokenPricePerMillion.toFixed(3)}</span>
                  </td>
                  <td className="py-3.5 px-6 font-mono text-muted-foreground">{model.contextWindow.toLocaleString()}</td>
                  <td className="py-3.5 px-6">
                    <span className={`inline-flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider ${
                      model.isActive ? "text-emerald-400" : "text-zinc-500"
                    }`}>
                      {model.isActive ? <CheckCircle2 className="h-3.5 w-3.5" /> : <XCircle className="h-3.5 w-3.5" />}
                      {model.isActive ? "Active" : "Disabled"}
                    </span>
                  </td>
                  <td className="py-3.5 px-6 text-right">
                    <div className="flex items-center justify-end gap-1.5">
                      <button
                        onClick={() => openEditModal(model)}
                        className="p-1.5 hover:bg-secondary rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
                        title="Edit Model"
                      >
                        <Edit2 className="h-3.5 w-3.5" />
                      </button>
                      <button
                        onClick={() => handleDelete(model.id)}
                        className="p-1.5 hover:bg-red-500/10 rounded text-muted-foreground hover:text-red-400 transition-colors cursor-pointer"
                        title="Delete Model"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {!models?.length && (
                <tr>
                  <td colSpan={8} className="text-center py-8 text-muted-foreground">No AI models cataloged in this system.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Model Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-lg w-full overflow-hidden">
            <div className="px-6 py-4 border-b border-border">
              <h3 className="text-lg font-bold">{editingModel ? "Edit AI Model" : "Catalog New AI Model"}</h3>
              <p className="text-xs text-muted-foreground mt-0.5">Specify API mappings, tier definitions, and token price rates.</p>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                {/* Provider Field */}
                {!editingModel && (
                  <div className="space-y-1.5 sm:col-span-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Provider Connection</label>
                    <select
                      value={providerId}
                      onChange={(e) => setProviderId(e.target.value)}
                      className="w-full bg-secondary border border-border px-3 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                    >
                      {providers?.map((p) => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>
                )}

                {/* Model Name */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Model Identifier</label>
                  <input
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="e.g. gpt-4o"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                  {formErrors.name && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.name}</p>}
                </div>

                {/* Deployment Name */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Deployment Name</label>
                  <input
                    type="text"
                    value={deploymentName}
                    onChange={(e) => setDeploymentName(e.target.value)}
                    placeholder="e.g. deploy-gpt-4o"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                  {formErrors.deploymentName && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.deploymentName}</p>}
                </div>

                {/* Tier */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Model Routing Tier</label>
                  <select
                    value={tier}
                    onChange={(e) => setTier(Number(e.target.value))}
                    className="w-full bg-secondary border border-border px-3 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  >
                    <option value={ModelTier.Cheap}>Cheap</option>
                    <option value={ModelTier.Standard}>Standard</option>
                    <option value={ModelTier.Premium}>Premium</option>
                  </select>
                </div>

                {/* Context Window */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Context Window Size</label>
                  <input
                    type="number"
                    value={contextWindow}
                    onChange={(e) => setContextWindow(e.target.value)}
                    placeholder="e.g. 128000"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                  {formErrors.contextWindow && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.contextWindow}</p>}
                </div>

                {/* Input Cost */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Input Cost ($ per Million)</label>
                  <input
                    type="text"
                    value={inputTokenPricePerMillion}
                    onChange={(e) => setInputTokenPricePerMillion(e.target.value)}
                    placeholder="e.g. 2.50"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                  {formErrors.inputTokenPricePerMillion && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.inputTokenPricePerMillion}</p>}
                </div>

                {/* Output Cost */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Output Cost ($ per Million)</label>
                  <input
                    type="text"
                    value={outputTokenPricePerMillion}
                    onChange={(e) => setOutputTokenPricePerMillion(e.target.value)}
                    placeholder="e.g. 10.00"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                  {formErrors.outputTokenPricePerMillion && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.outputTokenPricePerMillion}</p>}
                </div>
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
                <label htmlFor="isActive" className="text-xs font-semibold uppercase tracking-wider text-foreground cursor-pointer select-none">Active & Resolveable</label>
              </div>

              {/* Actions */}
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
                  {editingModel ? "Save Changes" : "Create"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
