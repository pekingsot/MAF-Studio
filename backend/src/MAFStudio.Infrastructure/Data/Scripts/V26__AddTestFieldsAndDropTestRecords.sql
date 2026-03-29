-- V26: 为大模型配置表添加测试时间和可用状态字段，并删除测试记录表

-- 添加测试时间字段
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS last_test_time TIMESTAMP;

-- 添加可用状态字段（0: 不可用, 1: 可用）
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS availability_status INTEGER DEFAULT 0;

-- 添加注释
COMMENT ON COLUMN llm_configs.last_test_time IS '最后测试时间';
COMMENT ON COLUMN llm_configs.availability_status IS '可用状态：0-不可用，1-可用';

-- 删除测试记录表
DROP TABLE IF EXISTS llm_test_records CASCADE;
