-- 创建全局ID序列（如果不存在）
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE sequencename = 'global_id_seq') THEN
        CREATE SEQUENCE global_id_seq START 1000000000;
    END IF;
END $$;

-- 协调会话表 - 记录每次协调的完整过程
CREATE TABLE IF NOT EXISTS coordination_sessions (
    id BIGINT PRIMARY KEY DEFAULT nextval('global_id_seq'),
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    workflow_execution_id BIGINT REFERENCES workflow_executions(id) ON DELETE SET NULL,
    orchestration_mode VARCHAR(50) NOT NULL DEFAULT 'RoundRobin',
    status VARCHAR(20) NOT NULL DEFAULT 'running',
    topic TEXT,
    start_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP,
    total_rounds INTEGER NOT NULL DEFAULT 0,
    total_messages INTEGER NOT NULL DEFAULT 0,
    conclusion TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_coordination_sessions_collaboration_id ON coordination_sessions(collaboration_id);
CREATE INDEX IF NOT EXISTS ix_coordination_sessions_status ON coordination_sessions(status);
CREATE INDEX IF NOT EXISTS ix_coordination_sessions_start_time ON coordination_sessions(start_time);

-- 协调轮次表 - 记录每轮发言的详细信息
CREATE TABLE IF NOT EXISTS coordination_rounds (
    id BIGINT PRIMARY KEY DEFAULT nextval('global_id_seq'),
    session_id BIGINT NOT NULL REFERENCES coordination_sessions(id) ON DELETE CASCADE,
    round_number INTEGER NOT NULL,
    speaker_agent_id BIGINT REFERENCES agents(id) ON DELETE SET NULL,
    speaker_name VARCHAR(100) NOT NULL,
    speaker_role VARCHAR(100),
    message_content TEXT NOT NULL,
    message_id BIGINT REFERENCES agent_messages(id) ON DELETE SET NULL,
    thinking_process TEXT,
    selected_next_speaker VARCHAR(100),
    selection_reason TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_coordination_rounds_session_id ON coordination_rounds(session_id);
CREATE INDEX IF NOT EXISTS ix_coordination_rounds_round_number ON coordination_rounds(session_id, round_number);
CREATE INDEX IF NOT EXISTS ix_coordination_rounds_speaker_agent_id ON coordination_rounds(speaker_agent_id);

-- 协调参与者表 - 记录参与协调的智能体
CREATE TABLE IF NOT EXISTS coordination_participants (
    id BIGINT PRIMARY KEY DEFAULT nextval('global_id_seq'),
    session_id BIGINT NOT NULL REFERENCES coordination_sessions(id) ON DELETE CASCADE,
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

COMMENT ON TABLE coordination_sessions IS '协调会话表 - 记录每次协调的完整过程';
COMMENT ON TABLE coordination_rounds IS '协调轮次表 - 记录每轮发言的详细信息';
COMMENT ON TABLE coordination_participants IS '协调参与者表 - 记录参与协调的智能体';
