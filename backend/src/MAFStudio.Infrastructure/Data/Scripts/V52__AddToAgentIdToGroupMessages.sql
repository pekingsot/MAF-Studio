ALTER TABLE group_messages ADD COLUMN IF NOT EXISTS to_agent_id BIGINT;

COMMENT ON COLUMN group_messages.to_agent_id IS '私聊目标Agent ID（仅私聊消息有值）';

CREATE INDEX IF NOT EXISTS ix_group_messages_to_agent_id ON group_messages(to_agent_id);
