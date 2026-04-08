-- V29: 修复collaboration_agents表的字段类型
-- 将agent_id和collaboration_id从UUID改为BIGINT

-- 修改collaboration_id字段类型
ALTER TABLE collaboration_agents 
ALTER COLUMN collaboration_id TYPE BIGINT 
USING collaboration_id::BIGINT;

-- 修改agent_id字段类型
ALTER TABLE collaboration_agents 
ALTER COLUMN agent_id TYPE BIGINT 
USING agent_id::BIGINT;

-- 删除旧的外键约束（如果存在）
DO $$
DECLARE
    fk_record RECORD;
BEGIN
    FOR fk_record IN 
        SELECT constraint_name 
        FROM information_schema.table_constraints 
        WHERE table_name = 'collaboration_agents' 
        AND constraint_type = 'FOREIGN KEY'
    LOOP
        EXECUTE 'ALTER TABLE collaboration_agents DROP CONSTRAINT IF EXISTS ' || fk_record.constraint_name;
    END LOOP;
END $$;

-- 删除旧的唯一索引
DROP INDEX IF EXISTS ix_collaboration_agents_collaboration_agent;

-- 创建新的唯一索引
CREATE UNIQUE INDEX IF NOT EXISTS ix_collaboration_agents_collaboration_agent 
ON collaboration_agents(collaboration_id, agent_id);

-- 验证修改
DO $$
DECLARE
    collaboration_id_type VARCHAR;
    agent_id_type VARCHAR;
BEGIN
    SELECT data_type INTO collaboration_id_type 
    FROM information_schema.columns 
    WHERE table_name = 'collaboration_agents' AND column_name = 'collaboration_id';
    
    SELECT data_type INTO agent_id_type 
    FROM information_schema.columns 
    WHERE table_name = 'collaboration_agents' AND column_name = 'agent_id';
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'collaboration_agents表字段类型修复完成！';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'collaboration_id 类型: %', collaboration_id_type;
    RAISE NOTICE 'agent_id 类型: %', agent_id_type;
    RAISE NOTICE '========================================';
END $$;
