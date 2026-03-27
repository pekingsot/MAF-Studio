-- V4__SeedDefaultData.sql
-- 初始化系统默认数据：用户、LLM供应商、智能体类型
-- 这些数据是系统级别的，不属于特定用户

-- =============================================
-- 0. 默认用户（必须在其他数据之前）
-- =============================================

-- 系统用户（用于系统默认数据）
INSERT INTO users (id, username, email, password_hash, role, avatar, created_at, updated_at) VALUES
('00000000-0000-0000-0000-000000000003', 'system', 'system@mafstudio.com', '', 'system', '⚙️', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (username) DO NOTHING;

-- 默认管理员用户 (密码: admin123)
INSERT INTO users (id, username, email, password_hash, role, avatar, created_at, updated_at) VALUES
('00000000-0000-0000-0000-000000000001', 'admin', 'admin@mafstudio.com', '$2a$12$X.IYxXx7hl6R26euoO8WxuhA6v4ppkJEj7UGV1.88uGJJZ5oSxD.C', 'admin', '👤', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (username) DO UPDATE SET 
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    role = EXCLUDED.role,
    updated_at = CURRENT_TIMESTAMP;

-- 测试用户
INSERT INTO users (id, username, email, password_hash, role, avatar, created_at, updated_at) VALUES
('00000000-0000-0000-0000-000000000002', 'pekingsot', 'pekingsot@example.com', '$2a$12$0ttKwEtrMO7hbqFcUoSGVubZLdQKIabVZBi5Y1Ubg5oiXqdb9E1fy', 'user', '👤', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT (username) DO UPDATE SET 
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    role = EXCLUDED.role,
    updated_at = CURRENT_TIMESTAMP;

-- =============================================
-- 1. LLM供应商配置（系统默认）
-- =============================================

-- 阿里千问供应商（系统默认，使用system用户ID）
INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, extra_config, user_id, created_at) VALUES
(1000000000000001, '阿里千问', 'Qwen', '', 'https://dashscope.aliyuncs.com/compatible-mode/v1', 'qwen-max', 
 '{"models": ["qwen-max", "qwen-plus", "qwen-turbo", "qwen-long"], "supports_streaming": true, "supports_vision": true}', 
 '00000000-0000-0000-0000-000000000003', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 智谱AI供应商
INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, extra_config, user_id, created_at) VALUES
(1000000000000002, '智谱AI', 'Zhipu', '', 'https://open.bigmodel.cn/api/paas/v4', 'glm-4', 
 '{"models": ["glm-4", "glm-4-flash", "glm-4-plus", "glm-4-air"], "supports_streaming": true, "supports_vision": true}', 
 '00000000-0000-0000-0000-000000000003', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 兼容OpenAI标准供应商（可配置任意兼容OpenAI API的服务）
INSERT INTO llm_configs (id, name, provider, api_key, endpoint, default_model, extra_config, user_id, created_at) VALUES
(1000000000000003, 'OpenAI兼容', 'OpenAI', '', 'https://api.openai.com/v1', 'gpt-4o', 
 '{"models": ["gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo"], "supports_streaming": true, "supports_vision": true, "compatible_mode": true}', 
 '00000000-0000-0000-0000-000000000003', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- =============================================
-- 2. LLM模型子配置（系统默认）
-- =============================================

-- 阿里千问模型
INSERT INTO llm_model_configs (id, llm_config_id, model_name, display_name, description, is_default, created_at) VALUES
(2000000000000001, 1000000000000001, 'qwen-max', '通义千问-Max', '阿里云最强模型，适合复杂任务', true, CURRENT_TIMESTAMP),
(2000000000000002, 1000000000000001, 'qwen-plus', '通义千问-Plus', '平衡性能与成本，适合日常任务', false, CURRENT_TIMESTAMP),
(2000000000000003, 1000000000000001, 'qwen-turbo', '通义千问-Turbo', '快速响应模型，适合简单任务', false, CURRENT_TIMESTAMP),
(2000000000000004, 1000000000000001, 'qwen-long', '通义千问-Long', '超长上下文模型，支持百万字', false, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- 智谱AI模型
INSERT INTO llm_model_configs (id, llm_config_id, model_name, display_name, description, is_default, created_at) VALUES
(2000000000000011, 1000000000000002, 'glm-4', 'GLM-4', '智谱最新旗舰模型，能力全面', true, CURRENT_TIMESTAMP),
(2000000000000012, 1000000000000002, 'glm-4-flash', 'GLM-4-Flash', '快速响应模型，性价比高', false, CURRENT_TIMESTAMP),
(2000000000000013, 1000000000000002, 'glm-4-plus', 'GLM-4-Plus', '增强版模型，推理能力更强', false, CURRENT_TIMESTAMP),
(2000000000000014, 1000000000000002, 'glm-4-air', 'GLM-4-Air', '轻量级模型，成本更低', false, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- OpenAI兼容模型
INSERT INTO llm_model_configs (id, llm_config_id, model_name, display_name, description, is_default, created_at) VALUES
(2000000000000021, 1000000000000003, 'gpt-4o', 'GPT-4o', 'OpenAI最新多模态模型', true, CURRENT_TIMESTAMP),
(2000000000000022, 1000000000000003, 'gpt-4-turbo', 'GPT-4 Turbo', 'GPT-4增强版本', false, CURRENT_TIMESTAMP),
(2000000000000023, 1000000000000003, 'gpt-3.5-turbo', 'GPT-3.5 Turbo', '快速响应模型', false, CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- =============================================
-- 3. 智能体类型（互联网公司职位）
-- =============================================

INSERT INTO agent_types (id, name, code, description, icon, default_configuration, llm_config_id, created_at) VALUES

-- 技术研发类
(3000000000000001, '产品经理', 'ProductManager', '负责产品规划、需求分析和用户体验设计，擅长将业务需求转化为产品方案', 
 '📋', '{"systemPrompt": "你是一位经验丰富的产品经理。你擅长需求分析、用户研究、产品规划和项目管理。你会用清晰的逻辑和结构化的方式思考问题，善于平衡用户需求、技术可行性和商业价值。", "temperature": 0.7, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000002, '前端工程师', 'FrontendEngineer', '专注于Web前端开发，精通React、Vue等框架，负责用户界面和交互体验', 
 '🎨', '{"systemPrompt": "你是一位专业的前端工程师。你精通HTML、CSS、JavaScript，熟练使用React、Vue、Angular等主流框架。你关注用户体验、性能优化和代码质量，遵循最佳实践和设计模式。", "temperature": 0.5, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000003, '后端工程师', 'BackendEngineer', '专注于服务端开发，精通Java、Python、Go等语言，负责系统架构和API设计', 
 '⚙️', '{"systemPrompt": "你是一位专业的后端工程师。你精通Java、Python、Go、C#等后端语言，熟悉微服务架构、数据库设计、API开发和性能优化。你注重代码质量、安全性和可维护性。", "temperature": 0.5, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000004, '全栈工程师', 'FullStackEngineer', '具备前后端全栈能力，能够独立完成完整的Web应用开发', 
 '🔧', '{"systemPrompt": "你是一位全栈工程师。你同时精通前端和后端开发，能够独立完成从需求分析到部署上线的全流程。你熟悉多种技术栈，善于技术选型和架构设计。", "temperature": 0.6, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000005, '测试工程师', 'QAEngineer', '负责软件测试和质量保证，精通自动化测试、性能测试和安全测试', 
 '🧪', '{"systemPrompt": "你是一位专业的测试工程师。你精通各种测试方法论，包括单元测试、集成测试、端到端测试、性能测试和安全测试。你善于发现潜在问题，确保产品质量。", "temperature": 0.5, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000006, '运维工程师', 'DevOpsEngineer', '负责系统运维和DevOps，精通Docker、K8s、CI/CD等技术和工具', 
 '🚀', '{"systemPrompt": "你是一位专业的运维工程师。你精通Linux系统管理、Docker容器化、Kubernetes编排、CI/CD流水线、监控告警和故障排查。你追求高可用、自动化和效率优化。", "temperature": 0.5, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000007, '数据库工程师', 'DBA', '负责数据库设计、优化和维护，精通MySQL、PostgreSQL、MongoDB等数据库', 
 '🗄️', '{"systemPrompt": "你是一位专业的数据库工程师。你精通关系型数据库（MySQL、PostgreSQL、Oracle）和NoSQL数据库（MongoDB、Redis），擅长数据库设计、SQL优化、性能调优和数据迁移。", "temperature": 0.5, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

-- 设计类
(3000000000000011, 'UI设计师', 'UIDesigner', '负责用户界面设计，精通视觉设计和交互设计，打造优秀的用户体验', 
 '🎨', '{"systemPrompt": "你是一位专业的UI设计师。你精通视觉设计、交互设计和用户体验设计，熟练使用Figma、Sketch等设计工具。你关注设计趋势，善于创造美观且易用的界面。", "temperature": 0.7, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000012, 'UX设计师', 'UXDesigner', '负责用户体验设计，通过用户研究和原型设计优化产品体验', 
 '💡', '{"systemPrompt": "你是一位专业的UX设计师。你擅长用户研究、信息架构、交互设计和原型制作。你以用户为中心思考问题，通过数据驱动设计决策。", "temperature": 0.7, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

-- 数据类
(3000000000000021, '数据分析师', 'DataAnalyst', '负责数据分析和可视化，通过数据洞察支持业务决策', 
 '📊', '{"systemPrompt": "你是一位专业的数据分析师。你精通统计学、数据可视化和数据分析工具（Python、SQL、Excel、Tableau）。你善于从数据中发现规律，提供有价值的业务洞察。", "temperature": 0.6, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000022, '数据科学家', 'DataScientist', '负责机器学习和数据挖掘，构建预测模型和推荐系统', 
 '🤖', '{"systemPrompt": "你是一位专业的数据科学家。你精通机器学习、深度学习和统计分析，熟练使用Python、TensorFlow、PyTorch等工具。你善于构建预测模型、推荐系统和自然语言处理应用。", "temperature": 0.5, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000023, '算法工程师', 'AlgorithmEngineer', '负责算法设计和优化，精通搜索、推荐、NLP等领域的算法实现', 
 '🧮', '{"systemPrompt": "你是一位专业的算法工程师。你精通数据结构与算法，熟悉搜索、推荐、广告、NLP等领域的算法。你善于解决复杂的算法问题，追求最优解。", "temperature": 0.5, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

-- 管理类
(3000000000000031, '项目经理', 'ProjectManager', '负责项目规划、进度管理和团队协调，确保项目按时交付', 
 '📅', '{"systemPrompt": "你是一位经验丰富的项目经理。你擅长项目规划、风险管理、团队协调和沟通。你熟悉敏捷开发方法论，善于推动项目进展，解决团队协作中的问题。", "temperature": 0.7, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000032, '技术主管', 'TechLead', '负责技术决策和团队技术指导，把控技术方向和代码质量', 
 '👨‍💻', '{"systemPrompt": "你是一位技术主管。你有丰富的技术背景和团队管理经验，擅长技术选型、架构设计和代码评审。你能够平衡技术追求与业务需求，带领团队成长。", "temperature": 0.6, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000033, '架构师', 'Architect', '负责系统架构设计，制定技术规范，解决复杂技术问题', 
 '🏛️', '{"systemPrompt": "你是一位资深的系统架构师。你精通分布式系统、微服务架构、高可用设计和技术选型。你能够从全局视角设计系统架构，平衡性能、可扩展性、安全性和成本。", "temperature": 0.5, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000034, 'CTO', 'CTO', '技术总监级别，负责技术战略规划和技术团队管理', 
 '👔', '{"systemPrompt": "你是一位CTO级别的技术领导者。你具有战略思维，能够将技术与业务目标结合。你擅长技术战略规划、团队建设、技术文化塑造和技术创新推动。", "temperature": 0.7, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

-- 运营类
(3000000000000041, '运营专员', 'OperationsSpecialist', '负责产品运营和用户运营，提升用户活跃和留存', 
 '📈', '{"systemPrompt": "你是一位专业的运营专员。你熟悉用户运营、活动运营和内容运营，善于通过数据分析优化运营策略，提升用户活跃度和留存率。", "temperature": 0.7, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

(3000000000000042, '内容运营', 'ContentOperator', '负责内容策划和内容营销，打造优质内容生态', 
 '✍️', '{"systemPrompt": "你是一位专业的内容运营。你擅长内容策划、文案撰写和内容营销，熟悉各平台的内容分发机制，能够创造有传播力的优质内容。", "temperature": 0.8, "maxTokens": 4000}', 
 NULL, CURRENT_TIMESTAMP),

-- 市场类
(3000000000000051, '市场专员', 'MarketingSpecialist', '负责市场推广和品牌建设，制定营销策略', 
 '📢', '{"systemPrompt": "你是一位专业的市场专员。你熟悉市场调研、品牌营销和数字营销，善于制定营销策略，提升品牌知名度和市场占有率。", "temperature": 0.7, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP),

-- 安全类
(3000000000000061, '安全工程师', 'SecurityEngineer', '负责系统安全和渗透测试，保障信息安全', 
 '🔒', '{"systemPrompt": "你是一位专业的安全工程师。你精通网络安全、渗透测试、漏洞挖掘和安全加固。你能够识别安全风险，制定安全策略，保护系统和数据安全。", "temperature": 0.5, "maxTokens": 3000}', 
 NULL, CURRENT_TIMESTAMP)

ON CONFLICT (id) DO NOTHING;

-- =============================================
-- 4. 系统配置（更新）
-- =============================================

INSERT INTO system_configs (id, key, value, description, created_at) VALUES
(4000000000000001, 'system.default_llm_config_id', '1000000000000001', '默认LLM配置ID（阿里千问）', CURRENT_TIMESTAMP),
(4000000000000002, 'system.default_agent_type_id', '3000000000000001', '默认智能体类型ID（产品经理）', CURRENT_TIMESTAMP),
(4000000000000003, 'llm.providers', '["Qwen", "Zhipu", "OpenAI"]', '支持的LLM供应商列表', CURRENT_TIMESTAMP)
ON CONFLICT (id) DO NOTHING;

-- =============================================
-- 完成
-- =============================================
