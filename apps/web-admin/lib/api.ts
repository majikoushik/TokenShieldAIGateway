import { 
  BudgetScope, 
  BudgetActionType, 
  RoutingActionType, 
  ModelTier, 
  Provider, 
  AiModel, 
  RoutingRule, 
  BudgetLimit, 
  ApiKey, 
  ClientApplication, 
  UsageLog, 
  AuditLog 
} from "@/types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK_API === "true";

// Helper to check if API is available, otherwise default to mock
export async function safeFetch(path: string, options: RequestInit = {}) {
  const url = `${API_BASE_URL}${path}`;
  const headers = new Headers(options.headers);
  
  // x-user-email identifies the actor for audit log purposes in MVP mode.
  // x-tenant-id is NOT set here - the backend resolves it from the seeded demo tenant.
  // In production this will be replaced by Azure Entra ID claims.
  if (!headers.has("x-user-email")) {
    headers.set("x-user-email", "admin@tokenshield.local");
  }
  headers.set("Content-Type", "application/json");

  try {
    const res = await fetch(url, { ...options, headers });
    if (!res.ok) {
      const errBody = await res.json();
      throw new Error(errBody.error || errBody.errors?.join(", ") || `HTTP error ${res.status}`);
    }
    if (res.status === 204) {
      return null;
    }
    return await res.json();
  } catch (error) {
    console.error(`API call to ${path} failed:`, error);
    throw error;
  }
}

