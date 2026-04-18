CREATE TABLE IF NOT EXISTS coordination_participants (
    id BIGINT PRIMARY KEY DEFAULT nextval('global_id_seq'),
    session_id BIGINT NOT NULL REFERENCES workflow_sessions(id) ON DELETE CASCADE,
    agent_id BIGINT NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    agent_name VARCHAR(100) NOT NULL,
    agent_role VARCHAR(100),
    is_manager BOOLEAN NOT NULL DEFAULT false,
    speak_count INTEGER NOT NULL DEFAULT 0,
    total_tokens INTEGER NOT NULL DEFAULT 0,
    joined_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_coordination_participants_session_agent ON coordination_participants(session_id, agent_id);
CREATE INDEX IF NOT EXISTS ix_coordination_participants_agent_id ON coordination_participants(agent_id);

COMMENT ON TABLE coordination_participants IS '协调参与者表 - 记录参与协调的智能体';
