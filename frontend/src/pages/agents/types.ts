import { Agent, AgentType } from '../../services/agentService';
import { AgentRuntimeStatus } from '../../services/agentRuntimeService';
import type { LLMConfig, LlmConfigInfo } from '../../types/llm';

export type { LLMConfig, LlmConfigInfo };

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
  lastTestTime?: string;
  availabilityStatus: number;
  testResult?: string;
}

export interface SelectedModel {
  llmConfigId: number;
  llmConfigName: string;
  llmModelConfigId: number;
  modelName: string;
  provider: string;
  isPrimary?: boolean;
}

export interface AgentFormData {
  name: string;
  description?: string;
  type: string;
  avatar?: string;
  systemPrompt?: string;
  llmConfigId: number;
  llmModelConfigId: number;
  llmConfigs?: string;
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
  // 人物-活动（男女交替）
  '🧘', '🧘‍♀️', '🧗', '🧗‍♀️', '🏊', '🏊‍♀️', '🏃', '🏃‍♀️', '💃', '🕺', '👯', '👯‍♀️',
  // 人物-角色（男女交替）
  '🦸', '🦸‍♀️', '🧙', '🧙‍♀️', '🧛', '🧛‍♀️', '🧜', '🧜‍♀️', '🧚', '🧚‍♀️',
  // 动物
  '🦊', '🐱', '🐶', '🦁', '🐯', '🐻', '🐼', '🐨', '🦄', '🐲',
  '🦋', '🦅', '🐬', '🐳', '🦈', '🐙', '🦀', '🐢', '🦎', '🐍',
  // 植物
  '🌸', '🌺', '🌻', '🌹', '🍀', '🌴', '🎄', '🌲', '⛰️', '🌊',
  // 汽车
  '🚗', '🚕', '🚙', '🚌', '🚎', '🏎️', '🚓', '🚑', '🚒', '🚐',
  // 自然元素
  '🔥', '❄️', '💧', '🌪️', '☄️', '🌙', '☀️', '⭐', '💫', '✨', '🌈',
  // 机器人/科技
  '🤖', '🧠', '💻', '🔬', '🚀', '⚡', '🌟', '🦾', '🔮', '💡',
  // 表演娱乐
  '🎭', '🎪', '🎠', '🎡',
  // 音乐游戏
  '🎮', '🎲', '🎸', '🎺', '🎻', '🎹', '🥁', '🎤', '🎧', '🕹️',
  // 文具
  '📚', '📖', '📝', '✏️', '🖊️', '📎', '📌', '📍', '🔖', '🏷️',
  // 工具武器
  '🛠️', '🔧', '🔨', '⚒️', '🛡️', '⚔️', '🏹', '🎯', '📊', '🎨',
  // 其他
  '🤝'
];

export const AGENT_STATUS_MAP: Record<string, { color: string; label: string }> = {
  Active: { color: 'green', label: '活跃' },
  Inactive: { color: 'default', label: '未激活' },
  Busy: { color: 'orange', label: '忙碌' },
  Error: { color: 'red', label: '错误' },
};

export const RUNTIME_STATE_MAP: Record<string, { color: string; label: string }> = {
  Uninitialized: { color: 'default', label: '未初始化' },
  Ready: { color: 'green', label: '可用' },
  Busy: { color: 'orange', label: '忙碌' },
  Error: { color: 'red', label: '错误' },
};