// --- MOCK SEED DATA ---
export const MOCK_DATA = {
  providers: [
    { id: "p-1", name: "Mock Provider", apiUrl: "http://localhost:5000/v1/mock", apiKeySecretRef: "kv-secret-provider-mock", isActive: true, createdAtUtc: "2026-06-01T00:00:00Z" },
    { id: "p-2", name: "OpenAI", apiUrl: "https://api.openai.com/v1", apiKeySecretRef: "kv-secret-provider-openai", isActive: true, createdAtUtc: "2026-06-01T00:00:00Z" },
    { id: "p-3", name: "Azure OpenAI", apiUrl: "https://acme-openai.openai.azure.com", apiKeySecretRef: "kv-secret-provider-azure-openai", isActive: true, createdAtUtc: "2026-06-01T00:00:00Z" },
    { id: "p-4", name: "Anthropic", apiUrl: "https://api.anthropic.com/v1", apiKeySecretRef: "kv-secret-provider-anthropic", isActive: false, createdAtUtc: "2026-06-01T00:00:00Z" }
  ] as Provider[],
  models: [
    { id: "m-1", providerId: "p-1", providerName: "Mock Provider", name: "mock-cheap", deploymentName: "mock-cheap-deployment", tier: ModelTier.Cheap, inputTokenPricePerMillion: 0.10, outputTokenPricePerMillion: 0.20, contextWindow: 8192, isActive: true },
    { id: "m-2", providerId: "p-1", providerName: "Mock Provider", name: "mock-standard", deploymentName: "mock-standard-deployment", tier: ModelTier.Standard, inputTokenPricePerMillion: 1.00, outputTokenPricePerMillion: 2.00, contextWindow: 16384, isActive: true },
    { id: "m-3", providerId: "p-1", providerName: "Mock Provider", name: "mock-premium", deploymentName: "mock-premium-deployment", tier: ModelTier.Premium, inputTokenPricePerMillion: 10.00, outputTokenPricePerMillion: 20.00, contextWindow: 32768, isActive: true },
    { id: "m-4", providerId: "p-2", providerName: "OpenAI", name: "gpt-4o-mini", deploymentName: "gpt-4o-mini", tier: ModelTier.Cheap, inputTokenPricePerMillion: 0.150, outputTokenPricePerMillion: 0.600, contextWindow: 128000, isActive: true },
    { id: "m-5", providerId: "p-2", providerName: "OpenAI", name: "gpt-4o", deploymentName: "gpt-4o", tier: ModelTier.Standard, inputTokenPricePerMillion: 2.500, outputTokenPricePerMillion: 10.000, contextWindow: 128000, isActive: true }
  ] as AiModel[],
  rules: [
    { id: "r-1", name: "Low-risk summarization to Cheap", priority: 1, conditionsJson: '[{"field":"riskLevel","operator":"Equals","value":"low"},{"field":"taskType","operator":"Equals","value":"summarization"}]', action: RoutingActionType.RouteToTier, targetTier: ModelTier.Cheap, isActive: true },
    { id: "r-2", name: "High complexity requests to Premium", priority: 2, conditionsJson: '[{"field":"complexityScore","operator":"GreaterThan","value":"80"}]', action: RoutingActionType.RouteToTier, targetTier: ModelTier.Premium, isActive: true },
    { id: "r-3", name: "Block suspicious PII request logs", priority: 3, conditionsJson: '[{"field":"containsPii","operator":"Equals","value":"true"}]', action: RoutingActionType.Block, targetTier: null, isActive: true }
  ] as RoutingRule[],
  budgets: [
    { id: "b-1", scope: BudgetScope.Tenant, targetId: null, targetName: "Tenant Budget", monthlyLimit: 5000.00, warningThresholdPercent: 80.00, currentSpend: 1245.89, action: BudgetActionType.WarnOnly, lastResetAtUtc: "2026-06-01T00:00:00Z" },
    { id: "b-2", scope: BudgetScope.Application, targetId: "app-1", targetName: "Acme Developer Portal", monthlyLimit: 1000.00, warningThresholdPercent: 90.00, currentSpend: 420.50, action: BudgetActionType.Block, lastResetAtUtc: "2026-06-01T00:00:00Z" }
  ] as BudgetLimit[],
  apiKeys: [
    { id: "k-1", clientApplicationId: "app-1", clientApplicationName: "Acme Developer Portal", name: "Acme Primary Dev Key", prefix: "ts_dev_", lastUsedAtUtc: "2026-06-08T22:15:30Z", expiresAtUtc: "2027-06-01T00:00:00Z", isRevoked: false, createdAtUtc: "2026-06-01T00:00:00Z" }
  ] as ApiKey[],
  applications: [
    { id: "app-1", name: "Acme Developer Portal", createdAtUtc: "2026-06-01T00:00:00Z" },
    { id: "app-2", name: "Acme Customer Chatbot", createdAtUtc: "2026-06-03T00:00:00Z" }
  ] as ClientApplication[],
  usageLogs: [
    { id: "log-1", correlationId: "c-1", requestId: "chatcmpl_mock_1", applicationId: "app-1", applicationName: "Acme Developer Portal", promptHash: "8a83fa71...", responseHash: "9a21bde4...", inputTokens: 120, outputTokens: 80, estimatedCost: 0.00025, selectedProvider: "Mock Provider", selectedModel: "mock-cheap", selectedTier: "cheap", matchedRuleName: "Low-risk summarization to Cheap", fallbackUsed: false, budgetStatus: "Within Limits", requestStatus: "Success", latencyMs: 38, createdAtUtc: "2026-06-09T08:00:00Z" },
    { id: "log-2", correlationId: "c-2", requestId: "chatcmpl_mock_2", applicationId: "app-1", applicationName: "Acme Developer Portal", promptHash: "f1a23de4...", responseHash: "e4d29ca1...", inputTokens: 4200, outputTokens: 950, estimatedCost: 0.02100, selectedProvider: "Mock Provider", selectedModel: "mock-premium", selectedTier: "premium", matchedRuleName: "High complexity requests to Premium", fallbackUsed: true, budgetStatus: "Warning", requestStatus: "Success", latencyMs: 120, createdAtUtc: "2026-06-09T07:45:00Z" }
  ] as UsageLog[],
  auditLogs: [
    { id: "audit-1", actionName: "DatabaseSeeded", entityName: "Database", entityId: "00000000-0000-0000-0000-000000000000", actorEmail: "admin@acme.com", detailsJson: '{"message":"Idempotent development seed database initialized successfully."}', createdAtUtc: "2026-06-01T00:00:00Z" }
  ] as AuditLog[]
};

const nowUtc = () => new Date().toISOString();

function requireString(value: string | undefined | null, fieldName: string): string {
  if (!value || value.trim().length === 0) {
    throw new Error(`${fieldName} is required`);
  }
  return value.trim();
}



