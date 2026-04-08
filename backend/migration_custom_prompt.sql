-- 执行此SQL脚本以添加custom_prompt字段到collaboration_agents表
-- 数据库连接信息：
-- Host: 192.168.1.250
-- Port: 5433
-- Database: mafstudio
-- Username: pekingsot

-- 1. 添加custom_prompt字段
ALTER TABLE collaboration_agents 
ADD COLUMN IF NOT EXISTS custom_prompt TEXT;

-- 2. 更新字段注释
COMMENT ON COLUMN collaboration_agents.role IS 'Agent在工作流中的角色：Manager（协调者）或 Worker（执行者）';
COMMENT ON COLUMN collaboration_agents.custom_prompt IS 'Agent的自定义提示词，用于覆盖系统提示词';

-- 3. 验证字段是否添加成功
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'collaboration_agents' 
AND column_name IN ('role', 'custom_prompt');
