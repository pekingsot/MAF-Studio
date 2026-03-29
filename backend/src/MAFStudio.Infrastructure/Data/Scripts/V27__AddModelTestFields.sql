-- V27: 为模型配置表添加测试相关字段

-- 添加最后测试时间字段
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS last_test_time TIMESTAMP;

-- 添加可用状态字段（0: 不可用, 1: 可用）
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS availability_status INTEGER DEFAULT 0;

-- 添加测试结果字段（记录测试失败原因，成功时显示延迟毫秒数）
ALTER TABLE llm_model_configs ADD COLUMN IF NOT EXISTS test_result VARCHAR(500);

-- 添加注释
COMMENT ON COLUMN llm_model_configs.last_test_time IS '最后测试时间';
COMMENT ON COLUMN llm_model_configs.availability_status IS '可用状态：0-不可用，1-可用';
COMMENT ON COLUMN llm_model_configs.test_result IS '测试结果：失败原因或成功延迟(ms)';
