-- 为 coordination_sessions 表添加 metadata 字段存储执行时的冗余信息
ALTER TABLE coordination_sessions ADD COLUMN IF NOT EXISTS metadata TEXT NULL;

-- 添加注释
COMMENT ON COLUMN coordination_sessions.metadata IS '执行时的元数据信息(JSON格式):协调者、执行者、工作流参数等';
