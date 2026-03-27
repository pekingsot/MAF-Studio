-- V6: 修复缺失字段并更新种子数据

-- 1. 确保 agent_types 表有所有必要字段
ALTER TABLE agent_types ADD COLUMN IF NOT EXISTS is_system BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE agent_types ADD COLUMN IF NOT EXISTS is_enabled BOOLEAN NOT NULL DEFAULT true;
ALTER TABLE agent_types ADD COLUMN IF NOT EXISTS sort_order INTEGER NOT NULL DEFAULT 0;

-- 2. 确保 llm_configs 表有所有必要字段
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS is_default BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE llm_configs ADD COLUMN IF NOT EXISTS is_enabled BOOLEAN NOT NULL DEFAULT true;

-- 3. 确保 llm_model_configs 表存在并有所有字段
CREATE TABLE IF NOT EXISTS llm_model_configs (
    id BIGINT PRIMARY KEY,
    llm_config_id BIGINT NOT NULL REFERENCES llm_configs(id) ON DELETE CASCADE,
    model_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(100),
    description TEXT,
    is_default BOOLEAN NOT NULL DEFAULT false,
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    sort_order INTEGER NOT NULL DEFAULT 0,
    temperature DECIMAL(3,2) NOT NULL DEFAULT 0.70,
    max_tokens INTEGER NOT NULL DEFAULT 4096,
    context_window INTEGER NOT NULL DEFAULT 8192,
    top_p DECIMAL(3,2),
    frequency_penalty DECIMAL(3,2),
    presence_penalty DECIMAL(3,2),
    stop_sequences TEXT,
    extra_config JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_llm_model_configs_llm_config_id ON llm_model_configs(llm_config_id);

-- 4. 更新系统用户
INSERT INTO users (id, username, email, password_hash, role, avatar, created_at, updated_at) 
VALUES ('00000000-0000-0000-0000-000000000003', 'system', 'system@mafstudio.com', '', 'system', '⚙️', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (username) DO UPDATE SET 
    email = EXCLUDED.email,
    role = EXCLUDED.role,
    avatar = EXCLUDED.avatar;

-- 5. 更新智能体类型种子数据（带默认提示词）
INSERT INTO agent_types (id, name, code, description, icon, default_configuration, is_system, is_enabled, sort_order, created_at) VALUES
(2000000000000001, '产品经理', 'product_manager', '负责产品规划、需求分析和用户研究', '📋', '{"systemPrompt": "你是一位经验丰富的产品经理，擅长用户需求分析、产品规划和项目管理。你需要帮助用户进行产品思考、需求梳理和方案设计。", "temperature": 0.7, "maxTokens": 4096}', true, true, 1, CURRENT_TIMESTAMP),
(2000000000000002, '前端工程师', 'frontend_engineer', '负责用户界面开发和用户体验优化', '🎨', '{"systemPrompt": "你是一位专业的前端工程师，精通React、Vue、TypeScript等现代前端技术栈。你需要帮助用户解决前端开发问题、优化用户界面和提升用户体验。", "temperature": 0.7, "maxTokens": 4096}', true, true, 2, CURRENT_TIMESTAMP),
(2000000000000003, '后端工程师', 'backend_engineer', '负责服务端开发和系统架构设计', '⚙️', '{"systemPrompt": "你是一位资深后端工程师，精通Java、Python、Go等后端语言，熟悉微服务架构、数据库设计和性能优化。你需要帮助用户解决后端开发问题、设计系统架构和优化性能。", "temperature": 0.7, "maxTokens": 4096}', true, true, 3, CURRENT_TIMESTAMP),
(2000000000000004, '全栈工程师', 'fullstack_engineer', '负责前后端全栈开发', '🔧', '{"systemPrompt": "你是一位全栈工程师，精通前后端开发技术，能够独立完成从需求分析到部署上线的全流程开发。你需要帮助用户解决全栈开发问题、设计完整的技术方案。", "temperature": 0.7, "maxTokens": 4096}', true, true, 4, CURRENT_TIMESTAMP),
(2000000000000005, 'UI设计师', 'ui_designer', '负责用户界面设计和视觉设计', '🎭', '{"systemPrompt": "你是一位专业的UI设计师，擅长用户界面设计、交互设计和视觉设计。你需要帮助用户进行界面设计、优化用户体验和提升产品视觉效果。", "temperature": 0.8, "maxTokens": 4096}', true, true, 5, CURRENT_TIMESTAMP),
(2000000000000006, '数据分析师', 'data_analyst', '负责数据分析和业务洞察', '📊', '{"systemPrompt": "你是一位专业的数据分析师，精通数据分析方法、统计建模和数据可视化。你需要帮助用户进行数据分析、发现业务洞察和提供决策支持。", "temperature": 0.6, "maxTokens": 4096}', true, true, 6, CURRENT_TIMESTAMP),
(2000000000000007, '测试工程师', 'qa_engineer', '负责软件测试和质量保证', '🔍', '{"systemPrompt": "你是一位专业的测试工程师，精通测试方法论、自动化测试和性能测试。你需要帮助用户设计测试方案、编写测试用例和保障产品质量。", "temperature": 0.5, "maxTokens": 4096}', true, true, 7, CURRENT_TIMESTAMP),
(2000000000000008, '运维工程师', 'devops_engineer', '负责系统运维和DevOps', '🚀', '{"systemPrompt": "你是一位专业的运维工程师，精通Linux系统、容器技术、CI/CD和云服务。你需要帮助用户解决运维问题、优化系统架构和提升部署效率。", "temperature": 0.6, "maxTokens": 4096}', true, true, 8, CURRENT_TIMESTAMP),
(2000000000000009, '架构师', 'architect', '负责系统架构设计和技术选型', '🏗️', '{"systemPrompt": "你是一位资深架构师，精通分布式系统、微服务架构和技术选型。你需要帮助用户进行系统架构设计、技术方案评估和架构优化。", "temperature": 0.6, "maxTokens": 8192}', true, true, 9, CURRENT_TIMESTAMP),
(2000000000000010, '技术总监', 'cto', '负责技术战略和团队管理', '👔', '{"systemPrompt": "你是一位技术总监，具有丰富的技术管理经验，擅长技术战略规划、团队建设和技术决策。你需要帮助用户进行技术规划、团队管理决策和技术方向把控。", "temperature": 0.6, "maxTokens": 8192}', true, true, 10, CURRENT_TIMESTAMP),
(2000000000000011, '项目经理', 'project_manager', '负责项目管理和进度控制', '📅', '{"systemPrompt": "你是一位专业的项目经理，精通项目管理方法论、敏捷开发和风险管理。你需要帮助用户进行项目规划、进度控制和风险应对。", "temperature": 0.6, "maxTokens": 4096}', true, true, 11, CURRENT_TIMESTAMP),
(2000000000000012, '安全工程师', 'security_engineer', '负责系统安全和安全审计', '🔒', '{"systemPrompt": "你是一位专业的安全工程师，精通网络安全、应用安全和安全审计。你需要帮助用户识别安全风险、设计安全方案和进行安全评估。", "temperature": 0.5, "maxTokens": 4096}', true, true, 12, CURRENT_TIMESTAMP),
(2000000000000013, '算法工程师', 'algorithm_engineer', '负责算法研发和模型优化', '🧮', '{"systemPrompt": "你是一位专业的算法工程师，精通机器学习、深度学习和数据挖掘。你需要帮助用户进行算法设计、模型优化和解决复杂计算问题。", "temperature": 0.6, "maxTokens": 8192}', true, true, 13, CURRENT_TIMESTAMP),
(2000000000000014, '内容运营', 'content_operator', '负责内容策划和运营', '✍️', '{"systemPrompt": "你是一位专业的内容运营，擅长内容策划、文案写作和用户运营。你需要帮助用户进行内容创作、运营策略制定和用户增长。", "temperature": 0.8, "maxTokens": 4096}', true, true, 14, CURRENT_TIMESTAMP),
(2000000000000015, '客服专员', 'customer_service', '负责客户服务和问题解答', '💬', '{"systemPrompt": "你是一位专业的客服专员，擅长客户沟通、问题解答和服务质量提升。你需要帮助用户解决客户问题、提升服务满意度和处理投诉。", "temperature": 0.7, "maxTokens": 4096}', true, true, 15, CURRENT_TIMESTAMP),
(2000000000000016, '销售顾问', 'sales_consultant', '负责销售和客户关系', '💼', '{"systemPrompt": "你是一位专业的销售顾问，擅长客户开发、销售谈判和客户关系维护。你需要帮助用户进行销售策略制定、客户沟通和业绩提升。", "temperature": 0.7, "maxTokens": 4096}', true, true, 16, CURRENT_TIMESTAMP),
(2000000000000017, '人力资源', 'hr_specialist', '负责招聘和员工管理', '👥', '{"systemPrompt": "你是一位专业的人力资源专员，擅长招聘、员工关系和绩效管理。你需要帮助用户进行人才招聘、员工管理和人力资源规划。", "temperature": 0.6, "maxTokens": 4096}', true, true, 17, CURRENT_TIMESTAMP),
(2000000000000018, '财务分析师', 'financial_analyst', '负责财务分析和预算管理', '💰', '{"systemPrompt": "你是一位专业的财务分析师，精通财务分析、预算管理和投资评估。你需要帮助用户进行财务分析、预算规划和投资决策支持。", "temperature": 0.5, "maxTokens": 4096}', true, true, 18, CURRENT_TIMESTAMP),
(2000000000000019, '法务顾问', 'legal_advisor', '负责法律事务和合规', '⚖️', '{"systemPrompt": "你是一位专业的法务顾问，精通公司法、合同法和知识产权保护。你需要帮助用户处理法律事务、审核合同和提供合规建议。", "temperature": 0.5, "maxTokens": 4096}', true, true, 19, CURRENT_TIMESTAMP),
(2000000000000020, '通用助手', 'general_assistant', '通用智能助手，可处理各类问题', '🤖', '{"systemPrompt": "你是一位智能助手，具有广泛的知识储备，能够帮助用户解答各类问题、提供建议和协助完成任务。", "temperature": 0.7, "maxTokens": 4096}', true, true, 20, CURRENT_TIMESTAMP)
ON CONFLICT (code) DO UPDATE SET 
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    icon = EXCLUDED.icon,
    default_configuration = EXCLUDED.default_configuration,
    is_system = EXCLUDED.is_system,
    is_enabled = EXCLUDED.is_enabled,
    sort_order = EXCLUDED.sort_order;

-- 6. 更新LLM配置种子数据
INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, extra_config, user_id, is_default, is_enabled, created_at, updated_at) VALUES
(1000000000000001, '阿里千问', 'Qwen', '', 'https://dashscope.aliyuncs.com/compatible-mode/v1', 'qwen-max', 
 '{"models": ["qwen-max", "qwen-plus", "qwen-turbo", "qwen-long"], "supports_streaming": true, "supports_vision": true}', 
 '00000000-0000-0000-0000-000000000003', false, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(1000000000000002, '智谱AI', 'Zhipu', '', 'https://open.bigmodel.cn/api/paas/v4', 'glm-4', 
 '{"models": ["glm-4", "glm-4-flash", "glm-4-plus", "glm-4-air"], "supports_streaming": true, "supports_vision": true}', 
 '00000000-0000-0000-0000-000000000003', false, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(1000000000000003, 'OpenAI兼容', 'OpenAI_Compatible', '', '', 'gpt-4o', 
 '{"models": [], "supports_streaming": true, "supports_vision": true}', 
 '00000000-0000-0000-0000-000000000003', false, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(1000000000000004, 'DeepSeek', 'DeepSeek', '', 'https://api.deepseek.com/v1', 'deepseek-chat', 
 '{"models": ["deepseek-chat", "deepseek-coder"], "supports_streaming": true, "supports_vision": false}', 
 '00000000-0000-0000-0000-000000000003', false, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO UPDATE SET 
    name = EXCLUDED.name,
    provider = EXCLUDED.provider,
    endpoint = EXCLUDED.endpoint,
    default_model = EXCLUDED.default_model,
    extra_config = EXCLUDED.extra_config,
    is_enabled = EXCLUDED.is_enabled;

-- 7. 添加默认模型配置
INSERT INTO llm_model_configs (id, llm_config_id, model_name, display_name, is_default, is_enabled, sort_order, temperature, max_tokens, context_window, created_at) VALUES
(3000000000000001, 1000000000000001, 'qwen-max', '通义千问-Max', true, true, 1, 0.70, 4096, 32768, CURRENT_TIMESTAMP),
(3000000000000002, 1000000000000001, 'qwen-plus', '通义千问-Plus', false, true, 2, 0.70, 4096, 32768, CURRENT_TIMESTAMP),
(3000000000000003, 1000000000000001, 'qwen-turbo', '通义千问-Turbo', false, true, 3, 0.70, 4096, 8192, CURRENT_TIMESTAMP),
(3000000000000004, 1000000000000002, 'glm-4', 'GLM-4', true, true, 1, 0.70, 4096, 128000, CURRENT_TIMESTAMP),
(3000000000000005, 1000000000000002, 'glm-4-flash', 'GLM-4-Flash', false, true, 2, 0.70, 4096, 128000, CURRENT_TIMESTAMP),
(3000000000000006, 1000000000000002, 'glm-4-plus', 'GLM-4-Plus', false, true, 3, 0.70, 4096, 128000, CURRENT_TIMESTAMP),
(3000000000000007, 1000000000000004, 'deepseek-chat', 'DeepSeek Chat', true, true, 1, 0.70, 4096, 64000, CURRENT_TIMESTAMP),
(3000000000000008, 1000000000000004, 'deepseek-coder', 'DeepSeek Coder', false, true, 2, 0.70, 4096, 16000, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- 8. 更新 operation_logs 表，添加API调用相关字段
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS ip_address VARCHAR(50);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS user_agent TEXT;
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS request_path VARCHAR(500);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS request_method VARCHAR(10);
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS status_code INTEGER;
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS duration_ms BIGINT;
ALTER TABLE operation_logs ADD COLUMN IF NOT EXISTS error_message TEXT;

-- 9. 添加索引优化查询
CREATE INDEX IF NOT EXISTS ix_operation_logs_user_id ON operation_logs(user_id);
CREATE INDEX IF NOT EXISTS ix_operation_logs_created_at ON operation_logs(created_at);
CREATE INDEX IF NOT EXISTS ix_operation_logs_resource_type ON operation_logs(resource_type);
