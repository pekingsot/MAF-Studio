-- 添加智能体模型配置表
-- 支持主模型 + 副模型故障转移机制

-- 1. 创建智能体模型配置表
CREATE TABLE IF NOT EXISTS agent_models (
    id BIGINT PRIMARY KEY,
    agent_id BIGINT NOT NULL,
    llm_config_id BIGINT NOT NULL,
    llm_model_config_id BIGINT,
    priority INTEGER NOT NULL DEFAULT 0,
    is_primary BOOLEAN NOT NULL DEFAULT false,
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_agent_models_agent_id ON agent_models(agent_id);
CREATE INDEX IF NOT EXISTS ix_agent_models_priority ON agent_models(priority);

-- 2. 添加智能体系统提示词字段
ALTER TABLE agents ADD COLUMN IF NOT EXISTS system_prompt TEXT;

-- 3. 从现有 configuration 字段迁移 systemPrompt 数据
UPDATE agents 
SET system_prompt = configuration::jsonb->>'systemPrompt'
WHERE configuration IS NOT NULL 
  AND configuration != '{}' 
  AND configuration::jsonb->>'systemPrompt' IS NOT NULL;

-- 4. 迁移现有 llm_config_id 到 agent_models 表作为主模型
INSERT INTO agent_models (id, agent_id, llm_config_id, llm_model_config_id, priority, is_primary, is_enabled, created_at)
SELECT 
    (ROW_NUMBER() OVER (ORDER BY a.id) + 5000000000000000)::BIGINT as id,
    a.id as agent_id,
    a.llm_config_id,
    a.llm_model_config_id,
    0 as priority,
    true as is_primary,
    true as is_enabled,
    CURRENT_TIMESTAMP as created_at
FROM agents a
WHERE a.llm_config_id IS NOT NULL;

-- 5. 添加注释
COMMENT ON TABLE agent_models IS '智能体模型配置表，支持主模型+副模型故障转移';
COMMENT ON COLUMN agent_models.priority IS '优先级，数字越小优先级越高';
COMMENT ON COLUMN agent_models.is_primary IS '是否为主模型';
COMMENT ON COLUMN agents.system_prompt IS '智能体自定义的系统提示词';
