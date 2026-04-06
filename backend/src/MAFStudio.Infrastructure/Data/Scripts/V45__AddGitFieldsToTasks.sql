-- V45__AddGitFieldsToTasks.sql
-- 为任务表添加Git仓库相关字段

-- 添加Git相关字段
ALTER TABLE collaboration_tasks
ADD COLUMN IF NOT EXISTS git_url TEXT,
ADD COLUMN IF NOT EXISTS git_branch VARCHAR(100),
ADD COLUMN IF NOT EXISTS git_credentials TEXT;

-- 添加注释
COMMENT ON COLUMN collaboration_tasks.git_url IS 'Git仓库地址';
COMMENT ON COLUMN collaboration_tasks.git_branch IS '目标分支';
COMMENT ON COLUMN collaboration_tasks.git_credentials IS 'Git凭证（加密存储的JSON）';
