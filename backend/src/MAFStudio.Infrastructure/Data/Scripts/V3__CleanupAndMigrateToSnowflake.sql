-- V3__CleanupAndMigrateToSnowflake.sql
-- 清理旧表并迁移到雪花算法ID
-- 执行此脚本前请确保已备份数据（测试环境可忽略）

-- =============================================
-- 第一步：删除所有旧表（按依赖关系倒序删除）
-- =============================================

DROP TABLE IF EXISTS system_logs CASCADE;
DROP TABLE IF EXISTS rag_document_chunks CASCADE;
DROP TABLE IF EXISTS rag_documents CASCADE;
DROP TABLE IF EXISTS system_configs CASCADE;
DROP TABLE IF EXISTS operation_logs CASCADE;
DROP TABLE IF EXISTS agent_messages CASCADE;
DROP TABLE IF EXISTS collaboration_tasks CASCADE;
DROP TABLE IF EXISTS collaboration_agents CASCADE;
DROP TABLE IF EXISTS collaborations CASCADE;
DROP TABLE IF EXISTS agents CASCADE;
DROP TABLE IF EXISTS agent_types CASCADE;
DROP TABLE IF EXISTS llm_test_records CASCADE;
DROP TABLE IF EXISTS llm_model_configs CASCADE;
DROP TABLE IF EXISTS llm_configs CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- 删除旧的触发器函数（如果存在）
DROP FUNCTION IF EXISTS update_updated_at_column() CASCADE;

-- =============================================
-- 第二步：创建新表结构（使用雪花算法ID）
-- =============================================

