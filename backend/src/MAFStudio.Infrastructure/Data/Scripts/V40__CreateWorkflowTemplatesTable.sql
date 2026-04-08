-- 创建工作流模板表
CREATE TABLE IF NOT EXISTS workflow_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    tags JSONB DEFAULT '[]'::jsonb,
    
    -- 工作流定义（JSON格式）
    workflow_definition JSONB NOT NULL,
    
    -- 参数定义
    parameters JSONB DEFAULT '{}'::jsonb,
    
    -- 元数据
    created_by BIGINT,
    is_public BOOLEAN DEFAULT false,
    usage_count INTEGER DEFAULT 0,
    
    -- Magentic学习相关
    source VARCHAR(50) DEFAULT 'manual',
    original_task TEXT,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_workflow_templates_category ON workflow_templates(category);
CREATE INDEX IF NOT EXISTS idx_workflow_templates_created_by ON workflow_templates(created_by);
CREATE INDEX IF NOT EXISTS idx_workflow_templates_is_public ON workflow_templates(is_public);
CREATE INDEX IF NOT EXISTS idx_workflow_templates_tags ON workflow_templates USING GIN(tags);

-- 添加注释
COMMENT ON TABLE workflow_templates IS '工作流模板表';
COMMENT ON COLUMN workflow_templates.name IS '模板名称';
COMMENT ON COLUMN workflow_templates.description IS '模板描述';
COMMENT ON COLUMN workflow_templates.category IS '模板分类';
COMMENT ON COLUMN workflow_templates.tags IS '标签（JSON数组）';
COMMENT ON COLUMN workflow_templates.workflow_definition IS '工作流定义（JSON格式）';
COMMENT ON COLUMN workflow_templates.parameters IS '参数定义（JSON对象）';
COMMENT ON COLUMN workflow_templates.created_by IS '创建者ID';
COMMENT ON COLUMN workflow_templates.is_public IS '是否公开';
COMMENT ON COLUMN workflow_templates.usage_count IS '使用次数';
COMMENT ON COLUMN workflow_templates.source IS '来源：manual（手动创建）、magentic（Magentic生成）、magentic_optimized（Magentic优化后）';
COMMENT ON COLUMN workflow_templates.original_task IS '原始任务描述（如果是Magentic生成的）';
