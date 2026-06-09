export interface ProfilerRule {
  id: string;
  name: string;
  targetTaskType: string;
  phrases: string[];
  regexPatterns: string[];
  confidence: number;
  priority: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateProfilerRuleRequest {
  name: string;
  targetTaskType: string;
  phrases: string[];
  regexPatterns: string[];
  confidence: number;
  priority: number;
  isActive: boolean;
}

export interface UpdateProfilerRuleRequest {
  name: string;
  targetTaskType: string;
  phrases: string[];
  regexPatterns: string[];
  confidence: number;
  priority: number;
  isActive: boolean;
}

export interface TestProfilerRuleRequest {
  prompt: string;
  targetTaskType: string;
  phrases: string[];
  regexPatterns: string[];
  confidence: number;
}

export interface TestProfilerRuleResponse {
  isMatch: boolean;
  matchReason: string;
  targetTaskType: string;
  confidence: number;
}
