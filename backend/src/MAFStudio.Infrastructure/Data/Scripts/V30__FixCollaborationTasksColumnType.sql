-- V30: 修复collaboration_tasks表的字段类型
-- 将collaboration_id从UUID改为BIGINT

-- 修改collaboration_id字段类型
ALTER TABLE collaboration_tasks 
ALTER COLUMN collaboration_id TYPE BIGINT 
USING collaboration_id::BIGINT;

-- 删除旧的外键约束（如果存在）
DO $$
DECLARE
    fk_record RECORD;
BEGIN
    FOR fk_record IN 
        SELECT constraint_name 
        FROM information_schema.table_constraints 
        WHERE table_name = 'collaboration_tasks' 
        AND constraint_type = 'FOREIGN KEY'
    LOOP
        EXECUTE 'ALTER TABLE collaboration_tasks DROP CONSTRAINT IF EXISTS ' || fk_record.constraint_name;
    END LOOP;
END $$;

-- 验证修改
DO $$
DECLARE
    collaboration_id_type VARCHAR;
BEGIN
    SELECT data_type INTO collaboration_id_type 
    FROM information_schema.columns 
    WHERE table_name = 'collaboration_tasks' AND column_name = 'collaboration_id';
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'collaboration_tasks表字段类型修复完成！';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'collaboration_id 类型: %', collaboration_id_type;
    RAISE NOTICE '========================================';
END $$;
