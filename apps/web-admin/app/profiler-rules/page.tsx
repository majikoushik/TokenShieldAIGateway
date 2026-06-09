/* eslint-disable @typescript-eslint/no-explicit-any, @typescript-eslint/no-unused-vars */
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { profilerRulesApi } from "@/lib/api/profiler-rules-api";
import { useState } from "react";
import { z } from "zod";
import { Activity, Plus, Edit2, Trash2, CheckCircle2, AlertCircle, Play, Info } from "lucide-react";
import { ProfilerRule } from "@/types/profiler-rules";

const ruleSchema = z.object({
  name: z.string().min(1, "Rule name is required"),
  targetTaskType: z.string().min(1, "Target task type is required"),
  phrases: z.string().optional(),
  regexPatterns: z.string().optional(),
  confidence: z.coerce.number().min(0).max(1),
  priority: z.coerce.number().int().min(0)
});

export default function ProfilerRulesPage() {
  const queryClient = useQueryClient();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<ProfilerRule | null>(null);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Form States
  const [name, setName] = useState("");
  const [targetTaskType, setTargetTaskType] = useState("");
  const [phrases, setPhrases] = useState("");
  const [regexPatterns, setRegexPatterns] = useState("");
  const [confidence, setConfidence] = useState("0.8");
  const [priority, setPriority] = useState("1");
  const [isActive, setIsActive] = useState(true);

  // Test Panel States
  const [testPrompt, setTestPrompt] = useState("");
  const [testResult, setTestResult] = useState<{ isMatch: boolean; matchReason: string; targetTaskType: string; confidence: number } | null>(null);

  const { data: rules, isLoading } = useQuery<ProfilerRule[]>({
    queryKey: ["profilerRules"],
    queryFn: () => profilerRulesApi.getRules()
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => profilerRulesApi.createRule(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["profilerRules"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => profilerRulesApi.updateRule(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["profilerRules"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => profilerRulesApi.deleteRule(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["profilerRules"] });
    }
  });

  const testMutation = useMutation({
    mutationFn: (data: any) => profilerRulesApi.testRule(data),
    onSuccess: (res) => {
      setTestResult(res);
    }
  });

  const openCreateModal = () => {
    setEditingRule(null);
    setName("");
    setTargetTaskType("");
    setPhrases("");
    setRegexPatterns("");
    setConfidence("0.8");
    setPriority((rules ? rules.length + 1 : 1).toString());
    setIsActive(true);
    setFormErrors({});
    setIsModalOpen(true);
  };

  const openEditModal = (rule: ProfilerRule) => {
    setEditingRule(rule);
    setName(rule.name);
    setTargetTaskType(rule.targetTaskType);
    setPhrases(rule.phrases.join("\n"));
    setRegexPatterns(rule.regexPatterns.join("\n"));
    setConfidence(rule.confidence.toString());
    setPriority(rule.priority.toString());
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
      targetTaskType,
      phrases,
      regexPatterns,
      confidence: Number(confidence),
      priority: Number(priority)
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
      name,
      targetTaskType,
      phrases: phrases.split("\n").map(s => s.trim()).filter(s => s.length > 0),
      regexPatterns: regexPatterns.split("\n").map(s => s.trim()).filter(s => s.length > 0),
      confidence: Number(confidence),
      priority: Number(priority),
      isActive
    };

    if (editingRule) {
      updateMutation.mutate({ id: editingRule.id, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleDelete = (id: string) => {
    if (confirm("Are you sure you want to delete this profiler rule?")) {
      deleteMutation.mutate(id);
    }
  };

  const handleTestRule = () => {
    if (!testPrompt) return;
    
    // We can test locally with the API
    const testData = {
      prompt: testPrompt,
      targetTaskType: targetTaskType || "test_task",
      phrases: phrases.split("\n").map(s => s.trim()).filter(s => s.length > 0),
      regexPatterns: regexPatterns.split("\n").map(s => s.trim()).filter(s => s.length > 0),
      confidence: Number(confidence) || 0.8
    };

    testMutation.mutate(testData);
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
          <h1 className="text-2xl font-bold tracking-tight">Profiler Rules</h1>
          <p className="text-sm text-muted-foreground">Manage classification rules used by the production profiler to infer taskType and riskLevel from prompts.</p>
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
                
                {/* Target info */}
                <div className="text-xs text-muted-foreground pl-9 flex items-center gap-2">
                  <span className="font-semibold text-foreground">TaskType Output:</span>
                  <span className="font-bold font-mono text-primary bg-primary/10 px-1.5 py-0.5 rounded">{rule.targetTaskType}</span>
                  <span className="text-muted-foreground ml-2">Confidence: {(rule.confidence * 100).toFixed(0)}%</span>
                </div>

                {/* Phrase/Regex info */}
                <div className="text-xs text-muted-foreground pl-9">
                  <span className="font-semibold text-foreground block">Match Criteria:</span>
                  <div className="mt-1 space-y-1">
                    {rule.phrases.length > 0 && (
                      <p className="bg-secondary/50 p-1.5 rounded border border-border">
                        <span className="font-bold">Phrases ({rule.phrases.length}):</span> {rule.phrases.join(', ')}
                      </p>
                    )}
                    {rule.regexPatterns.length > 0 && (
                      <p className="bg-secondary/50 p-1.5 rounded border border-border font-mono text-[10px]">
                        <span className="font-bold">Regex ({rule.regexPatterns.length}):</span> {rule.regexPatterns.join(', ')}
                      </p>
                    )}
                  </div>
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
              <Activity className="h-10 w-10 text-muted-foreground mx-auto" />
              <h3 className="font-bold text-foreground">No profiler rules created</h3>
              <p className="text-sm text-muted-foreground">Requests will be classified by default rules or fallback types.</p>
            </div>
          )}
        </div>

        {/* Local Test Simulator Panel (Right column) */}
        <div className="bg-card border border-border rounded-lg p-6 space-y-4 sticky top-6">
          <div className="flex items-center gap-2 border-b border-border pb-3">
            <Play className="h-4 w-4 text-primary" />
            <h3 className="font-bold text-sm uppercase tracking-wider">Profiler Simulator</h3>
          </div>
          <p className="text-xs text-muted-foreground">Test how the currently entered rule in the modal would classify a raw user prompt.</p>
          
          <div className="space-y-4 pt-2">
            {/* Prompt input */}
            <div className="space-y-1">
              <label className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground">User Prompt (Memory Only)</label>
              <textarea
                rows={4}
                value={testPrompt}
                onChange={(e) => setTestPrompt(e.target.value)}
                placeholder="Enter a prompt to see if the configured phrases or regex match it..."
                className="w-full bg-secondary border border-border px-3 py-2 rounded text-xs focus:outline-none focus:border-primary"
              />
            </div>

            {/* Run button */}
            <button
              onClick={handleTestRule}
              disabled={testMutation.isPending || !testPrompt || (!phrases && !regexPatterns)}
              className="w-full inline-flex items-center justify-center px-4 py-2 text-xs font-semibold text-primary-foreground bg-primary rounded hover:bg-primary/95 transition-colors gap-2 cursor-pointer disabled:opacity-50"
            >
              <Play className="h-3.5 w-3.5" />
              {testMutation.isPending ? "Evaluating..." : "Evaluate Current Rule"}
            </button>

            {/* Test Results Output */}
            {testResult && (
              <div className={`mt-4 border p-4 rounded-lg space-y-3 ${testResult.isMatch ? "bg-emerald-500/10 border-emerald-500/20" : "bg-zinc-500/10 border-zinc-500/20"}`}>
                <h4 className={`text-xs font-bold uppercase tracking-wider flex items-center gap-1.5 ${testResult.isMatch ? "text-emerald-500" : "text-muted-foreground"}`}>
                  {testResult.isMatch ? <CheckCircle2 className="h-4 w-4" /> : <Info className="h-4 w-4" />}
                  {testResult.isMatch ? "Match Found" : "No Match"}
                </h4>
                <div className="space-y-1.5 text-xs">
                  <div>
                    <span className="text-[10px] text-muted-foreground block uppercase">Reason</span>
                    <span className="font-semibold text-foreground">{testResult.matchReason}</span>
                  </div>
                  {testResult.isMatch && (
                    <>
                      <div>
                        <span className="text-[10px] text-muted-foreground block uppercase">Assigned TaskType</span>
                        <span className="font-bold text-primary">{testResult.targetTaskType}</span>
                      </div>
                      <div>
                        <span className="text-[10px] text-muted-foreground block uppercase">Confidence</span>
                        <span className="font-bold text-emerald-400">{(testResult.confidence * 100).toFixed(0)}%</span>
                      </div>
                    </>
                  )}
                </div>
              </div>
            )}
            {!phrases && !regexPatterns && (
              <p className="text-[10px] text-amber-500">Configure phrases or regex in the creation modal before testing.</p>
            )}
          </div>
        </div>
      </div>

      {/* Profiler Rule Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card border border-border rounded-lg shadow-lg max-w-lg w-full overflow-hidden">
            <div className="px-6 py-4 border-b border-border">
              <h3 className="text-lg font-bold">{editingRule ? "Edit Profiler Rule" : "Create Profiler Rule"}</h3>
              <p className="text-xs text-muted-foreground mt-0.5">Define text matching rules to classify prompts automatically.</p>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
              
              {/* Name */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Rule Name</label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Detect Translation"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                />
                {formErrors.name && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.name}</p>}
              </div>

              {/* TargetTaskType */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Target Task Type</label>
                <input
                  type="text"
                  value={targetTaskType}
                  onChange={(e) => setTargetTaskType(e.target.value)}
                  placeholder="e.g. translation"
                  className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground font-mono focus:outline-none focus:border-primary"
                />
                {formErrors.targetTaskType && <p className="text-xs text-red-500 flex items-center gap-1 mt-1"><AlertCircle className="h-3 w-3" />{formErrors.targetTaskType}</p>}
              </div>

              <div className="grid grid-cols-2 gap-4">
                {/* Confidence */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Confidence (0.0 - 1.0)</label>
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    max="1"
                    value={confidence}
                    onChange={(e) => setConfidence(e.target.value)}
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                </div>

                {/* Priority */}
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Priority</label>
                  <input
                    type="number"
                    value={priority}
                    onChange={(e) => setPriority(e.target.value)}
                    placeholder="e.g. 10"
                    className="w-full bg-secondary border border-border px-3.5 py-2 rounded text-sm text-foreground focus:outline-none focus:border-primary"
                  />
                </div>
              </div>

              {/* Phrases */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Phrases (One per line)</label>
                <textarea
                  value={phrases}
                  onChange={(e) => setPhrases(e.target.value)}
                  rows={3}
                  placeholder="translate to&#10;in spanish"
                  className="w-full text-sm bg-secondary border border-border px-3.5 py-2 rounded text-foreground focus:outline-none focus:border-primary"
                />
              </div>

              {/* Regex */}
              <div className="space-y-1.5">
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Regex Patterns (One per line)</label>
                <textarea
                  value={regexPatterns}
                  onChange={(e) => setRegexPatterns(e.target.value)}
                  rows={3}
                  placeholder="\b(translate|translation)\b"
                  className="w-full font-mono text-sm bg-secondary border border-border px-3.5 py-2 rounded text-foreground focus:outline-none focus:border-primary"
                />
              </div>

              {/* Active check */}
              <div className="flex items-center gap-2 py-2">
                <input
                  type="checkbox"
                  id="isActiveProfiler"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="h-4 w-4 bg-secondary border border-border rounded text-primary focus:ring-primary focus:ring-opacity-50"
                />
                <label htmlFor="isActiveProfiler" className="text-xs font-semibold uppercase tracking-wider text-foreground cursor-pointer select-none">Rule Enabled</label>
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
