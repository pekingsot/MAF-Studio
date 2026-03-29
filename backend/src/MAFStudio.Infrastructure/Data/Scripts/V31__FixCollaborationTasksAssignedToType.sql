-- V31: 修复collaboration_tasks表的assigned_to字段类型
-- 将assigned_to从VARCHAR(36)改为BIGINT

-- 修改assigned_to字段类型
ALTER TABLE collaboration_tasks 
ALTER COLUMN assigned_to TYPE BIGINT 
USING NULL;

-- 验证修改
DO $$
DECLARE
    assigned_to_type VARCHAR;
BEGIN
    SELECT data_type INTO assigned_to_type 
    FROM information_schema.columns 
    WHERE table_name = 'collaboration_tasks' AND column_name = 'assigned_to';
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'collaboration_tasks表字段类型修复完成！';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'assigned_to 类型: %', assigned_to_type;
    RAISE NOTICE '========================================';
END $$;
