-- 手动添加 custom_prompt 字段到 collaboration_agents 表
-- 请在数据库管理工具中执行此SQL

-- 添加字段（如果不存在）
ALTER TABLE collaboration_agents 
ADD COLUMN IF NOT EXISTS custom_prompt TEXT;

-- 验证字段是否添加成功
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'collaboration_agents' 
AND column_name = 'custom_prompt';

-- 查看表的所有列
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'collaboration_agents' 
ORDER BY ordinal_position;
