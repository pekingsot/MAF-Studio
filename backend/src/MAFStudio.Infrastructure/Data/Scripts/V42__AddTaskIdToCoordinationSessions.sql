-- 为 coordination_sessions 表添加 task_id 字段
ALTER TABLE coordination_sessions ADD COLUMN IF NOT EXISTS task_id BIGINT NULL;

-- 添加索引
CREATE INDEX IF NOT EXISTS idx_coordination_sessions_task_id ON coordination_sessions(task_id);

-- 添加注释
COMMENT ON COLUMN coordination_sessions.task_id IS '关联的任务ID';
