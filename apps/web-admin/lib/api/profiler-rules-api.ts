import { 
  ProfilerRule, 
  CreateProfilerRuleRequest, 
  UpdateProfilerRuleRequest, 
  TestProfilerRuleRequest, 
  TestProfilerRuleResponse 
} from '@/types/profiler-rules';
import { safeFetch } from '../api';

export const profilerRulesApi = {
  getRules: async (): Promise<ProfilerRule[]> => {
    return safeFetch('/api/admin/profilerrules');
  },

  getRule: async (id: string): Promise<ProfilerRule> => {
    return safeFetch(`/api/admin/profilerrules/${id}`);
  },

  createRule: async (request: CreateProfilerRuleRequest): Promise<ProfilerRule> => {
    return safeFetch('/api/admin/profilerrules', { method: 'POST', body: JSON.stringify(request) });
  },

  updateRule: async (id: string, request: UpdateProfilerRuleRequest): Promise<void> => {
    await safeFetch(`/api/admin/profilerrules/${id}`, { method: 'PUT', body: JSON.stringify(request) });
  },

  deleteRule: async (id: string): Promise<void> => {
    await safeFetch(`/api/admin/profilerrules/${id}`, { method: 'DELETE' });
  },

  testRule: async (request: TestProfilerRuleRequest): Promise<TestProfilerRuleResponse> => {
    return safeFetch('/api/admin/profilerrules/test', { method: 'POST', body: JSON.stringify(request) });
  }
};
