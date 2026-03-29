-- V23: 迁移到自增ID并清理数据
-- 保留 agent_types 和权限相关数据（roles, permissions, user_roles, role_permissions）

-- =====================================================
-- 第一步：清理不需要保留的数据
-- =====================================================

-- 清理智能体相关数据
TRUNCATE TABLE agent_messages CASCADE;
TRUNCATE TABLE agent_models CASCADE;
TRUNCATE TABLE agents CASCADE;

-- 清理协作相关数据
TRUNCATE TABLE collaboration_tasks CASCADE;
TRUNCATE TABLE collaboration_agents CASCADE;
TRUNCATE TABLE collaborations CASCADE;

-- 清理LLM配置相关数据
TRUNCATE TABLE llm_test_records CASCADE;
TRUNCATE TABLE llm_model_configs CASCADE;
TRUNCATE TABLE llm_configs CASCADE;

-- 清理RAG文档相关数据
TRUNCATE TABLE rag_document_chunks CASCADE;
TRUNCATE TABLE rag_documents CASCADE;

-- 清理日志和配置
TRUNCATE TABLE system_logs CASCADE;
TRUNCATE TABLE operation_logs CASCADE;
TRUNCATE TABLE system_configs CASCADE;

-- =====================================================
-- 第二步：将所有表的ID字段改为自增序列
-- =====================================================

-- 为 agent_types 创建序列（保留现有数据）
CREATE SEQUENCE IF NOT EXISTS agent_types_id_seq;
SELECT setval('agent_types_id_seq', COALESCE((SELECT MAX(id) FROM agent_types), 0) + 1);
ALTER TABLE agent_types ALTER COLUMN id SET DEFAULT nextval('agent_types_id_seq');
ALTER SEQUENCE agent_types_id_seq OWNED BY agent_types.id;

-- 为 agents 创建序列
CREATE SEQUENCE IF NOT EXISTS agents_id_seq;
ALTER TABLE agents ALTER COLUMN id SET DEFAULT nextval('agents_id_seq');
ALTER SEQUENCE agents_id_seq OWNED BY agents.id;

-- 为 llm_configs 创建序列
CREATE SEQUENCE IF NOT EXISTS llm_configs_id_seq;
ALTER TABLE llm_configs ALTER COLUMN id SET DEFAULT nextval('llm_configs_id_seq');
ALTER SEQUENCE llm_configs_id_seq OWNED BY llm_configs.id;

-- 为 llm_model_configs 创建序列
CREATE SEQUENCE IF NOT EXISTS llm_model_configs_id_seq;
ALTER TABLE llm_model_configs ALTER COLUMN id SET DEFAULT nextval('llm_model_configs_id_seq');
ALTER SEQUENCE llm_model_configs_id_seq OWNED BY llm_model_configs.id;

-- 为 llm_test_records 创建序列
CREATE SEQUENCE IF NOT EXISTS llm_test_records_id_seq;
ALTER TABLE llm_test_records ALTER COLUMN id SET DEFAULT nextval('llm_test_records_id_seq');
ALTER SEQUENCE llm_test_records_id_seq OWNED BY llm_test_records.id;

-- 为 collaborations 创建序列
CREATE SEQUENCE IF NOT EXISTS collaborations_id_seq;
ALTER TABLE collaborations ALTER COLUMN id SET DEFAULT nextval('collaborations_id_seq');
ALTER SEQUENCE collaborations_id_seq OWNED BY collaborations.id;

-- 为 collaboration_agents 创建序列
CREATE SEQUENCE IF NOT EXISTS collaboration_agents_id_seq;
ALTER TABLE collaboration_agents ALTER COLUMN id SET DEFAULT nextval('collaboration_agents_id_seq');
ALTER SEQUENCE collaboration_agents_id_seq OWNED BY collaboration_agents.id;

-- 为 collaboration_tasks 创建序列
CREATE SEQUENCE IF NOT EXISTS collaboration_tasks_id_seq;
ALTER TABLE collaboration_tasks ALTER COLUMN id SET DEFAULT nextval('collaboration_tasks_id_seq');
ALTER SEQUENCE collaboration_tasks_id_seq OWNED BY collaboration_tasks.id;

-- 为 agent_messages 创建序列
CREATE SEQUENCE IF NOT EXISTS agent_messages_id_seq;
ALTER TABLE agent_messages ALTER COLUMN id SET DEFAULT nextval('agent_messages_id_seq');
ALTER SEQUENCE agent_messages_id_seq OWNED BY agent_messages.id;

-- 为 operation_logs 创建序列
CREATE SEQUENCE IF NOT EXISTS operation_logs_id_seq;
ALTER TABLE operation_logs ALTER COLUMN id SET DEFAULT nextval('operation_logs_id_seq');
ALTER SEQUENCE operation_logs_id_seq OWNED BY operation_logs.id;

-- 为 system_logs 创建序列
CREATE SEQUENCE IF NOT EXISTS system_logs_id_seq;
ALTER TABLE system_logs ALTER COLUMN id SET DEFAULT nextval('system_logs_id_seq');
ALTER SEQUENCE system_logs_id_seq OWNED BY system_logs.id;

-- 为 system_configs 创建序列
CREATE SEQUENCE IF NOT EXISTS system_configs_id_seq;
ALTER TABLE system_configs ALTER COLUMN id SET DEFAULT nextval('system_configs_id_seq');
ALTER SEQUENCE system_configs_id_seq OWNED BY system_configs.id;

-- 为 rag_documents 创建序列
CREATE SEQUENCE IF NOT EXISTS rag_documents_id_seq;
ALTER TABLE rag_documents ALTER COLUMN id SET DEFAULT nextval('rag_documents_id_seq');
ALTER SEQUENCE rag_documents_id_seq OWNED BY rag_documents.id;

-- 为 rag_document_chunks 创建序列
CREATE SEQUENCE IF NOT EXISTS rag_document_chunks_id_seq;
ALTER TABLE rag_document_chunks ALTER COLUMN id SET DEFAULT nextval('rag_document_chunks_id_seq');
ALTER SEQUENCE rag_document_chunks_id_seq OWNED BY rag_document_chunks.id;

-- 为 agent_models 创建序列
CREATE SEQUENCE IF NOT EXISTS agent_models_id_seq;
ALTER TABLE agent_models ALTER COLUMN id SET DEFAULT nextval('agent_models_id_seq');
ALTER SEQUENCE agent_models_id_seq OWNED BY agent_models.id;

-- =====================================================
-- 第三步：验证迁移结果
-- =====================================================

DO $$
DECLARE
    agent_types_count INTEGER;
    roles_count INTEGER;
    permissions_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO agent_types_count FROM agent_types;
    SELECT COUNT(*) INTO roles_count FROM roles;
    SELECT COUNT(*) INTO permissions_count FROM permissions;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE '迁移完成！';
    RAISE NOTICE '========================================';
    RAISE NOTICE '保留的数据：';
    RAISE NOTICE '  agent_types 记录数: %', agent_types_count;
    RAISE NOTICE '  roles 记录数: %', roles_count;
    RAISE NOTICE '  permissions 记录数: %', permissions_count;
    RAISE NOTICE '========================================';
    RAISE NOTICE '所有表的ID字段已改为自增序列';
    RAISE NOTICE '========================================';
END $$;
