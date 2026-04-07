-- 为collaboration_tasks表添加prompt字段
-- 用于存储任务级别的提示词，与Agent的custom_prompt组合使用

ALTER TABLE collaboration_tasks 
ADD COLUMN IF NOT EXISTS prompt TEXT;

COMMENT ON COLUMN collaboration_tasks.prompt IS '任务级别的提示词，会与Agent的custom_prompt组合使用';

-- 为现有任务添加默认提示词模板
UPDATE collaboration_tasks 
SET prompt = '【任务要求】
请各位团队成员根据任务描述，积极参与讨论并提交自己的专业观点。

【Git提交要求】
讨论结束后，每个成员必须将自己的观点文档提交到Git仓库：
1. 克隆仓库
2. 在docs目录下创建以自己名字命名的文档
3. 提交并推送到远程仓库

【注意事项】
- 文档内容要体现专业见解
- 必须真实调用Git工具提交
- 不要只是说"已提交"'
WHERE prompt IS NULL;
