import { 
  ProfilerRule, 
  CreateProfilerRuleRequest, 
  UpdateProfilerRuleRequest, 
  TestProfilerRuleRequest, 
  TestProfilerRuleResponse 
} from '@/types/profiler-rules';
import { apiClient } from './client';

export const profilerRulesApi = {
  getRules: async (): Promise<ProfilerRule[]> => {
    const response = await apiClient.get('/admin/profilerrules');
    return response.data;
  },

  getRule: async (id: string): Promise<ProfilerRule> => {
    const response = await apiClient.get(`/admin/profilerrules/${id}`);
    return response.data;
  },

  createRule: async (request: CreateProfilerRuleRequest): Promise<ProfilerRule> => {
    const response = await apiClient.post('/admin/profilerrules', request);
    return response.data;
  },

  updateRule: async (id: string, request: UpdateProfilerRuleRequest): Promise<void> => {
    await apiClient.put(`/admin/profilerrules/${id}`, request);
  },

  deleteRule: async (id: string): Promise<void> => {
    await apiClient.delete(`/admin/profilerrules/${id}`);
  },

  testRule: async (request: TestProfilerRuleRequest): Promise<TestProfilerRuleResponse> => {
    const response = await apiClient.post('/admin/profilerrules/test', request);
    return response.data;
  }
};
