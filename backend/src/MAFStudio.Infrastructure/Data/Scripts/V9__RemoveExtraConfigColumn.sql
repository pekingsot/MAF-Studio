-- V9: 删除 extra_config 列
-- 从 llm_configs 表删除 extra_config 列
ALTER TABLE llm_configs DROP COLUMN IF EXISTS extra_config;

-- 从 llm_model_configs 表删除 extra_config 列
ALTER TABLE llm_model_configs DROP COLUMN IF EXISTS extra_config;
