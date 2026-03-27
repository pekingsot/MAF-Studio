-- 添加智能体副模型配置字段
-- 支持主模型 + 副模型故障转移机制

-- 1. 添加 fallback_models 字段（JSON格式存储副模型配置）
ALTER TABLE agents ADD COLUMN IF NOT EXISTS fallback_models TEXT;

-- 2. 添加注释
COMMENT ON COLUMN agents.fallback_models IS '副模型配置列表（JSON格式），用于主模型故障转移';
