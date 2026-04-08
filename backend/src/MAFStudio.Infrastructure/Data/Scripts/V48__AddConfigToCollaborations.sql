-- V48: 为 collaborations 表添加 config JSONB 字段

-- 添加 config 字段
ALTER TABLE collaborations ADD COLUMN IF NOT EXISTS config JSONB;

-- 添加注释
COMMENT ON COLUMN collaborations.config IS '协作配置，JSON格式，包含SMTP配置、其他工具配置等';

-- 创建 GIN 索引以优化 JSONB 查询
CREATE INDEX IF NOT EXISTS ix_collaborations_config ON collaborations USING GIN(config);

-- 示例配置结构说明：
-- {
--   "smtp": {
--     "server": "smtp.qq.com",
--     "port": 587,
--     "username": "284184032@qq.com",
--     "password": "xqjrxtrgjzncbhca",
--     "fromEmail": "284184032@qq.com",
--     "enableSsl": true
--   },
--   "otherSettings": {
--     "key": "value"
--   }
-- }
