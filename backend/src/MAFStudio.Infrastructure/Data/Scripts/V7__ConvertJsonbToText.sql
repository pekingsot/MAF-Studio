-- V7__ConvertJsonbToText.sql
-- 将所有JSONB字段转换为TEXT类型，避免类型转换问题

-- 1. llm_configs 表
ALTER TABLE llm_configs ALTER COLUMN extra_config TYPE TEXT;

-- 2. llm_model_configs 表
ALTER TABLE llm_model_configs ALTER COLUMN stop_sequences TYPE TEXT;
ALTER TABLE llm_model_configs ALTER COLUMN extra_config TYPE TEXT;

-- 3. agent_types 表
ALTER TABLE agent_types ALTER COLUMN default_configuration TYPE TEXT;

-- 4. agents 表
ALTER TABLE agents ALTER COLUMN configuration TYPE TEXT;

-- 5. operation_logs 表
ALTER TABLE operation_logs ALTER COLUMN details TYPE TEXT;

-- 6. system_logs 表
ALTER TABLE system_logs ALTER COLUMN additional_data TYPE TEXT;

-- 7. collaborations 表 (如果存在)
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'collaborations' AND column_name = 'metadata') THEN
        ALTER TABLE collaborations ALTER COLUMN metadata TYPE TEXT;
    END IF;
END $$;
