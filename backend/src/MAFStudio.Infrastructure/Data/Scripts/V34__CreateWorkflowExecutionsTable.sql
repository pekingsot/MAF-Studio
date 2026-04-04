-- 工作流执行记录表
CREATE TABLE IF NOT EXISTS workflow_executions (
    id BIGINT PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    task_id BIGINT REFERENCES collaboration_tasks(id) ON DELETE SET NULL,
    workflow_type VARCHAR(50) NOT NULL,
    input TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_workflow_executions_collaboration_id ON workflow_executions(collaboration_id);
CREATE INDEX IF NOT EXISTS ix_workflow_executions_status ON workflow_executions(status);
CREATE INDEX IF NOT EXISTS ix_workflow_executions_task_id ON workflow_executions(task_id);

-- 工作流执行消息表
CREATE TABLE IF NOT EXISTS workflow_execution_messages (
    id BIGINT PRIMARY KEY,
    execution_id BIGINT NOT NULL REFERENCES workflow_executions(id) ON DELETE CASCADE,
    sender VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    role VARCHAR(50) NOT NULL,
    timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_workflow_execution_messages_execution_id ON workflow_execution_messages(execution_id);
CREATE INDEX IF NOT EXISTS ix_workflow_execution_messages_timestamp ON workflow_execution_messages(timestamp);
