/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { useState } from "react";
import { z } from "zod";
import { Route, Plus, Edit2, Trash2, CheckCircle2, XCircle, AlertCircle, Play, Info } from "lucide-react";
import { RoutingRule, RoutingActionType, ModelTier } from "@/types";

const ruleSchema = z.object({
  name: z.string().min(1, "Rule name is required"),
  priority: z.coerce.number().int().positive("Priority must be positive"),
  conditionsJson: z.string().min(1, "Conditions JSON is required").refine((val) => {
    try {
      JSON.parse(val);
      return true;
    } catch {
      return false;
    }
  }, "Conditions must be a valid JSON string structure"),
  action: z.coerce.number().int().min(0).max(2),
  targetTier: z.coerce.number().int().min(0).max(2).nullable()
});

export default function RoutingRulesPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<RoutingRule | null>(null);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Form States
  const [name, setName] = useState("");
  const [priority, setPriority] = useState("1");
  const [conditionsJson, setConditionsJson] = useState("[]");
  const [action, setAction] = useState<RoutingActionType>(RoutingActionType.RouteToTier);
  const [targetTier, setTargetTier] = useState<ModelTier | null>(ModelTier.Cheap);
  const [isActive, setIsActive] = useState(true);

  // Test Panel States
  const [testTaskType, setTestTaskType] = useState("summarization");
  const [testRiskLevel, setTestRiskLevel] = useState("low");
  const [testComplexityScore, setTestComplexityScore] = useState("30");
  const [testContainsPii, setTestContainsPii] = useState(false);
  const [testResult, setTestResult] = useState<{ matchedRule: string; finalAction: string; targetTier?: string } | null>(null);

  const { data: rules, isLoading } = useQuery<RoutingRule[]>({
    queryKey: ["routingRules"],
    queryFn: () => api.getRules()
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => api.createRule(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["routingRules"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => api.updateRule(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["routingRules"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.deleteRule(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["routingRules"] });
    }
  });

  const openCreateModal = () => {
    setEditingRule(null);
    setName("");
    setPriority((rules ? rules.length + 1 : 1).toString());
    setConditionsJson('[{"field":"taskType","operator":"Equals","value":"summarization"}]');
    setAction(RoutingActionType.RouteToTier);
    setTargetTier(ModelTier.Cheap);
    setIsActive(true);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const openEditModal = (rule: RoutingRule) => {
    setEditingRule(rule);
    setName(rule.name);
    setPriority(rule.priority.toString());
    setConditionsJson(rule.conditionsJson);
    setAction(rule.action);
    setTargetTier(rule.targetTier);
    setIsActive(rule.isActive);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingRule(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors({});

    const formData = {
      name,
      priority: Number(priority),
      conditionsJson,
      action: Number(action),
      targetTier: action === RoutingActionType.RouteToTier ? (targetTier !== null ? Number(targetTier) : null) : null
    };

    const result = ruleSchema.safeParse(formData);

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
      ...formData,
      isActive
    };

    if (editingRule) {
      updateMutation.mutate({ id: editingRule.id, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleDelete = (id: string) => {
    if (confirm("Are you sure you want to delete this routing rule?")) {
      deleteMutation.mutate(id);
    }
  };

  // Local rule evaluation engine simulation for the test panel
  const handleTestRuleEngine = () => {
    if (!rules || rules.length === 0) {
      setTestResult({ matchedRule: "Default Fallback", finalAction: "RouteToTier", targetTier: "standard" });
      return;
    }

    const activeRules = rules.filter(r => r.isActive).sort((a, b) => a.priority - b.priority);
    const mockRequest = {
      taskType: testTaskType,
      riskLevel: testRiskLevel,
      complexityScore: Number(testComplexityScore),
      containsPii: testContainsPii.toString()
    };

    let matched: RoutingRule | null = null;

    for (const r of activeRules) {
      try {
        const conditions = JSON.parse(r.conditionsJson);
        let allConditionsPassed = true;

        if (Array.isArray(conditions)) {
          for (const cond of conditions) {
            const reqVal = (mockRequest as any)[cond.field]?.toString().toLowerCase();
            const condVal = cond.value?.toString().toLowerCase();

            if (cond.operator === "Equals") {
              if (reqVal !== condVal) allConditionsPassed = false;
            } else if (cond.operator === "NotEquals") {
              if (reqVal === condVal) allConditionsPassed = false;
            } else if (cond.operator === "GreaterThan") {
              if (!(Number(reqVal) > Number(condVal))) allConditionsPassed = false;
            } else if (cond.operator === "LessThan") {
              if (!(Number(reqVal) < Number(condVal))) allConditionsPassed = false;
            }
          }
        }

        if (allConditionsPassed && conditions.length > 0) {
          matched = r;
          break;
        }
      } catch (ex) {
        console.warn("Error parsing conditions inside rule test evaluator:", ex);
      }
    }

    if (matched) {
      setTestResult({
        matchedRule: matched.name,
        finalAction: matched.action === RoutingActionType.Block ? "Block Request (403)" : matched.action === RoutingActionType.HumanReview ? "Require Human Review (422)" : "Route to Model Tier",
        targetTier: matched.action === RoutingActionType.RouteToTier ? (matched.targetTier === ModelTier.Cheap ? "cheap" : matched.targetTier === ModelTier.Premium ? "premium" : "standard") : undefined
      });
    } else {
      setTestResult({
        matchedRule: "Default System Logic (No rule matched)",
        finalAction: "Route to Model Tier",
        targetTier: "standard"
      });
    }
  };

  const getActionName = (a: RoutingActionType) => {
    return a === RoutingActionType.Block ? "Block" : a === RoutingActionType.HumanReview ? "Human Review" : "Route To Tier";
  };

  const getActionColor = (a: RoutingActionType) => {
    return a === RoutingActionType.Block 
      ? "bg-red-500/10 text-red-400 border-red-500/20" 
      : a === RoutingActionType.HumanReview 
        ? "bg-amber-500/10 text-amber-400 border-amber-500/20" 
        : "bg-emerald-500/10 text-emerald-400 border-emerald-500/20";
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
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">Routing Rules</h1>
          <p className="text-sm text-muted-foreground">Manage gateway intercept parameters. Requests matching these conditions override default model tier selection.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="inline-flex items-center justify-center px-4 py-2 text-sm font-medium text-primary-foreground bg-primary rounded-md shadow hover:bg-primary/95 transition-colors gap-2 cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          Create Rule
        </button>
      </div>

      <div className="grid gap-6 lg:grid-cols-3 items-start">
        {/* Rules List (Left 2 columns) */}
        <div className="lg:col-span-2 space-y-4">
          {rules?.map((rule) => (
            <div key={rule.id} className="bg-card border border-border p-5 rounded-lg flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div className="space-y-2 max-w-xl">
                <div className="flex items-center gap-3">
                  <span className="h-6 w-6 rounded-full bg-secondary border border-border flex items-center justify-center text-xs font-bold text-primary">
                    {rule.priority}
                  </span>
                  <h3 className="font-bold text-sm text-foreground">{rule.name}</h3>
                  <span className={`px-2 py-0.5 rounded-full border text-[9px] font-semibold uppercase tracking-wider ${
                    rule.isActive ? "bg-emerald-500/5 text-emerald-400 border-emerald-500/10" : "bg-zinc-500/5 text-zinc-400 border-zinc-500/10"
                  }`}>
                    {rule.isActive ? "Active" : "Disabled"}
                  </span>
                </div>
                
                {/* Conditions info */}
                <div className="text-xs text-muted-foreground pl-9">
                  <span className="font-semibold text-foreground">Conditions (JSON):</span>
                  <pre className="font-mono bg-secondary/50 p-2 rounded border border-border mt-1 text-[10px] overflow-x-auto max-h-24">
                    {rule.conditionsJson}
                  </pre>
                </div>
                
                {/* Action info */}
                <div className="text-xs pl-9 flex items-center gap-2">
                  <span className="font-semibold text-muted-foreground">Action outcome:</span>
                  <span className={`px-2 py-0.5 rounded border text-[10px] font-bold uppercase ${getActionColor(rule.action)}`}>
                    {getActionName(rule.action)}
                  </span>
                  {rule.action === RoutingActionType.RouteToTier && rule.targetTier !== null && (
                    <span className="text-[11px] font-semibold text-muted-foreground">
                      &rarr; target tier: <span className="text-foreground font-bold">{rule.targetTier === ModelTier.Cheap ? "cheap" : rule.targetTier === ModelTier.Premium ? "premium" : "standard"}</span>
                    </span>
                  )}
                </div>
              </div>

              {/* Actions right */}
              <div className="flex items-center gap-1 shrink-0 self-end sm:self-center">
                <button
                  onClick={() => openEditModal(rule)}
                  className="p-2 hover:bg-secondary rounded text-muted-foreground hover:text-foreground cursor-pointer"
                  title="Edit Rule"
                >
                  <Edit2 className="h-4 w-4" />
                </button>
                <button
                  onClick={() => handleDelete(rule.id)}
                  className="p-2 hover:bg-red-500/10 rounded text-muted-foreground hover:text-red-400 cursor-pointer"
                  title="Delete Rule"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            </div>
          ))}

          {!rules?.length && (
            <div className="border border-dashed border-border p-12 rounded-lg text-center space-y-2 bg-card">
              <Route className="h-10 w-10 text-muted-foreground mx-auto" />
              <h3 className="font-bold text-foreground">No routing rules created</h3>
              <p className="text-sm text-muted-foreground">Requests will automatically be routed to the system&apos;s &quot;standard&quot; tier.</p>
            </div>
          )}
        </div>

        {/* Local Test Simulator Panel (Right column) */}
        <div className="bg-card border border-border rounded-lg p-6 space-y-4">
          <div className="flex items-center gap-2 border-b border-border pb-3">
            <Play className="h-4 w-4 text-primary" />
            <h3 className="font-bold text-sm uppercase tracking-wider">Routing Simulator</h3>
          </div>
          <p className="text-xs text-muted-foreground">Test how TokenShield resolves model tiers and actions dynamically based on custom request payloads.</p>
          
          <div className="space-y-4 pt-2">
            {/* Task Type input */}
            <div className="space-y-1">
              <label className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">taskType (Metadata)</label>
              <input
                type="text"
                value={testTaskType}
                onChange={(e) => setTestTaskType(e.target.value)}
                className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-xs focus:outline-none focus:border-primary"
              />
            </div>

            {/* Risk Level input */}
            <div className="space-y-1">
              <label className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">riskLevel</label>
              <select
                value={testRiskLevel}
                onChange={(e) => setTestRiskLevel(e.target.value)}
                className="w-full bg-secondary border border-border px-3.5 py-1.5 rounded text-xs focus:outline-none focus:border-primary"
              >
                <option value="low">low</option>
                <option value="medium">medium</option>
                <option value="high">high</option>
              </select>
            </div>

            {/* Complexity input */}
            <div className="space-y-1">
              <label className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">complexityScore (0-100)</label>
              <input
                type="number"
                value={testComplexityScore}
                onChange={(e) => setTestComplexityScore(e.target.value)}
                className="w-full bg-secondary border border-border px-3 py-1.5 rounded text-xs focus:outline-none focus:border-primary"
              />
            </div>

            {/* Contains PII check */}
            <div className="flex items-center gap-2 py-1">
              <input
                type="checkbox"
                id="testPii"
                checked={testContainsPii}
                onChange={(e) => setTestContainsPii(e.target.checked)}
                className="h-3.5 w-3.5"
              />
              <label htmlFor="testPii" className="text-xs font-semibold text-foreground cursor-pointer">containsPii flag = true</label>
            </div>

            {/* Run button */}
            <button
              onClick={handleTestRuleEngine}
              className="w-full inline-flex items-center justify-center px-4 py-2 text-xs font-semibold text-primary-foreground bg-primary rounded hover:bg-primary/95 transition-colors gap-2 cursor-pointer"
            >
              <Play className="h-3.5 w-3.5" />
              Evaluate Rules
            </button>

            {/* Test Results Output */}
            {testResult && (
              <div className="mt-4 bg-secondary/50 border border-border p-4 rounded-lg space-y-3">
                <h4 className="text-xs font-bold uppercase text-primary tracking-wider flex items-center gap-1.5">
                  <Info className="h-3.5 w-3.5" />
                  Routing Outcome
                </h4>
                <div className="space-y-1.5 text-xs">
                  <div>
                    <span className="text-[10px] text-muted-foreground block uppercase">Matched Policy</span>
                    <span className="font-bold">{testResult.matchedRule}</span>
                  </div>
                  <div>
                    <span className="text-[10px] text-muted-foreground block uppercase">Gateway Action</span>
                    <span className="font-bold">{testResult.finalAction}</span>
                  </div>
                  {testResult.targetTier && (
                    <div>
                      <span className="text-[10px] text-muted-foreground block uppercase">Resolved Tier</span>
                      <span className="font-bold text-emerald-400 uppercase font-mono">{testResult.targetTier}</span>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Routing Rule Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-lg w-full overflow-hidden">
            <div className="px-6 py-4 border-b border-border">
              <h3 className="text-lg font-bold">{editingRule ? "Edit Routing Rule" : "Create Routing Rule"}</h3>
              <p className="text-xs text-muted-foreground mt-0.5">Specify conditions, actions, and priority evaluations order.</p>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              
              {/* Name */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Rule Name</label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Low risk categorization to Cheap"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.name && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.name}</p>}
              </div>

              {/* Priority */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Priority (Order of matching)</label>
                <input
                  type="number"
                  value={priority}
                  onChange={(e) => setPriority(e.target.value)}
                  placeholder="e.g. 1 (Lowest runs first)"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.priority && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.priority}</p>}
              </div>

              {/* Conditions JSON */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Conditions JSON</label>
                <textarea
                  value={conditionsJson}
                  onChange={(e) => setConditionsJson(e.target.value)}
                  rows={4}
                  placeholder='[{"field":"taskType","operator":"Equals","value":"summarization"}]'
                  className="w-full font-mono text-xs bg-secondary border border-border px-3.5 py-2 rounded text-foreground focus:outline-none focus:border-primary"
                />
                <p className="text-[10px] text-muted-foreground">Supported fields: riskLevel, taskType, complexityScore, containsPii, inputTokens.</p>
                {formErrors.conditionsJson && <p className="text-xs text-red-500 flex items-center gap-1.5 mt-1"><AlertCircle className="h-3.5 w-3.5" />{formErrors.conditionsJson}</p>}
              </div>

              {/* Action */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Outcome Action</label>
                <select
                  value={action}
                  onChange={(e) => setAction(Number(e.target.value))}
                  className="w-full bg-secondary border border-border px-3 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                >
                  <option value={RoutingActionType.RouteToTier}>Route to Model Tier</option>
                  <option value={RoutingActionType.HumanReview}>Human Review Required</option>
                  <option value={RoutingActionType.Block}>Block Request</option>
                </select>
              </div>

              {/* Target Tier */}
              {action === RoutingActionType.RouteToTier && (
                <div className="space-y-1.5 animate-fadeIn">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Target Tier</label>
                  <select
                    value={targetTier === null ? "" : targetTier}
                    onChange={(e) => setTargetTier(e.target.value === "" ? null : Number(e.target.value))}
                    className="w-full bg-secondary border border-border px-3 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  >
                    <option value={ModelTier.Cheap}>Cheap</option>
                    <option value={ModelTier.Standard}>Standard</option>
                    <option value={ModelTier.Premium}>Premium</option>
                  </select>
                </div>
              )}

              {/* Active check */}
              <div className="flex items-center gap-2 py-2">
                <input
                  type="checkbox"
                  id="isActiveRule"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="h-4 w-4 bg-secondary border border-border rounded text-primary focus:ring-primary focus:ring-opacity-50"
                />
                <label htmlFor="isActiveRule" className="text-xs font-semibold uppercase tracking-wider text-foreground cursor-pointer select-none">Rule Enabled & Evaluated</label>
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
                  {editingRule ? "Save Changes" : "Create"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
