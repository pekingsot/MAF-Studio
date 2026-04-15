ALTER TABLE collaboration_tasks ADD COLUMN IF NOT EXISTS task_flow TEXT;

COMMENT ON COLUMN collaboration_tasks.task_flow IS '工作流编排数据（JSON格式），包含节点、边、参数等';
