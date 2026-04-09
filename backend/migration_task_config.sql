-- 为 collaboration_tasks 表添加 config 字段
-- 用于存储任务配置（执行模式、协调者ID、最大迭代次数等）

ALTER TABLE collaboration_tasks 
ADD COLUMN IF NOT EXISTS config TEXT;

COMMENT ON COLUMN collaboration_tasks.config IS '任务配置（JSON格式）：orchestrationMode, managerAgentId, maxIterations 等';
