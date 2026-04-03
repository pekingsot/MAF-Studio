-- V32: 为collaboration_agents表添加custom_prompt字段
-- 用于存储Agent的自定义提示词（覆盖系统提示词）

ALTER TABLE collaboration_agents 
ADD COLUMN IF NOT EXISTS custom_prompt TEXT;

-- 更新注释
COMMENT ON COLUMN collaboration_agents.role IS 'Agent在工作流中的角色：Manager（协调者）或 Worker（执行者）';
COMMENT ON COLUMN collaboration_agents.custom_prompt IS 'Agent的自定义提示词，用于覆盖系统提示词';

-- 验证字段是否添加成功
DO $$
DECLARE
    column_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'collaboration_agents' 
        AND column_name = 'custom_prompt'
    ) INTO column_exists;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'collaboration_agents表字段添加完成！';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'custom_prompt字段存在: %', column_exists;
    RAISE NOTICE '========================================';
END $$;
