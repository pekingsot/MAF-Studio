-- 创建工作流计划表
CREATE TABLE IF NOT EXISTS workflow_plans (
    id BIGSERIAL PRIMARY KEY,
    collaboration_id BIGINT NOT NULL REFERENCES collaborations(id) ON DELETE CASCADE,
    task TEXT NOT NULL,
    workflow_definition JSONB NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    created_by BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    approved_at TIMESTAMP,
    approved_by BIGINT,
    executed_at TIMESTAMP,
    completed_at TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_workflow_plans_collaboration_id ON workflow_plans(collaboration_id);
CREATE INDEX IF NOT EXISTS idx_workflow_plans_status ON workflow_plans(status);
CREATE INDEX IF NOT EXISTS idx_workflow_plans_created_at ON workflow_plans(created_at);

-- 添加注释
COMMENT ON TABLE workflow_plans IS '工作流计划表，用于保存Magentic工作流生成的计划';
COMMENT ON COLUMN workflow_plans.id IS '主键ID';
COMMENT ON COLUMN workflow_plans.collaboration_id IS '协作ID';
COMMENT ON COLUMN workflow_plans.task IS '任务描述';
COMMENT ON COLUMN workflow_plans.workflow_definition IS '工作流定义（JSON格式）';
COMMENT ON COLUMN workflow_plans.status IS '状态：pending-待审核, approved-已批准, rejected-已拒绝, executing-执行中, completed-已完成';
COMMENT ON COLUMN workflow_plans.created_by IS '创建人ID';
COMMENT ON COLUMN workflow_plans.created_at IS '创建时间';
COMMENT ON COLUMN workflow_plans.updated_at IS '更新时间';
COMMENT ON COLUMN workflow_plans.approved_at IS '批准时间';
COMMENT ON COLUMN workflow_plans.approved_by IS '批准人ID';
COMMENT ON COLUMN workflow_plans.executed_at IS '执行时间';
COMMENT ON COLUMN workflow_plans.completed_at IS '完成时间';
