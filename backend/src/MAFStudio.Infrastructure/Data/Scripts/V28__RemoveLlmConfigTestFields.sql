-- V28: 从大模型配置表移除测试字段（测试字段已移至模型配置表）

-- 删除测试时间字段
ALTER TABLE llm_configs DROP COLUMN IF EXISTS last_test_time;

-- 删除可用状态字段
ALTER TABLE llm_configs DROP COLUMN IF EXISTS availability_status;
