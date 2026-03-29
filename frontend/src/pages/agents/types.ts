import { Agent, AgentType, FallbackModel } from '../../services/agentService';
import { AgentRuntimeStatus } from '../../services/agentRuntimeService';

export interface LLMConfig {
  id: number;
  name: string;
  provider: string;
  models?: LlmModel[];
  endpoint?: string;
  isEnabled: boolean;
  isDefault: boolean;
}

export interface LlmModel {
  id: number;
  modelName: string;
  displayName?: string;
  description?: string;
  temperature: number;
  maxTokens: number;
  contextWindow: number;
  isDefault: boolean;
  isEnabled: boolean;
  sortOrder: number;
}

export interface SelectedModel {
  llmConfigId: number;
  llmConfigName: string;
  llmModelConfigId: number;
  modelName: string;
  provider: string;
}

export interface AgentFormData {
  name: string;
  description?: string;
  type: string;
  avatar?: string;
  systemPrompt?: string;
  llmConfigId: number;
  llmModelConfigId: number;
  fallbackModels?: FallbackModelRequest[];
}

export interface FallbackModelRequest {
  llmConfigId: number;
  llmModelConfigId: number;
  priority: number;
}

export interface AgentTableProps {
  agents: Agent[];
  agentTypes: AgentType[];
  llmConfigs: LLMConfig[];
  runtimeStatuses: Record<string, AgentRuntimeStatus>;
  loading: boolean;
  testingAgent: number | null;
  activatingAgent: number | null;
  onEdit: (agent: Agent) => void;
  onDelete: (id: number) => void;
  onActivate: (id: number) => void;
  onSleep: (id: number) => void;
  onDestroy: (id: number) => void;
  onTest: (id: number) => void;
}

export interface AgentFormModalProps {
  visible: boolean;
  editingAgent: Agent | null;
  agentTypes: AgentType[];
  llmConfigs: LLMConfig[];
  onCancel: () => void;
  onSubmit: (data: AgentFormData) => Promise<void>;
}

export const AVATAR_OPTIONS = [
  '🤖', '🧠', '💻', '🎯', '📊', '🔬', '🚀', '⚡', '🌟', '🎨',
  '🦾', '🤝', '🔮', '💡', '🎭', '🦸', '🌈', '🎪', '🎠', '🎡'
];

export const AGENT_STATUS_MAP: Record<string, { color: string; label: string }> = {
  Active: { color: 'green', label: '活跃' },
  Inactive: { color: 'default', label: '未激活' },
  Busy: { color: 'orange', label: '忙碌' },
  Error: { color: 'red', label: '错误' },
};

export const RUNTIME_STATE_MAP: Record<string, { color: string; label: string }> = {
  Uninitialized: { color: 'default', label: '未初始化' },
  Ready: { color: 'green', label: '就绪' },
  Busy: { color: 'orange', label: '忙碌' },
  Sleeping: { color: 'purple', label: '休眠' },
  Error: { color: 'red', label: '错误' },
};
