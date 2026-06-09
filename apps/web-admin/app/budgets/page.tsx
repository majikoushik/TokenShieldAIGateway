/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { z } from "zod";
import { Coins, Plus, Edit2, Trash2, CheckCircle2, AlertCircle, Percent, ShieldAlert } from "lucide-react";
import { BudgetLimit, BudgetScope, BudgetActionType } from "@/types";

const budgetSchema = z.object({
  scope: z.number(),
  targetId: z.string().nullable().optional(),
  monthlyLimit: z.preprocess(
    (val) => (val === "" ? undefined : Number(val)),
    z.number({ message: "Monthly limit is required" }).positive("Monthly limit must be a positive number")
  ),
  warningThresholdPercent: z.preprocess(
    (val) => (val === "" ? undefined : Number(val)),
    z.number({ message: "Warning threshold is required" }).min(1, "Threshold must be at least 1%").max(100, "Threshold cannot exceed 100%")
  ),
  action: z.number()
});

type BudgetFormData = z.infer<typeof budgetSchema>;

export default function BudgetsPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingBudget, setEditingBudget] = useState<BudgetLimit | null>(null);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Form States
  const [scope, setScope] = useState<BudgetScope>(BudgetScope.Tenant);
  const [targetId, setTargetId] = useState<string>("");
  const [monthlyLimit, setMonthlyLimit] = useState<string>("");
  const [warningThresholdPercent, setWarningThresholdPercent] = useState<string>("80");
  const [action, setAction] = useState<BudgetActionType>(BudgetActionType.WarnOnly);

  // Queries
  const { data: budgets, isLoading, error } = useQuery<BudgetLimit[]>({
    queryKey: ["budgets"],
    queryFn: () => api.getBudgets()
  });

  const { data: applications } = useQuery({
    queryKey: ["applications"],
    queryFn: () => api.getApplications(),
    enabled: isModalOpen
  });

  const { data: models } = useQuery({
    queryKey: ["models"],
    queryFn: () => api.getModels(),
    enabled: isModalOpen
  });

  const { data: apiKeys } = useQuery({
    queryKey: ["apiKeys"],
    queryFn: () => api.getApiKeys(),
    enabled: isModalOpen
  });

  // Mutations
  const createMutation = useMutation({
    mutationFn: (data: any) => api.createBudget(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => api.updateBudget(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.deleteBudget(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
    }
  });

  const openCreateModal = () => {
    setEditingBudget(null);
    setScope(BudgetScope.Tenant);
    setTargetId("");
    setMonthlyLimit("");
    setWarningThresholdPercent("80");
    setAction(BudgetActionType.WarnOnly);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const openEditModal = (budget: BudgetLimit) => {
    setEditingBudget(budget);
    setScope(budget.scope);
    setTargetId(budget.targetId || "");
    setMonthlyLimit(budget.monthlyLimit.toString());
    setWarningThresholdPercent(budget.warningThresholdPercent.toString());
    setAction(budget.action);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingBudget(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors({});

    const rawFormData = {
      scope: Number(scope),
      targetId: scope === BudgetScope.Tenant ? null : targetId || null,
      monthlyLimit: monthlyLimit,
      warningThresholdPercent: warningThresholdPercent,
      action: Number(action)
    };

    const result = budgetSchema.safeParse(rawFormData);

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
      scope: Number(scope),
      targetId: scope === BudgetScope.Tenant ? null : targetId || null,
      monthlyLimit: Number(monthlyLimit),
      warningThresholdPercent: Number(warningThresholdPercent),
      action: Number(action)
    };

    if (editingBudget) {
      // For updates, the backend might only require limit, warning, and action
      updateMutation.mutate({
        id: editingBudget.id,
        data: {
          monthlyLimit: payload.monthlyLimit,
          warningThresholdPercent: payload.warningThresholdPercent,
          action: payload.action
        }
      });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleDelete = (id: string) => {
    if (confirm("Are you sure you want to delete this budget limit?")) {
      deleteMutation.mutate(id);
    }
  };

  const getScopeLabel = (s: BudgetScope) => {
    switch (s) {
      case BudgetScope.Tenant: return "Tenant";
      case BudgetScope.Application: return "Application";
      case BudgetScope.ApiKey: return "API Key";
      case BudgetScope.Model: return "Model";
      default: return "Unknown";
    }
  };

  const getActionLabel = (a: BudgetActionType) => {
    switch (a) {
      case BudgetActionType.WarnOnly: return "Warn Only";
      case BudgetActionType.Block: return "Block Requests";
      case BudgetActionType.Downgrade: return "Downgrade Tier";
      default: return "Unknown";
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
          <h1 className="text-2xl font-bold tracking-tight">Budget Limits</h1>
          <p className="text-sm text-muted-foreground">Monitor and enforce spend limits for specific tenant, application, API key, or model scopes.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-foreground bg-primary rounded-md shadow hover:bg-primary/95 transition-colors gap-2 cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          Create Budget
        </button>
      </div>

      {/* Grid of budgets */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {budgets?.map((budget) => {
          const percent = (budget.currentSpend / budget.monthlyLimit) * 100;
          const isWarning = percent >= budget.warningThresholdPercent;
          const isExceeded = percent >= 100;

          // Color classification
          let progressColor = "bg-emerald-500";
          let textColor = "text-emerald-400";
          let badgeBg = "bg-emerald-500/10 border-emerald-500/20";
          if (isExceeded) {
            progressColor = "bg-red-500";
            textColor = "text-red-400";
            badgeBg = "bg-red-500/10 border-red-500/20";
          } else if (isWarning) {
            progressColor = "bg-amber-500";
            textColor = "text-amber-400";
            badgeBg = "bg-amber-500/10 border-amber-500/20";
          }

          return (
            <div key={budget.id} className="bg-card border border-border rounded-lg p-6 flex flex-col justify-between space-y-4">
              <div className="space-y-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <span className="text-[10px] uppercase font-bold tracking-wider text-muted-foreground bg-secondary/80 px-2 py-0.5 rounded border border-border">
                      {getScopeLabel(budget.scope)} Scope
                    </span>
                    <h3 className="font-bold text-base mt-1.5 truncate">
                      {budget.scope === BudgetScope.Tenant ? "Tenant Overall" : budget.targetName || "Acme Scope"}
                    </h3>
                  </div>
                  <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-semibold uppercase tracking-wider border ${badgeBg} ${textColor}`}>
                    {isExceeded ? "Exceeded" : isWarning ? "Warning" : "Healthy"}
                  </span>
                </div>

                {/* Progress bar and metrics */}
                <div className="space-y-2">
                  <div className="flex justify-between text-xs font-semibold">
                    <span>Spent: ${budget.currentSpend.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                    <span className="text-muted-foreground">Limit: ${budget.monthlyLimit.toLocaleString(undefined, { minimumFractionDigits: 2 })}</span>
                  </div>

                  {/* Progress tracks */}
                  <div className="relative w-full h-3 bg-secondary rounded-full overflow-hidden border border-border">
                    {/* Progress Fill */}
                    <div
                      className={`h-full transition-all duration-500 ${progressColor}`}
                      style={{ width: `${Math.min(percent, 100)}%` }}
                    />
                    
                    {/* Warning mark line */}
                    <div 
                      className="absolute top-0 bottom-0 w-0.5 bg-yellow-500/50"
                      style={{ left: `${budget.warningThresholdPercent}%` }}
                      title={`Warning threshold at ${budget.warningThresholdPercent}%`}
                    />
                  </div>

                  <div className="flex justify-between items-center text-[10px] text-muted-foreground pt-0.5">
                    <span>Usage: {percent.toFixed(1)}%</span>
                    <span>Warning threshold: {budget.warningThresholdPercent}%</span>
                  </div>
                </div>

                {/* Scope target metadata */}
                <div className="space-y-2 pt-2 border-t border-border/60 text-xs">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Enforcement Action:</span>
                    <span className="font-semibold text-foreground flex items-center gap-1">
                      <ShieldAlert className="h-3.5 w-3.5 text-primary" />
                      {getActionLabel(budget.action)}
                    </span>
                  </div>
                </div>
              </div>

              <div className="flex items-center justify-end gap-2 pt-4 border-t border-border">
                <button
                  onClick={() => openEditModal(budget)}
                  className="p-1.5 hover:bg-secondary rounded text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
                  title="Edit Budget"
                >
                  <Edit2 className="h-4 w-4" />
                </button>
                <button
                  onClick={() => handleDelete(budget.id)}
                  className="p-1.5 hover:bg-red-500/10 rounded text-muted-foreground hover:text-red-400 transition-colors cursor-pointer"
                  title="Delete Budget"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            </div>
          );
        })}

        {!budgets?.length && (
          <div className="col-span-full border border-dashed border-border p-12 rounded-lg text-center space-y-2">
            <Coins className="h-10 w-10 text-muted-foreground mx-auto" />
            <h3 className="font-bold text-foreground">No budgets configured</h3>
            <p className="text-sm text-muted-foreground">Enforce limits to prevent unexpected spending spikes.</p>
          </div>
        )}
      </div>

      {/* Create/Edit Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-lg w-full overflow-hidden flex flex-col">
            <div className="px-6 py-4 border-b border-border">
              <h3 className="text-lg font-bold">{editingBudget ? "Edit Budget Limit" : "Create Budget Limit"}</h3>
              <p className="text-xs text-muted-foreground mt-0.5">Control token cost overheads and configure threshold rules.</p>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4 flex-1">
              
              {/* Scope select (only allowed on create) */}
              {!editingBudget && (
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Budget Scope</label>
                  <select
                    value={scope}
                    onChange={(e) => {
                      setScope(Number(e.target.value) as BudgetScope);
                      setTargetId("");
                    }}
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  >
                    <option value={BudgetScope.Tenant}>Tenant Overall</option>
                    <option value={BudgetScope.Application}>Client Application</option>
                    <option value={BudgetScope.ApiKey}>API Key</option>
                    <option value={BudgetScope.Model}>AI Model</option>
                  </select>
                </div>
              )}

              {/* Dynamic Target selector */}
              {!editingBudget && scope !== BudgetScope.Tenant && (
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Select Target {getScopeLabel(scope)}
                  </label>
                  <select
                    value={targetId}
                    onChange={(e) => setTargetId(e.target.value)}
                    required
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  >
                    <option value="">-- Choose Target --</option>
                    {scope === BudgetScope.Application &&
                      applications?.map((app: any) => (
                        <option key={app.id} value={app.id}>{app.name}</option>
                      ))}
                    {scope === BudgetScope.ApiKey &&
                      apiKeys?.map((key: any) => (
                        <option key={key.id} value={key.id}>{key.name} ({key.prefix}***)</option>
                      ))}
                    {scope === BudgetScope.Model &&
                      models?.map((model: any) => (
                        <option key={model.id} value={model.id}>{model.name} ({model.providerName})</option>
                      ))}
                  </select>
                  {formErrors.targetId && (
                    <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                      <AlertCircle className="h-3.5 w-3.5" />
                      {formErrors.targetId}
                    </p>
                  )}
                </div>
              )}

              {/* Monthly Limit */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Monthly Limit (USD $)</label>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={monthlyLimit}
                  onChange={(e) => setMonthlyLimit(e.target.value)}
                  placeholder="e.g. 500.00"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.monthlyLimit && (
                  <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                    <AlertCircle className="h-3.5 w-3.5" />
                    {formErrors.monthlyLimit}
                  </p>
                )}
              </div>

              {/* Warning Threshold Percent */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Warning Threshold (%)</label>
                <div className="relative flex items-center">
                  <input
                    type="number"
                    min="1"
                    max="100"
                    value={warningThresholdPercent}
                    onChange={(e) => setWarningThresholdPercent(e.target.value)}
                    placeholder="e.g. 80"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:border-primary pr-8"
                  />
                  <Percent className="absolute right-3 h-4 w-4 text-muted-foreground pointer-events-none" />
                </div>
                {formErrors.warningThresholdPercent && (
                  <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1">
                    <AlertCircle className="h-3.5 w-3.5" />
                    {formErrors.warningThresholdPercent}
                  </p>
                )}
              </div>

              {/* Enforcement Action */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Action on Exceeded Limit</label>
                <select
                  value={action}
                  onChange={(e) => setAction(Number(e.target.value) as BudgetActionType)}
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                >
                  <option value={BudgetActionType.WarnOnly}>Warn Only (Log Alert)</option>
                  <option value={BudgetActionType.Block}>Block (Decline Gateway Calls)</option>
                  <option value={BudgetActionType.Downgrade}>Downgrade (Route to Cheapest Tier)</option>
                </select>
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
                  {editingBudget ? "Save Changes" : "Create"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
