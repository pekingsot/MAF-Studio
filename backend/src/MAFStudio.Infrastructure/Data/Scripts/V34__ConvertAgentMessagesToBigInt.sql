-- V34: 将agent_messages表的所有UUID字段改为BIGINT自增ID
-- 遵循系统设计规范：所有ID使用自增ID，不使用UUID

-- 1. 删除外键约束
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'agent_messages_collaboration_id_fkey'
        AND table_name = 'agent_messages'
    ) THEN
        ALTER TABLE agent_messages DROP CONSTRAINT agent_messages_collaboration_id_fkey;
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'agent_messages_from_agent_id_fkey'
        AND table_name = 'agent_messages'
    ) THEN
        ALTER TABLE agent_messages DROP CONSTRAINT agent_messages_from_agent_id_fkey;
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'agent_messages_to_agent_id_fkey'
        AND table_name = 'agent_messages'
    ) THEN
        ALTER TABLE agent_messages DROP CONSTRAINT agent_messages_to_agent_id_fkey;
    END IF;
END $$;

-- 2. 修改字段类型为BIGINT
ALTER TABLE agent_messages 
ALTER COLUMN id TYPE BIGINT USING id::text::bigint;

ALTER TABLE agent_messages 
ALTER COLUMN from_agent_id TYPE BIGINT USING from_agent_id::text::bigint;

ALTER TABLE agent_messages 
ALTER COLUMN to_agent_id TYPE BIGINT USING to_agent_id::text::bigint;

ALTER TABLE agent_messages 
ALTER COLUMN collaboration_id TYPE BIGINT USING collaboration_id::text::bigint;

-- 3. 重新添加外键约束
ALTER TABLE agent_messages 
ADD CONSTRAINT agent_messages_collaboration_id_fkey 
FOREIGN KEY (collaboration_id) REFERENCES collaborations(id) ON DELETE CASCADE;

ALTER TABLE agent_messages 
ADD CONSTRAINT agent_messages_from_agent_id_fkey 
FOREIGN KEY (from_agent_id) REFERENCES agents(id) ON DELETE SET NULL;

ALTER TABLE agent_messages 
ADD CONSTRAINT agent_messages_to_agent_id_fkey 
FOREIGN KEY (to_agent_id) REFERENCES agents(id) ON DELETE SET NULL;

-- 4. 验证修改
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'agent_messages' 
AND column_name IN ('id', 'from_agent_id', 'to_agent_id', 'collaboration_id')
ORDER BY column_name;
