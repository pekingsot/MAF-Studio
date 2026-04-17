ALTER TABLE group_messages ADD COLUMN IF NOT EXISTS llm_config_name VARCHAR(200);

COMMENT ON COLUMN group_messages.llm_config_name IS '大模型配置名称';