-- 用户表（用户ID保持VARCHAR，因为可能来自外部系统）
CREATE TABLE IF NOT EXISTS users (
    id VARCHAR(36) PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'user',
    avatar VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username ON users(username);
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON users(email);

-- 大模型配置表（使用BIGINT雪花ID）
CREATE TABLE IF NOT EXISTS llm_configs (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    provider VARCHAR(50) NOT NULL,
    api_key TEXT,
    endpoint TEXT,
    default_model VARCHAR(100),
    extra_config JSONB,
    user_id VARCHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_llm_configs_name ON llm_configs(name);
CREATE INDEX IF NOT EXISTS ix_llm_configs_provider ON llm_configs(provider);
CREATE INDEX IF NOT EXISTS ix_llm_configs_user_id ON llm_configs(user_id);

-- 大模型子配置表
CREATE TABLE IF NOT EXISTS llm_model_configs (
    id BIGINT PRIMARY KEY,
    llm_config_id BIGINT NOT NULL REFERENCES llm_configs(id) ON DELETE CASCADE,
    model_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(100),
    description TEXT,
    is_default BOOLEAN NOT NULL DEFAULT false,
    extra_config JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_llm_model_configs_llm_config_id ON llm_model_configs(llm_config_id);
CREATE INDEX IF NOT EXISTS ix_llm_model_configs_model_name ON llm_model_configs(model_name);

-- 大模型测试记录表
CREATE TABLE IF NOT EXISTS llm_test_records (
    id BIGINT PRIMARY KEY,
    llm_config_id BIGINT NOT NULL REFERENCES llm_configs(id) ON DELETE CASCADE,
    llm_model_config_id BIGINT REFERENCES llm_model_configs(id) ON DELETE SET NULL,
    prompt TEXT NOT NULL,
    response TEXT,
    success BOOLEAN NOT NULL DEFAULT false,
    error_message TEXT,
    tokens_used INTEGER NOT NULL DEFAULT 0,
    response_time_ms INTEGER NOT NULL DEFAULT 0,
    user_id VARCHAR(36) NOT NULL,
    tested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_llm_test_records_llm_config_id ON llm_test_records(llm_config_id);
CREATE INDEX IF NOT EXISTS ix_llm_test_records_tested_at ON llm_test_records(tested_at);

-- 智能体类型表
CREATE TABLE IF NOT EXISTS agent_types (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    default_configuration JSONB,
    llm_config_id BIGINT REFERENCES llm_configs(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_agent_types_code ON agent_types(code);

-- 智能体表
CREATE TABLE IF NOT EXISTS agents (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL DEFAULT 'Assistant',
    configuration JSONB NOT NULL DEFAULT '{}',
    avatar VARCHAR(50),
    user_id VARCHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status INTEGER NOT NULL DEFAULT 0,
    llm_config_id BIGINT REFERENCES llm_configs(id) ON DELETE SET NULL,
    llm_model_config_id BIGINT REFERENCES llm_model_configs(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_agents_name ON agents(name);
CREATE INDEX IF NOT EXISTS ix_agents_user_id ON agents(user_id);
CREATE INDEX IF NOT EXISTS ix_agents_status ON agents(status);

-- 协作项目表
CREATE TABLE IF NOT EXISTS collaborations (
    id BIGINT PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    path VARCHAR(500),
    status INTEGER NOT NULL DEFAULT 0,
    user_id VARCHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    git_repository_url VARCHAR(500),
    git_branch VARCHAR(100),
    git_username VARCHAR(100),
    git_email VARCHAR(100),
    git_access_token TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_collaborations_user_id ON collaborations(user_id);
CREATE INDEX IF NOT EXISTS ix_collaborations_status ON collaborations(status);

-- 协作智能体关联表
CREATE TABLE IF NOT EXISTS collaboration_agents (
    id BIGINT PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    agent_id BIGINT NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    role VARCHAR(100),
    joined_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_collaboration_agents_collaboration_agent ON collaboration_agents(collaboration_id, agent_id);

-- 协作任务表
CREATE TABLE IF NOT EXISTS collaboration_tasks (
    id BIGINT PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    status INTEGER NOT NULL DEFAULT 0,
    assigned_to VARCHAR(36),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_collaboration_tasks_collaboration_id ON collaboration_tasks(collaboration_id);

-- 智能体消息表
CREATE TABLE IF NOT EXISTS agent_messages (
    id BIGINT PRIMARY KEY,
    from_agent_id BIGINT REFERENCES agents(id) ON DELETE SET NULL,
    to_agent_id BIGINT REFERENCES agents(id) ON DELETE SET NULL,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    sender_type INTEGER NOT NULL DEFAULT 0,
    sender_name VARCHAR(100),
    user_id VARCHAR(36),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_streaming BOOLEAN NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS ix_agent_messages_collaboration_id ON agent_messages(collaboration_id);
CREATE INDEX IF NOT EXISTS ix_agent_messages_created_at ON agent_messages(created_at);

-- 操作日志表
CREATE TABLE IF NOT EXISTS operation_logs (
    id BIGINT PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    action VARCHAR(50) NOT NULL,
    resource_type VARCHAR(50) NOT NULL,
    resource_id VARCHAR(36),
    description TEXT,
    details JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_operation_logs_user_id ON operation_logs(user_id);
CREATE INDEX IF NOT EXISTS ix_operation_logs_created_at ON operation_logs(created_at);

-- 系统配置表
CREATE TABLE IF NOT EXISTS system_configs (
    id BIGINT PRIMARY KEY,
    key VARCHAR(100) NOT NULL,
    value TEXT,
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_system_configs_key ON system_configs(key);

-- RAG文档表
CREATE TABLE IF NOT EXISTS rag_documents (
    id BIGINT PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500),
    file_type VARCHAR(50),
    file_size BIGINT NOT NULL DEFAULT 0,
    status INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    user_id VARCHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_rag_documents_file_name ON rag_documents(file_name);
CREATE INDEX IF NOT EXISTS ix_rag_documents_status ON rag_documents(status);
CREATE INDEX IF NOT EXISTS ix_rag_documents_user_id ON rag_documents(user_id);

-- RAG文档分块表
CREATE TABLE IF NOT EXISTS rag_document_chunks (
    id BIGINT PRIMARY KEY,
    document_id BIGINT NOT NULL REFERENCES rag_documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_rag_document_chunks_document_id ON rag_document_chunks(document_id);

-- 系统日志表
CREATE TABLE IF NOT EXISTS system_logs (
    id BIGINT PRIMARY KEY,
    level VARCHAR(20) NOT NULL DEFAULT 'Info',
    category VARCHAR(100) NOT NULL,
    message TEXT NOT NULL,
    exception TEXT,
    stack_trace TEXT,
    user_id VARCHAR(36),
    request_path VARCHAR(500),
    additional_data JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_system_logs_level ON system_logs(level);
CREATE INDEX IF NOT EXISTS ix_system_logs_category ON system_logs(category);
CREATE INDEX IF NOT EXISTS ix_system_logs_created_at ON system_logs(created_at);
CREATE INDEX IF NOT EXISTS ix_system_logs_user_id ON system_logs(user_id);

-- =============================================
-- 说明：UpdatedAt 字段由应用程序代码自动维护
-- 不再使用数据库触发器，提高可维护性和可测试性
-- =============================================