export const api = {
  // Providers
  async getProviders() {
    if (USE_MOCK) return MOCK_DATA.providers;
    return safeFetch("/api/admin/providers");
  },
  async getProvider(id: string) {
    if (USE_MOCK) return MOCK_DATA.providers.find(p => p.id === id);
    return safeFetch(`/api/admin/providers/${id}`);
  },
  async createProvider(data: Partial<Provider>) {
    if (USE_MOCK) {
      const newProvider: Provider = {
        id: `p-${Date.now()}`,
        name: requireString(data.name, "Provider name"),
        apiUrl: data.apiUrl ?? "",
        apiKeySecretRef: data.apiKeySecretRef ?? "",
        isActive: data.isActive ?? true,
        createdAtUtc: nowUtc(),
        updatedAtUtc: nowUtc(),
      };
      MOCK_DATA.providers.push(newProvider);
      return newProvider;
    }
    return safeFetch("/api/admin/providers", { method: "POST", body: JSON.stringify(data) });
  },
  async updateProvider(id: string, data: Partial<Provider>) {
    if (USE_MOCK) {
      const providerIdx = MOCK_DATA.providers.findIndex(p => p.id === id);
      if (providerIdx !== -1) {
        MOCK_DATA.providers[providerIdx] = { ...MOCK_DATA.providers[providerIdx], ...data };
      }
      return data;
    }
    return safeFetch(`/api/admin/providers/${id}`, { method: "PUT", body: JSON.stringify(data) });
  },
  async deleteProvider(id: string) {
    if (USE_MOCK) {
      const providerIdx = MOCK_DATA.providers.findIndex(p => p.id === id);
      if (providerIdx !== -1) {
        MOCK_DATA.providers[providerIdx].isActive = false;
      }
      return null;
    }
    return safeFetch(`/api/admin/providers/${id}`, { method: "DELETE" });
  },

  // Models
  async getModels() {
    if (USE_MOCK) return MOCK_DATA.models;
    return safeFetch("/api/admin/models");
  },
  async getModel(id: string) {
    if (USE_MOCK) return MOCK_DATA.models.find(m => m.id === id);
    return safeFetch(`/api/admin/models/${id}`);
  },
  async createModel(data: Partial<AiModel>) {
    if (USE_MOCK) {
      const provider = MOCK_DATA.providers.find(p => p.id === data.providerId);
      const newModel: AiModel = {
        id: `m-${Date.now()}`,
        providerId: requireString(data.providerId, "Provider ID"),
        providerName: provider?.name || "Mock",
        name: requireString(data.name, "Model name"),
        deploymentName: data.deploymentName ?? "",
        tier: data.tier ?? ModelTier.Standard,
        inputTokenPricePerMillion: data.inputTokenPricePerMillion ?? 0,
        outputTokenPricePerMillion: data.outputTokenPricePerMillion ?? 0,
        contextWindow: data.contextWindow ?? 0,
        isActive: data.isActive ?? true,
        createdAtUtc: nowUtc()
      };
      MOCK_DATA.models.push(newModel);
      return newModel;
    }
    return safeFetch("/api/admin/models", { method: "POST", body: JSON.stringify(data) });
  },
  async updateModel(id: string, data: Partial<AiModel>) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.models.findIndex(m => m.id === id);
      if (idx !== -1) {
        MOCK_DATA.models[idx] = { ...MOCK_DATA.models[idx], ...data };
      }
      return data;
    }
    return safeFetch(`/api/admin/models/${id}`, { method: "PUT", body: JSON.stringify(data) });
  },
  async deleteModel(id: string) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.models.findIndex(m => m.id === id);
      if (idx !== -1) {
        MOCK_DATA.models[idx].isActive = false;
      }
      return null;
    }
    return safeFetch(`/api/admin/models/${id}`, { method: "DELETE" });
  },

  // Routing Rules
  async getRules() {
    if (USE_MOCK) return MOCK_DATA.rules;
    return safeFetch("/api/admin/routing-rules");
  },
  async getRule(id: string) {
    if (USE_MOCK) return MOCK_DATA.rules.find(r => r.id === id);
    return safeFetch(`/api/admin/routing-rules/${id}`);
  },
  async createRule(data: Partial<RoutingRule>) {
    if (USE_MOCK) {
      const newRule: RoutingRule = {
        id: `r-${Date.now()}`,
        name: requireString(data.name, "Routing rule name"),
        priority: data.priority ?? 100,
        conditionsJson: data.conditionsJson ?? "{}",
        action: data.action ?? RoutingActionType.RouteToTier,
        targetTier: data.targetTier ?? null,
        isActive: data.isActive ?? true,
        createdAtUtc: nowUtc()
      };
      MOCK_DATA.rules.push(newRule);
      return newRule;
    }
    return safeFetch("/api/admin/routing-rules", { method: "POST", body: JSON.stringify(data) });
  },
  async updateRule(id: string, data: Partial<RoutingRule>) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.rules.findIndex(r => r.id === id);
      if (idx !== -1) {
        MOCK_DATA.rules[idx] = { ...MOCK_DATA.rules[idx], ...data };
      }
      return data;
    }
    return safeFetch(`/api/admin/routing-rules/${id}`, { method: "PUT", body: JSON.stringify(data) });
  },
  async deleteRule(id: string) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.rules.findIndex(r => r.id === id);
      if (idx !== -1) {
        MOCK_DATA.rules.splice(idx, 1);
      }
      return null;
    }
    return safeFetch(`/api/admin/routing-rules/${id}`, { method: "DELETE" });
  },

  // Budgets
  async getBudgets() {
    if (USE_MOCK) return MOCK_DATA.budgets;
    return safeFetch("/api/admin/budgets");
  },
  async getBudget(id: string) {
    if (USE_MOCK) return MOCK_DATA.budgets.find(b => b.id === id);
    return safeFetch(`/api/admin/budgets/${id}`);
  },
  async createBudget(data: Partial<BudgetLimit>) {
    if (USE_MOCK) {
      let targetName = "Acme Developer Portal";
      if (data.scope === BudgetScope.Model) targetName = "mock-cheap";
      const newBudget: BudgetLimit = {
        id: `b-${Date.now()}`,
        scope: data.scope ?? BudgetScope.Tenant,
        targetId: data.targetId ?? null,
        targetName: data.targetName ?? targetName,
        monthlyLimit: data.monthlyLimit ?? 0,
        warningThresholdPercent: data.warningThresholdPercent ?? 80,
        currentSpend: data.currentSpend ?? 0,
        action: data.action ?? BudgetActionType.WarnOnly,
        lastResetAtUtc: data.lastResetAtUtc ?? nowUtc(),
        createdAtUtc: nowUtc()
      };
      MOCK_DATA.budgets.push(newBudget);
      return newBudget;
    }
    return safeFetch("/api/admin/budgets", { method: "POST", body: JSON.stringify(data) });
  },
  async updateBudget(id: string, data: Partial<BudgetLimit>) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.budgets.findIndex(b => b.id === id);
      if (idx !== -1) {
        MOCK_DATA.budgets[idx] = { ...MOCK_DATA.budgets[idx], ...data };
      }
      return data;
    }
    return safeFetch(`/api/admin/budgets/${id}`, { method: "PUT", body: JSON.stringify(data) });
  },
  async deleteBudget(id: string) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.budgets.findIndex(b => b.id === id);
      if (idx !== -1) {
        MOCK_DATA.budgets.splice(idx, 1);
      }
      return null;
    }
    return safeFetch(`/api/admin/budgets/${id}`, { method: "DELETE" });
  },

  // API Keys
  async getApiKeys() {
    if (USE_MOCK) return MOCK_DATA.apiKeys;
    return safeFetch("/api/admin/api-keys");
  },
  async createApiKey(data: Partial<ApiKey>) {
    if (USE_MOCK) {
      const app = MOCK_DATA.applications.find(a => a.id === data.clientApplicationId);
      const newKey = {
        id: `k-${Date.now()}`,
        clientApplicationId: requireString(data.clientApplicationId, "Client application ID"),
        clientApplicationName: app?.name || "Acme Developer Portal",
        name: requireString(data.name, "API Key name"),
        prefix: data.prefix ?? "ts_live_",
        rawKey: `${data.prefix ?? "ts_live_"}mockrawkey_${Math.random().toString(36).substring(2, 15)}_${Math.random().toString(36).substring(2, 15)}`,
        lastUsedAtUtc: null,
        expiresAtUtc: data.expiresAtUtc ?? null,
        isRevoked: false,
        createdAtUtc: nowUtc()
      };
      MOCK_DATA.apiKeys.push({
        id: newKey.id,
        clientApplicationId: newKey.clientApplicationId,
        clientApplicationName: newKey.clientApplicationName,
        name: newKey.name,
        prefix: newKey.prefix,
        lastUsedAtUtc: newKey.lastUsedAtUtc,
        expiresAtUtc: newKey.expiresAtUtc,
        isRevoked: newKey.isRevoked,
        createdAtUtc: newKey.createdAtUtc
      });
      return newKey;
    }
    return safeFetch("/api/admin/api-keys", { method: "POST", body: JSON.stringify(data) });
  },
  async revokeApiKey(id: string) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.apiKeys.findIndex(k => k.id === id);
      if (idx !== -1) {
        MOCK_DATA.apiKeys[idx].isRevoked = true;
      }
      return null;
    }
    return safeFetch(`/api/admin/api-keys/${id}/revoke`, { method: "POST" });
  },
  async deleteApiKey(id: string) {
    if (USE_MOCK) {
      const idx = MOCK_DATA.apiKeys.findIndex(k => k.id === id);
      if (idx !== -1) {
        MOCK_DATA.apiKeys.splice(idx, 1);
      }
      return null;
    }
    return safeFetch(`/api/admin/api-keys/${id}`, { method: "DELETE" });
  },

  // Usage Analytics
  async getUsageLogs(filters: Record<string, string | number | undefined> = {}) {
    if (USE_MOCK) {
      return MOCK_DATA.usageLogs;
    }
    const params = new URLSearchParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val !== undefined && val !== "") params.append(key, val.toString());
    });
    return safeFetch(`/api/admin/usage-analytics/logs?${params.toString()}`);
  },

  async getUsageSummary(filters: Record<string, string | undefined> = {}) {
    if (USE_MOCK) {
      return this.getMockSummary();
    }
    const params = new URLSearchParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val !== undefined && val !== "") params.append(key, val.toString());
    });
    return safeFetch(`/api/admin/usage-analytics/summary?${params.toString()}`);
  },

  // Audit Logs
  async getAuditLogs(filters: Record<string, string | number | undefined> = {}) {
    if (USE_MOCK) {
      return MOCK_DATA.auditLogs;
    }
    const params = new URLSearchParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val !== undefined && val !== "") params.append(key, val.toString());
    });
    return safeFetch(`/api/admin/audit-logs?${params.toString()}`);
  },

  // Settings / Applications
  async getCatalog() {
    if (USE_MOCK) {
      return {
        tiers: ["cheap", "standard", "premium"],
        budgetScopes: ["tenant", "application", "apikey", "model"],
        budgetActions: ["warnonly", "block", "downgrade"],
        routingActions: ["routetotier", "humanreview", "block"]
      };
    }
    return safeFetch("/api/admin/settings/catalog");
  },

  async getApplications() {
    if (USE_MOCK) return MOCK_DATA.applications;
    return safeFetch("/api/admin/settings/applications");
  },

  async createApplication(data: Partial<ClientApplication>) {
    if (USE_MOCK) {
      const newApp: ClientApplication = {
        id: `app-${Date.now()}`,
        name: requireString(data.name, "Client application name"),
        createdAtUtc: nowUtc()
      };
      MOCK_DATA.applications.push(newApp);
      return newApp;
    }
    return safeFetch("/api/admin/settings/applications", { method: "POST", body: JSON.stringify(data) });
  },

  getMockSummary() {
    return {
      totalCost: 1245.89,
      totalRequests: 348290,
      totalInputTokens: 154890200,
      totalOutputTokens: 38290100,
      averageLatencyMs: 145.2,
      costByProvider: [
        { groupKey: "Mock Provider", cost: 120.50, requestCount: 32000 },
        { groupKey: "OpenAI", cost: 745.20, requestCount: 210000 },
        { groupKey: "Azure OpenAI", cost: 380.19, requestCount: 106290 }
      ],
      costByModel: [
        { groupKey: "mock-cheap", cost: 30.50, requestCount: 20000 },
        { groupKey: "mock-standard", cost: 90.00, requestCount: 12000 },
        { groupKey: "gpt-4o-mini", cost: 420.10, requestCount: 180000 },
        { groupKey: "gpt-4o", cost: 325.10, requestCount: 30000 },
        { groupKey: "deploy-gpt-4o", cost: 380.19, requestCount: 106290 }
      ],
      costByTier: [
        { groupKey: "cheap", cost: 450.60, requestCount: 200000 },
        { groupKey: "standard", cost: 795.29, requestCount: 148290 }
      ],
      requestByStatus: [
        { groupKey: "Success", count: 348120 },
        { groupKey: "Failed", count: 120 },
        { groupKey: "Blocked", count: 50 }
      ],
      requestByBudgetState: [
        { groupKey: "Within Limits", count: 345000 },
        { groupKey: "Warning", count: 3200 },
        { groupKey: "Exceeded", count: 90 }
      ]
    };
  }
};
