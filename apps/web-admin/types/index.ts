export enum ModelTier {
  Cheap = 0,
  Standard = 1,
  Premium = 2
}

export enum RoutingActionType {
  RouteToTier = 0,
  HumanReview = 1,
  Block = 2
}

export enum BudgetScope {
  Tenant = 0,
  Application = 1,
  ApiKey = 2,
  Model = 3
}

export enum BudgetActionType {
  WarnOnly = 0,
  Block = 1,
  Downgrade = 2
}

export interface Provider {
  id: string;
  name: string;
  apiUrl: string;
  apiKeySecretRef: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface AiModel {
  id: string;
  providerId: string;
  providerName: string;
  name: string;
  deploymentName: string;
  tier: ModelTier;
  inputTokenPricePerMillion: number;
  outputTokenPricePerMillion: number;
  contextWindow: number;
  isActive: boolean;
  createdAtUtc?: string;
}

export interface RoutingRule {
  id: string;
  name: string;
  priority: number;
  conditionsJson: string;
  action: RoutingActionType;
  targetTier: ModelTier | null;
  isActive: boolean;
  createdAtUtc?: string;
}

export interface BudgetLimit {
  id: string;
  scope: BudgetScope;
  targetId: string | null;
  targetName?: string;
  monthlyLimit: number;
  warningThresholdPercent: number;
  currentSpend: number;
  action: BudgetActionType;
  lastResetAtUtc: string;
  createdAtUtc?: string;
}

export interface ApiKey {
  id: string;
  clientApplicationId: string;
  clientApplicationName: string;
  name: string;
  prefix: string;
  lastUsedAtUtc: string | null;
  expiresAtUtc: string | null;
  isRevoked: boolean;
  createdAtUtc: string;
}

export interface ClientApplication {
  id: string;
  name: string;
  createdAtUtc: string;
}

export interface UsageLog {
  id: string;
  correlationId: string;
  requestId: string;
  applicationId: string;
  applicationName: string;
  promptHash: string;
  responseHash: string;
  inputTokens: number;
  outputTokens: number;
  estimatedCost: number;
  selectedProvider: string;
  selectedModel: string;
  selectedTier: string;
  matchedRuleName: string | null;
  fallbackUsed: boolean;
  budgetStatus: string;
  requestStatus: string;
  latencyMs: number;
  createdAtUtc: string;
}

export interface AuditLog {
  id: string;
  actionName: string;
  entityName: string;
  entityId: string;
  actorEmail: string;
  detailsJson: string;
  createdAtUtc: string;
}
