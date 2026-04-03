-- 为collaboration_agents表添加custom_prompt字段
-- 用于存储Agent的自定义提示词（覆盖系统提示词）

ALTER TABLE collaboration_agents 
ADD COLUMN IF NOT EXISTS custom_prompt TEXT;

-- 更新注释
COMMENT ON COLUMN collaboration_agents.role IS 'Agent在工作流中的角色：Manager（协调者）或 Worker（执行者）';
COMMENT ON COLUMN collaboration_agents.custom_prompt IS 'Agent的自定义提示词，用于覆盖系统提示词';
