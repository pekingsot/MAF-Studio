-- 为agents表添加llm_configs字段
-- 用于存储大模型选择配置（包含主模型和副模型，带验证状态）

ALTER TABLE agents 
ADD COLUMN IF NOT EXISTS llm_configs TEXT;

-- 更新注释
COMMENT ON COLUMN agents.llm_configs IS '大模型选择配置（JSON格式，包含主模型和副模型，带验证状态）';
