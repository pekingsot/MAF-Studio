-- MAF Studio 基础数据初始化脚本

-- 删除已存在的用户数据
DELETE FROM users WHERE id IN ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002');

-- 插入默认管理员用户 (密码: admin123)
INSERT INTO users (id, username, email, password_hash, role, avatar, created_at, updated_at) VALUES
('00000000-0000-0000-0000-000000000001', 'admin', 'admin@mafstudio.com', '$2a$12$X.IYxXx7hl6R26euoO8WxuhA6v4ppkJEj7UGV1.88uGJJZ5oSxD.C', 'admin', '👤', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('00000000-0000-0000-0000-000000000002', 'pekingsot', 'pekingsot@example.com', '$2a$12$0ttKwEtrMO7hbqFcUoSGVubZLdQKIabVZBi5Y1Ubg5oiXqdb9E1fy', 'user', '👤', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- 插入默认智能体类型
INSERT INTO agent_types (id, name, code, description, icon, default_configuration, created_at) VALUES
('10000000-0000-0000-0000-000000000001', '助手', 'Assistant', '通用智能助手，可以回答问题、提供建议和帮助完成各种任务', '🤖', '{"systemPrompt": "你是一个有帮助的AI助手。", "temperature": 0.7, "maxTokens": 2000}', CURRENT_TIMESTAMP),
('10000000-0000-0000-0000-000000000002', '代码专家', 'CodeExpert', '专注于编程和软件开发，可以帮助编写、调试和优化代码', '💻', '{"systemPrompt": "你是一个专业的程序员，擅长各种编程语言和开发框架。", "temperature": 0.5, "maxTokens": 4000}', CURRENT_TIMESTAMP),
('10000000-0000-0000-0000-000000000003', '数据分析师', 'DataAnalyst', '擅长数据分析和可视化，可以帮助处理和分析数据', '📊', '{"systemPrompt": "你是一个数据分析专家，擅长统计学、数据可视化和数据挖掘。", "temperature": 0.6, "maxTokens": 3000}', CURRENT_TIMESTAMP),
('10000000-0000-0000-0000-000000000004', '创意写手', 'CreativeWriter', '擅长创意写作，可以撰写文章、故事、文案等', '✍️', '{"systemPrompt": "你是一个创意写作专家，擅长各种文体和风格。", "temperature": 0.8, "maxTokens": 4000}', CURRENT_TIMESTAMP),
('10000000-0000-0000-0000-000000000005', '项目经理', 'ProjectManager', '帮助规划和管理项目，协调团队工作', '📋', '{"systemPrompt": "你是一个经验丰富的项目经理，擅长项目规划、风险管理和团队协调。", "temperature": 0.6, "maxTokens": 2000}', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 插入默认大模型配置
INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, user_id, created_at) VALUES
('20000000-0000-0000-0000-000000000001', 'OpenAI 默认配置', 'OpenAI', '', 'https://api.openai.com/v1', 'gpt-4o', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP),
('20000000-0000-0000-0000-000000000002', '通义千问 默认配置', 'Qwen', '', 'https://dashscope.aliyuncs.com/api/v1', 'qwen-max', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP),
('20000000-0000-0000-0000-000000000003', '智谱AI 默认配置', 'Zhipu', '', 'https://open.bigmodel.cn/api/paas/v4', 'glm-4', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 插入默认大模型子配置
INSERT INTO llm_model_configs (id, llm_config_id, model_name, display_name, description, is_default, created_at) VALUES
-- OpenAI 模型
('30000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'gpt-4o', 'GPT-4o', 'OpenAI最新多模态模型', true, CURRENT_TIMESTAMP),
('30000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 'gpt-4-turbo', 'GPT-4 Turbo', 'GPT-4增强版本', false, CURRENT_TIMESTAMP),
('30000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000001', 'gpt-3.5-turbo', 'GPT-3.5 Turbo', '快速响应模型', false, CURRENT_TIMESTAMP),
-- 通义千问模型
('30000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000002', 'qwen-max', '通义千问-Max', '阿里云最强模型', true, CURRENT_TIMESTAMP),
('30000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000002', 'qwen-plus', '通义千问-Plus', '平衡性能与成本', false, CURRENT_TIMESTAMP),
('30000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000002', 'qwen-turbo', '通义千问-Turbo', '快速响应模型', false, CURRENT_TIMESTAMP),
-- 智谱模型
('30000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000003', 'glm-4', 'GLM-4', '智谱最新模型', true, CURRENT_TIMESTAMP),
('30000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000003', 'glm-4-flash', 'GLM-4-Flash', '快速响应模型', false, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 插入默认智能体
INSERT INTO agents (id, name, description, type, configuration, avatar, user_id, status, llm_config_id, created_at) VALUES
('40000000-0000-0000-0000-000000000001', '小助手', '您的通用AI助手，可以回答各种问题', 'Assistant', '{"systemPrompt": "你是一个友好、有帮助的AI助手。请用简洁清晰的语言回答用户的问题。", "temperature": 0.7, "maxTokens": 2000}', '🤖', '00000000-0000-0000-0000-000000000001', 0, '20000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP),
('40000000-0000-0000-0000-000000000002', '代码专家', '专业的编程助手，帮助您解决代码问题', 'CodeExpert', '{"systemPrompt": "你是一个专业的程序员，擅长各种编程语言。请提供清晰、高效的代码解决方案。", "temperature": 0.5, "maxTokens": 4000}', '💻', '00000000-0000-0000-0000-000000000001', 0, '20000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP),
('40000000-0000-0000-0000-000000000003', '数据分析师', '数据分析专家，帮助您处理和分析数据', 'DataAnalyst', '{"systemPrompt": "你是一个数据分析专家，擅长统计学和数据可视化。请提供专业的数据分析建议。", "temperature": 0.6, "maxTokens": 3000}', '📊', '00000000-0000-0000-0000-000000000001', 0, '20000000-0000-0000-0000-000000000002', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 插入系统配置
INSERT INTO system_configs (id, key, value, description, created_at) VALUES
('50000000-0000-0000-0000-000000000001', 'system.name', 'MAF Studio', '系统名称', CURRENT_TIMESTAMP),
('50000000-0000-0000-0000-000000000002', 'system.version', '1.0.0', '系统版本', CURRENT_TIMESTAMP),
('50000000-0000-0000-0000-000000000003', 'system.description', '多智能体协作平台', '系统描述', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;
