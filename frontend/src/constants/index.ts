export const AGENT_STATUS = {
  Active: { value: 'Active', label: '活跃', color: 'green' },
  Inactive: { value: 'Inactive', label: '未激活', color: 'default' },
  Busy: { value: 'Busy', label: '忙碌', color: 'orange' },
  Error: { value: 'Error', label: '错误', color: 'red' },
} as const;

export const RUNTIME_STATE = {
  Uninitialized: { value: 'Uninitialized', label: '未初始化', color: 'default' },
  Ready: { value: 'Ready', label: '就绪', color: 'green' },
  Busy: { value: 'Busy', label: '忙碌', color: 'orange' },
  Sleeping: { value: 'Sleeping', label: '休眠', color: 'purple' },
  Error: { value: 'Error', label: '错误', color: 'red' },
} as const;

export const COLLABORATION_STATUS = {
  Pending: { value: 'Pending', label: '待处理', color: 'default' },
  Running: { value: 'Running', label: '运行中', color: 'blue' },
  Completed: { value: 'Completed', label: '已完成', color: 'green' },
  Failed: { value: 'Failed', label: '失败', color: 'red' },
  Cancelled: { value: 'Cancelled', label: '已取消', color: 'default' },
} as const;

export const AVATAR_OPTIONS = [
  '🤖', '🧠', '💻', '🎯', '📊', '🔬', '🚀', '⚡', '🌟', '🎨',
  '🦾', '🤝', '🔮', '💡', '🎭', '🦸', '🌈', '🎪', '🎠', '🎡',
];

export const LLM_PROVIDERS = [
  { value: 'OpenAI', label: 'OpenAI' },
  { value: 'Azure', label: 'Azure OpenAI' },
  { value: 'Alibaba', label: '阿里千问' },
  { value: 'Zhipu', label: '智谱AI' },
  { value: 'Baidu', label: '百度文心' },
  { value: 'Tencent', label: '腾讯混元' },
  { value: 'Moonshot', label: 'Moonshot' },
  { value: 'DeepSeek', label: 'DeepSeek' },
  { value: 'Ollama', label: 'Ollama (本地)' },
  { value: 'Other', label: '其他' },
] as const;

export const PAGE_SIZE_OPTIONS = ['10', '20', '50', '100'];

export const DEFAULT_PAGE_SIZE = 10;

export const API_ERROR_MESSAGES: Record<number, string> = {
  400: '请求参数错误',
  401: '未登录或登录已过期',
  403: '权限不足',
  404: '请求的资源不存在',
  500: '服务器内部错误',
  502: '网关错误',
  503: '服务暂不可用',
  504: '网关超时',
};
