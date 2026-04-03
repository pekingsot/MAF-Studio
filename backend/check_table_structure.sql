-- 检查 collaboration_agents 表结构
-- 请在数据库管理工具中执行此SQL

-- 1. 查看表的所有列
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'collaboration_agents' 
ORDER BY ordinal_position;

-- 2. 查看最近的迁移记录
SELECT script_name, executed_at 
FROM __migration_history 
WHERE script_name LIKE 'V3%' 
ORDER BY executed_at DESC;

-- 3. 如果 custom_prompt 字段不存在，手动添加
-- ALTER TABLE collaboration_agents ADD COLUMN IF NOT EXISTS custom_prompt TEXT;
