-- V16__FixLlmConfigsColumns.sql
-- 修复llm_configs表缺少的列

-- 添加缺失的列
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS is_default BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS is_enabled BOOLEAN NOT NULL DEFAULT true;

-- 创建索引
CREATE INDEX IF NOT EXISTS ix_llm_configs_is_default ON llm_configs(is_default);
CREATE INDEX IF NOT EXISTS ix_llm_configs_is_enabled ON llm_configs(is_enabled);
