-- 添加智能体冗余字段，优化查询性能
-- 避免每次查询都需要关联查询 llm_configs 和 llm_model_configs 表

-- 1. 添加智能体类型名称冗余字段
ALTER TABLE agents ADD COLUMN IF NOT EXISTS type_name VARCHAR(100);

-- 2. 添加主模型配置名称冗余字段
ALTER TABLE agents ADD COLUMN IF NOT EXISTS llm_config_name VARCHAR(100);

-- 3. 添加主模型名称冗余字段
ALTER TABLE agents ADD COLUMN IF NOT EXISTS llm_model_name VARCHAR(100);

-- 4. 添加注释
COMMENT ON COLUMN agents.type_name IS '智能体类型名称（冗余字段）';
COMMENT ON COLUMN agents.llm_config_name IS '主模型配置名称（冗余字段）';
COMMENT ON COLUMN agents.llm_model_name IS '主模型名称（冗余字段）';

-- 5. 创建索引优化查询
CREATE INDEX IF NOT EXISTS ix_agents_type_name ON agents(type_name);
CREATE INDEX IF NOT EXISTS ix_agents_llm_config_name ON agents(llm_config_name);

-- 6. 删除之前添加的 fallback_models_detail 字段（如果存在）
ALTER TABLE agents DROP COLUMN IF EXISTS fallback_models_detail;
