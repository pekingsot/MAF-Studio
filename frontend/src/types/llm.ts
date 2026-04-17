export interface LLMConfig {
  id: number;
  name: string;
  provider: string;
  apiKey?: string;
  endpoint?: string;
  defaultModel?: string;
  isDefault: boolean;
  isEnabled: boolean;
  createdAt: string;
  updatedAt?: string;
  models: LLMModelConfig[];
}

export interface LLMModelConfig {
  id: number;
  modelName: string;
  displayName?: string;
  description?: string;
  temperature: number;
  maxTokens: number;
  contextWindow: number;
  topP?: number;
  frequencyPenalty?: number;
  presencePenalty?: number;
  stopSequences?: string;
  isDefault: boolean;
  isEnabled: boolean;
  sortOrder: number;
  lastTestTime?: string;
  availabilityStatus: number;
  testResult?: string;
  createdAt: string;
}

export interface LlmConfigInfo {
  llmConfigId: number;
  llmConfigName: string;
  llmModelConfigId?: number;
  modelName: string;
  isPrimary: boolean;
  priority: number;
  isValid: boolean;
  lastChecked?: string;
  msg: string;
}

export interface SystemConfig {
  id: number;
  key: string;
  value: string;
  description?: string;
  group?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface ConnectionStatus {
  success: boolean;
  message: string;
  latencyMs: number;
}

export interface ProviderInfo {
  id: string;
  displayName: string;
  defaultEndpoint: string;
  defaultModel: string;
}

export const PROVIDER_COLORS: Record<string, string> = {
  qwen: '#ff6a00',
  openai: '#10a37f',
  deepseek: '#0066ff',
  zhipu: '#1e88e5',
  anthropic: '#d97706',
  openai_compatible: '#6b7280',
};
