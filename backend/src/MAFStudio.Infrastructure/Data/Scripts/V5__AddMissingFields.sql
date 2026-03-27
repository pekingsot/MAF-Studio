-- V5__AddMissingFields.sql
-- 修复缺失字段：智能体类型、LLM配置

-- =============================================
-- 1. 扩展 agent_types 表，添加缺失字段
-- =============================================

ALTER TABLE agent_types ADD COLUMN IF NOT EXISTS is_system BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE agent_types ADD COLUMN IF NOT EXISTS is_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE agent_types ADD COLUMN IF NOT EXISTS sort_order INTEGER NOT NULL DEFAULT 0;

-- 扩展 default_configuration 存储更多信息
-- 已有字段: systemPrompt, temperature, maxTokens
-- 需要: default_system_prompt, default_temperature, default_max_tokens

-- =============================================
-- 2. 扩展 llm_configs 表，添加缺失字段
-- =============================================

ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS is_default BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS is_enabled BOOLEAN NOT NULL DEFAULT true;

-- =============================================
-- 3. 扩展 llm_model_configs 表，添加缺失字段
-- =============================================

ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS temperature DECIMAL(3,2) NOT NULL DEFAULT 0.7;
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS max_tokens INTEGER NOT NULL DEFAULT 4096;
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS context_window INTEGER NOT NULL DEFAULT 8192;
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS top_p DECIMAL(3,2);
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS frequency_penalty DECIMAL(3,2);
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS presence_penalty DECIMAL(3,2);
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS stop_sequences TEXT;
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS is_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS sort_order INTEGER NOT NULL DEFAULT 0;

-- =============================================
-- 4. 更新现有智能体类型数据，补充缺失字段
-- =============================================

UPDATE agent_types SET is_system = true, is_enabled = true, sort_order = 0 WHERE is_system IS NULL OR is_system = false;

-- =============================================
-- 5. 创建供应商列表接口的模拟数据（系统配置）
-- =============================================

INSERT INTO system_configs (id, key, value, description, created_at) VALUES
(5000000000000010, 'llm.providers.list', 
 '{"providers": [
   {"id": "qwen", "displayName": "阿里千问", "defaultEndpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1", "defaultModel": "qwen-max"},
   {"id": "zhipu", "displayName": "智谱AI", "defaultEndpoint": "https://open.bigmodel.cn/api/paas/v4", "defaultModel": "glm-4"},
   {"id": "openai", "displayName": "OpenAI", "defaultEndpoint": "https://api.openai.com/v1", "defaultModel": "gpt-4o"},
   {"id": "deepseek", "displayName": "DeepSeek", "defaultEndpoint": "https://api.deepseek.com/v1", "defaultModel": "deepseek-chat"},
   {"id": "anthropic", "displayName": "Anthropic", "defaultEndpoint": "https://api.anthropic.com/v1", "defaultModel": "claude-3-opus-20240229"},
   {"id": "openai_compatible", "displayName": "OpenAI兼容", "defaultEndpoint": "", "defaultModel": "gpt-4o"}
 ]}', 
 'LLM供应商列表配置', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;
