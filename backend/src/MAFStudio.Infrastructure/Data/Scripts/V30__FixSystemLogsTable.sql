ALTER TABLE system_logs ALTER COLUMN level SET DEFAULT 'Information';

ALTER TABLE system_logs ADD COLUMN IF NOT EXISTS request_method VARCHAR(10);
ALTER TABLE system_logs ADD COLUMN IF NOT EXISTS source VARCHAR(200);

CREATE INDEX IF NOT EXISTS ix_system_logs_created_at_desc ON system_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_system_logs_level_created_at ON system_logs(level, created_at DESC);
