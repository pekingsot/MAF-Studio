-- V15__FixOperationLogsColumns.sql
-- 修复operation_logs表缺少的列

-- 添加缺失的列
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS ip_address VARCHAR(50);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS user_agent VARCHAR(500);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS request_path VARCHAR(500);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS request_method VARCHAR(10);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS status_code INTEGER;
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS duration_ms BIGINT;
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS error_message TEXT;

-- 创建索引
CREATE INDEX IF NOT EXISTS ix_operation_logs_action ON operation_logs(action);
CREATE INDEX IF NOT EXISTS ix_operation_logs_resource_type ON operation_logs(resource_type);
