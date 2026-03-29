-- V14__MigrateUsersToSnowflakeId.sql
-- 将users表的ID从VARCHAR(36) UUID改为BIGINT雪花ID
-- 同时更新所有引用users.id的字段类型

-- =============================================
-- 第一步：创建临时映射表
-- =============================================

CREATE TEMP TABLE user_id_mapping (
    old_id VARCHAR(36),
    new_id BIGINT
);

-- 为现有用户生成新的雪花ID
INSERT INTO user_id_mapping (old_id, new_id)
SELECT 
    id,
    CASE 
        WHEN id = '00000000-0000-0000-0000-000000000001' THEN 1000000000000001
        WHEN id = '00000000-0000-0000-0000-000000000002' THEN 1000000000000002
        WHEN id = '00000000-0000-0000-0000-000000000003' THEN 1000000000000003
        ELSE 1000000000000004 + ROW_NUMBER() OVER (ORDER BY created_at) - 1
    END
FROM users;

-- =============================================
-- 第二步：创建新的users表
-- =============================================

CREATE TABLE users_new (
    id BIGINT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'user',
    avatar VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 迁移数据
INSERT INTO users_new (id, username, email, password_hash, role, avatar, created_at, updated_at)
SELECT 
    m.new_id,
    u.username,
    u.email,
    u.password_hash,
    u.role,
    u.avatar,
    u.created_at,
    u.updated_at
FROM users u
INNER JOIN user_id_mapping m ON u.id = m.old_id;

-- 创建索引
CREATE UNIQUE INDEX ix_users_new_username ON users_new(username);
CREATE UNIQUE INDEX ix_users_new_email ON users_new(email);

-- =============================================
-- 第三步：备份并重建所有引用user_id的表
-- =============================================

-- 备份 llm_configs 表
CREATE TABLE llm_configs_backup AS SELECT * FROM llm_configs;

-- 删除并重建 llm_configs 表
DROP TABLE llm_configs CASCADE;
CREATE TABLE llm_configs (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    provider VARCHAR(50) NOT NULL,
    api_key TEXT,
    endpoint TEXT,
    default_model VARCHAR(100),
    user_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- 迁移数据
INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, user_id, created_at, updated_at)
SELECT 
    b.id,
    b.name,
    b.provider,
    b.api_key,
    b.endpoint,
    b.default_model,
    m.new_id,
    b.created_at,
    b.updated_at
FROM llm_configs_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

-- 创建索引
CREATE INDEX ix_llm_configs_name ON llm_configs(name);
CREATE INDEX ix_llm_configs_provider ON llm_configs(provider);
CREATE INDEX ix_llm_configs_user_id ON llm_configs(user_id);

-- 备份 agents 表
CREATE TABLE agents_backup AS SELECT * FROM agents;

-- 删除并重建 agents 表
DROP TABLE agents CASCADE;
CREATE TABLE agents (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    type VARCHAR(50) NOT NULL DEFAULT 'Assistant',
    system_prompt TEXT,
    avatar VARCHAR(50),
    user_id BIGINT NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    llm_config_id BIGINT,
    llm_model_config_id BIGINT,
    fallback_models TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- 迁移数据
INSERT INTO agents (id, name, description, type, system_prompt, avatar, user_id, status, llm_config_id, llm_model_config_id, fallback_models, created_at, updated_at)
SELECT 
    b.id,
    b.name,
    b.description,
    b.type,
    b.system_prompt,
    b.avatar,
    m.new_id,
    b.status,
    b.llm_config_id,
    b.llm_model_config_id,
    b.fallback_models,
    b.created_at,
    b.updated_at
FROM agents_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

-- 创建索引
CREATE INDEX ix_agents_name ON agents(name);
CREATE INDEX ix_agents_user_id ON agents(user_id);
CREATE INDEX ix_agents_status ON agents(status);

-- 备份 collaborations 表
CREATE TABLE collaborations_backup AS SELECT * FROM collaborations;

-- 删除并重建 collaborations 表
DROP TABLE collaborations CASCADE;
CREATE TABLE collaborations (
    id BIGINT PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    path VARCHAR(500),
    status INTEGER NOT NULL DEFAULT 0,
    user_id BIGINT NOT NULL,
    git_repository_url VARCHAR(500),
    git_branch VARCHAR(100),
    git_username VARCHAR(100),
    git_email VARCHAR(100),
    git_access_token TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- 迁移数据
INSERT INTO collaborations (id, name, description, path, status, user_id, git_repository_url, git_branch, git_username, git_email, git_access_token, created_at, updated_at)
SELECT 
    b.id,
    b.name,
    b.description,
    b.path,
    b.status,
    m.new_id,
    b.git_repository_url,
    b.git_branch,
    b.git_username,
    b.git_email,
    b.git_access_token,
    b.created_at,
    b.updated_at
FROM collaborations_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

-- 创建索引
CREATE INDEX ix_collaborations_user_id ON collaborations(user_id);
CREATE INDEX ix_collaborations_status ON collaborations(status);

-- 备份并更新其他表
-- operation_logs
CREATE TABLE operation_logs_backup AS SELECT * FROM operation_logs;
DROP TABLE operation_logs CASCADE;
CREATE TABLE operation_logs (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    action VARCHAR(50) NOT NULL,
    resource_type VARCHAR(50) NOT NULL,
    resource_id VARCHAR(36),
    description TEXT,
    details TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO operation_logs (id, user_id, action, resource_type, resource_id, description, details, created_at)
SELECT 
    b.id,
    m.new_id,
    b.action,
    b.resource_type,
    b.resource_id,
    b.description,
    b.details,
    b.created_at
FROM operation_logs_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

CREATE INDEX ix_operation_logs_user_id ON operation_logs(user_id);
CREATE INDEX ix_operation_logs_created_at ON operation_logs(created_at);

-- rag_documents
CREATE TABLE rag_documents_backup AS SELECT * FROM rag_documents;
DROP TABLE rag_documents CASCADE;
CREATE TABLE rag_documents (
    id BIGINT PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500),
    file_type VARCHAR(50),
    file_size BIGINT NOT NULL DEFAULT 0,
    status INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    user_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP
);

INSERT INTO rag_documents (id, file_name, file_path, file_type, file_size, status, error_message, user_id, created_at, processed_at)
SELECT 
    b.id,
    b.file_name,
    b.file_path,
    b.file_type,
    b.file_size,
    b.status,
    b.error_message,
    m.new_id,
    b.created_at,
    b.processed_at
FROM rag_documents_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

CREATE INDEX ix_rag_documents_file_name ON rag_documents(file_name);
CREATE INDEX ix_rag_documents_status ON rag_documents(status);
CREATE INDEX ix_rag_documents_user_id ON rag_documents(user_id);

-- system_logs
CREATE TABLE system_logs_backup AS SELECT * FROM system_logs;
DROP TABLE system_logs CASCADE;
CREATE TABLE system_logs (
    id BIGINT PRIMARY KEY,
    level VARCHAR(20) NOT NULL DEFAULT 'Info',
    category VARCHAR(100) NOT NULL,
    message TEXT NOT NULL,
    exception TEXT,
    stack_trace TEXT,
    user_id BIGINT,
    request_path VARCHAR(500),
    additional_data TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO system_logs (id, level, category, message, exception, stack_trace, user_id, request_path, additional_data, created_at)
SELECT 
    b.id,
    b.level,
    b.category,
    b.message,
    b.exception,
    b.stack_trace,
    m.new_id,
    b.request_path,
    b.additional_data,
    b.created_at
FROM system_logs_backup b
LEFT JOIN user_id_mapping m ON b.user_id = m.old_id;

CREATE INDEX ix_system_logs_level ON system_logs(level);
CREATE INDEX ix_system_logs_category ON system_logs(category);
CREATE INDEX ix_system_logs_created_at ON system_logs(created_at);
CREATE INDEX ix_system_logs_user_id ON system_logs(user_id);

-- llm_test_records
CREATE TABLE llm_test_records_backup AS SELECT * FROM llm_test_records;
DROP TABLE llm_test_records CASCADE;
CREATE TABLE llm_test_records (
    id BIGINT PRIMARY KEY,
    llm_config_id BIGINT NOT NULL,
    llm_model_config_id BIGINT,
    prompt TEXT NOT NULL,
    response TEXT,
    success BOOLEAN NOT NULL DEFAULT false,
    error_message TEXT,
    tokens_used INTEGER NOT NULL DEFAULT 0,
    response_time_ms INTEGER NOT NULL DEFAULT 0,
    user_id BIGINT NOT NULL,
    tested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO llm_test_records (id, llm_config_id, llm_model_config_id, prompt, response, success, error_message, tokens_used, response_time_ms, user_id, tested_at)
SELECT 
    b.id,
    b.llm_config_id,
    b.llm_model_config_id,
    b.prompt,
    b.response,
    b.success,
    b.error_message,
    b.tokens_used,
    b.response_time_ms,
    m.new_id,
    b.tested_at
FROM llm_test_records_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

CREATE INDEX ix_llm_test_records_llm_config_id ON llm_test_records(llm_config_id);
CREATE INDEX ix_llm_test_records_tested_at ON llm_test_records(tested_at);

-- user_roles
CREATE TABLE user_roles_backup AS SELECT * FROM user_roles;
DROP TABLE user_roles CASCADE;
CREATE TABLE user_roles (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, role_id)
);

INSERT INTO user_roles (id, user_id, role_id, created_at)
SELECT 
    b.id,
    m.new_id,
    b.role_id,
    b.created_at
FROM user_roles_backup b
INNER JOIN user_id_mapping m ON b.user_id = m.old_id;

CREATE INDEX ix_user_roles_user_id ON user_roles(user_id);
CREATE INDEX ix_user_roles_role_id ON user_roles(role_id);

-- agent_messages (user_id 可为空)
CREATE TABLE agent_messages_backup AS SELECT * FROM agent_messages;
DROP TABLE agent_messages CASCADE;
CREATE TABLE agent_messages (
    id BIGINT PRIMARY KEY,
    from_agent_id BIGINT,
    to_agent_id BIGINT,
    collaboration_id BIGINT NOT NULL,
    content TEXT NOT NULL,
    sender_type INTEGER NOT NULL DEFAULT 0,
    sender_name VARCHAR(100),
    user_id BIGINT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_streaming BOOLEAN NOT NULL DEFAULT false
);

INSERT INTO agent_messages (id, from_agent_id, to_agent_id, collaboration_id, content, sender_type, sender_name, user_id, created_at, is_streaming)
SELECT 
    b.id,
    b.from_agent_id,
    b.to_agent_id,
    b.collaboration_id,
    b.content,
    b.sender_type,
    b.sender_name,
    m.new_id,
    b.created_at,
    b.is_streaming
FROM agent_messages_backup b
LEFT JOIN user_id_mapping m ON b.user_id = m.old_id;

CREATE INDEX ix_agent_messages_collaboration_id ON agent_messages(collaboration_id);
CREATE INDEX ix_agent_messages_created_at ON agent_messages(created_at);

-- collaboration_tasks (assigned_to 可为空)
CREATE TABLE collaboration_tasks_backup AS SELECT * FROM collaboration_tasks;
DROP TABLE collaboration_tasks CASCADE;
CREATE TABLE collaboration_tasks (
    id BIGINT PRIMARY KEY,
    collaboration_id BIGINT NOT NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    status INTEGER NOT NULL DEFAULT 0,
    assigned_to BIGINT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);

INSERT INTO collaboration_tasks (id, collaboration_id, title, description, status, assigned_to, created_at, completed_at)
SELECT 
    b.id,
    b.collaboration_id,
    b.title,
    b.description,
    b.status,
    m.new_id,
    b.created_at,
    b.completed_at
FROM collaboration_tasks_backup b
LEFT JOIN user_id_mapping m ON b.assigned_to = m.old_id;

CREATE INDEX ix_collaboration_tasks_collaboration_id ON collaboration_tasks(collaboration_id);

-- =============================================
-- 第四步：删除旧表，重命名新表
-- =============================================

DROP TABLE users CASCADE;
ALTER TABLE users_new RENAME TO users;

-- =============================================
-- 第五步：清理备份表
-- =============================================

DROP TABLE llm_configs_backup;
DROP TABLE agents_backup;
DROP TABLE collaborations_backup;
DROP TABLE operation_logs_backup;
DROP TABLE rag_documents_backup;
DROP TABLE system_logs_backup;
DROP TABLE llm_test_records_backup;
DROP TABLE user_roles_backup;
DROP TABLE agent_messages_backup;
DROP TABLE collaboration_tasks_backup;

-- 验证迁移结果
DO $$
DECLARE
    user_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO user_count FROM users;
    RAISE NOTICE '迁移完成！users表共有 % 条记录', user_count;
END $$;
