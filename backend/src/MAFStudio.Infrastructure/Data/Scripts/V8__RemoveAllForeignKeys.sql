-- V8__RemoveAllForeignKeys.sql
-- 删除所有外键约束，使用应用层逻辑维护数据一致性

-- 使用 DO 块来安全地删除外键约束，忽略不存在的约束

-- 1. 删除 llm_configs 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'llm_configs_user_id_fkey' AND table_name = 'llm_configs') THEN
        ALTER TABLE llm_configs DROP CONSTRAINT llm_configs_user_id_fkey;
    END IF;
END $$;

-- 2. 删除 llm_model_configs 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'llm_model_configs_llm_config_id_fkey' AND table_name = 'llm_model_configs') THEN
        ALTER TABLE llm_model_configs DROP CONSTRAINT llm_model_configs_llm_config_id_fkey;
    END IF;
END $$;

-- 3. 删除 agent_types 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'agent_types_llm_config_id_fkey' AND table_name = 'agent_types') THEN
        ALTER TABLE agent_types DROP CONSTRAINT agent_types_llm_config_id_fkey;
    END IF;
END $$;

-- 4. 删除 agents 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'agents_user_id_fkey' AND table_name = 'agents') THEN
        ALTER TABLE agents DROP CONSTRAINT agents_user_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'agents_llm_config_id_fkey' AND table_name = 'agents') THEN
        ALTER TABLE agents DROP CONSTRAINT agents_llm_config_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'agents_llm_model_config_id_fkey' AND table_name = 'agents') THEN
        ALTER TABLE agents DROP CONSTRAINT agents_llm_model_config_id_fkey;
    END IF;
END $$;

-- 5. 删除 collaborations 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'collaborations_user_id_fkey' AND table_name = 'collaborations') THEN
        ALTER TABLE collaborations DROP CONSTRAINT collaborations_user_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'collaborations_llm_config_id_fkey' AND table_name = 'collaborations') THEN
        ALTER TABLE collaborations DROP CONSTRAINT collaborations_llm_config_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'collaborations_llm_model_config_id_fkey' AND table_name = 'collaborations') THEN
        ALTER TABLE collaborations DROP CONSTRAINT collaborations_llm_model_config_id_fkey;
    END IF;
END $$;

-- 6. 删除 collaboration_agents 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'collaboration_agents_collaboration_id_fkey' AND table_name = 'collaboration_agents') THEN
        ALTER TABLE collaboration_agents DROP CONSTRAINT collaboration_agents_collaboration_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'collaboration_agents_agent_id_fkey' AND table_name = 'collaboration_agents') THEN
        ALTER TABLE collaboration_agents DROP CONSTRAINT collaboration_agents_agent_id_fkey;
    END IF;
END $$;

-- 7. 删除 collaboration_tasks 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'collaboration_tasks_collaboration_id_fkey' AND table_name = 'collaboration_tasks') THEN
        ALTER TABLE collaboration_tasks DROP CONSTRAINT collaboration_tasks_collaboration_id_fkey;
    END IF;
END $$;

-- 8. 删除 messages 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'messages_from_agent_id_fkey' AND table_name = 'messages') THEN
        ALTER TABLE messages DROP CONSTRAINT messages_from_agent_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'messages_to_agent_id_fkey' AND table_name = 'messages') THEN
        ALTER TABLE messages DROP CONSTRAINT messages_to_agent_id_fkey;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'messages_collaboration_id_fkey' AND table_name = 'messages') THEN
        ALTER TABLE messages DROP CONSTRAINT messages_collaboration_id_fkey;
    END IF;
END $$;

-- 9. 删除 rag_chunks 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'rag_chunks_document_id_fkey' AND table_name = 'rag_chunks') THEN
        ALTER TABLE rag_chunks DROP CONSTRAINT rag_chunks_document_id_fkey;
    END IF;
END $$;

-- 10. 删除 rag_documents 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'rag_documents_user_id_fkey' AND table_name = 'rag_documents') THEN
        ALTER TABLE rag_documents DROP CONSTRAINT rag_documents_user_id_fkey;
    END IF;
END $$;

-- 11. 删除 operation_logs 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'operation_logs_user_id_fkey' AND table_name = 'operation_logs') THEN
        ALTER TABLE operation_logs DROP CONSTRAINT operation_logs_user_id_fkey;
    END IF;
END $$;

-- 12. 删除 system_logs 表的外键
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'system_logs_user_id_fkey' AND table_name = 'system_logs') THEN
        ALTER TABLE system_logs DROP CONSTRAINT system_logs_user_id_fkey;
    END IF;
END $$;
