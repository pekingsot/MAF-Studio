-- MAF Studio 基础数据初始化脚本
-- 此文件仅作为占位符，实际种子数据在 V4 中
-- 因为 V3 会清理表结构

-- 系统配置
INSERT INTO system_configs (id, key, value, description, created_at) VALUES
(5000000000000001, 'system.name', 'MAF Studio', '系统名称', CURRENT_TIMESTAMP),
(5000000000000002, 'system.version', '1.1.0', '系统版本', CURRENT_TIMESTAMP),
(5000000000000003, 'system.description', '多智能体协作平台', '系统描述', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;
