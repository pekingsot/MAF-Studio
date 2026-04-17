CREATE TABLE IF NOT EXISTS agent_skills (
    id BIGSERIAL PRIMARY KEY,
    agent_id BIGINT NOT NULL,
    skill_name VARCHAR(200) NOT NULL,
    skill_content TEXT NOT NULL,
    enabled BOOLEAN DEFAULT TRUE,
    priority INT DEFAULT 0,
    runtime VARCHAR(50) DEFAULT 'python',
    entry_point VARCHAR(500),
    allowed_tools TEXT,
    permissions TEXT,
    parameters TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(agent_id, skill_name)
);

COMMENT ON TABLE agent_skills IS 'Agent技能配置表，存储SKILL.md格式的技能定义';
COMMENT ON COLUMN agent_skills.agent_id IS '关联的Agent ID';
COMMENT ON COLUMN agent_skills.skill_name IS '技能唯一标识，kebab-case格式，如code-review';
COMMENT ON COLUMN agent_skills.skill_content IS 'SKILL.md完整内容（YAML frontmatter + Markdown指令）';
COMMENT ON COLUMN agent_skills.enabled IS '是否启用';
COMMENT ON COLUMN agent_skills.priority IS '优先级，数字越大越优先';
COMMENT ON COLUMN agent_skills.runtime IS '脚本运行时：python/node/bash';
COMMENT ON COLUMN agent_skills.entry_point IS '脚本入口文件路径，如scripts/main.py';
COMMENT ON COLUMN agent_skills.allowed_tools IS '允许使用的工具列表，JSON数组格式，如["ReadFile","WriteFile"]';
COMMENT ON COLUMN agent_skills.permissions IS '权限声明，JSON格式，如{"network":true,"filesystem":true}';
COMMENT ON COLUMN agent_skills.parameters IS '技能参数定义，JSON格式';

CREATE INDEX IF NOT EXISTS ix_agent_skills_agent_id ON agent_skills(agent_id);
CREATE INDEX IF NOT EXISTS ix_agent_skills_enabled ON agent_skills(enabled);

CREATE TABLE IF NOT EXISTS skill_templates (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    content TEXT NOT NULL,
    category VARCHAR(100),
    tags TEXT,
    runtime VARCHAR(50) DEFAULT 'python',
    usage_count INT DEFAULT 0,
    is_official BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

COMMENT ON TABLE skill_templates IS '技能模板库，预置常用技能模板';
COMMENT ON COLUMN skill_templates.content IS 'SKILL.md模板内容';
COMMENT ON COLUMN skill_templates.category IS '分类：development/testing/design/analysis/automation';
COMMENT ON COLUMN skill_templates.tags IS '标签，逗号分隔';
COMMENT ON COLUMN skill_templates.is_official IS '是否为官方预置模板';

CREATE INDEX IF NOT EXISTS ix_skill_templates_category ON skill_templates(category);

INSERT INTO skill_templates (name, description, content, category, tags, runtime, is_official) VALUES
(
    'code-review',
    '代码审查专家，擅长发现代码中的潜在问题和改进空间',
    '---
name: code-review
description: "Performs thorough code review, identifies potential bugs, security vulnerabilities, performance issues and improvement opportunities. Use when asked to review, audit or analyze source code."
---

# Code Review Expert

## When to use this skill
- User submits code for review
- User asks for code quality analysis
- User requests security audit of source code
- User wants to find bugs or performance issues

## How to use this skill

1. Use ReadFile to read the target source code file
2. Analyze code structure, naming conventions, error handling
3. Check for common issues:
   - Security vulnerabilities (SQL injection, XSS, CSRF)
   - Performance bottlenecks
   - Code smells and anti-patterns
   - Missing error handling
   - Violation of SOLID principles
4. Use WriteFile to output review report if needed

## Output Format
- Use Chinese for output
- Mark severity for each issue (🔴 Critical / 🟡 Warning / 🟢 Suggestion)
- Provide specific fix suggestions with code examples

## Constraints
- Do not modify files without user authorization
- Mark uncertain issues as "需人工确认"
- Review one file at a time to avoid information overload

## Keywords
code review, code audit, code quality, security check, bug detection',
    'development',
    'code-review,security,quality',
    'python',
    TRUE
),
(
    'api-doc-generator',
    'API文档生成器，自动分析代码并生成API文档',
    '---
name: api-doc-generator
description: "Analyzes source code to generate comprehensive API documentation. Use when asked to create, update or generate API docs, endpoint documentation, or interface specifications."
---

# API Documentation Generator

## When to use this skill
- User asks to generate API documentation
- User wants to document endpoints or interfaces
- User needs to create interface specifications
- User requests OpenAPI/Swagger documentation

## How to use this skill

1. Use SearchInCode to find API endpoint definitions
2. Use ReadFile to read each endpoint implementation
3. Extract: HTTP method, path, parameters, request/response schema
4. Use FindDefinitions to locate related models/DTOs
5. Generate documentation in requested format (Markdown, OpenAPI, etc.)

## Output Format
- Default: Markdown with tables
- Optional: OpenAPI 3.0 YAML format
- Include request/response examples

## Keywords
api docs, documentation, swagger, openapi, endpoint, interface spec',
    'development',
    'documentation,api,openapi',
    'python',
    TRUE
),
(
    'test-writer',
    '测试用例编写专家，自动分析代码并生成单元测试',
    '---
name: test-writer
description: "Analyzes source code and generates comprehensive unit tests. Use when asked to write tests, create test cases, improve test coverage, or add unit/integration tests."
---

# Test Writer Expert

## When to use this skill
- User asks to write unit tests
- User wants to improve test coverage
- User needs integration tests
- User requests test case design

## How to use this skill

1. Use ReadFile to read the target source code
2. AnalyzeCode to understand structure (classes, methods, branches)
3. Identify test scenarios:
   - Happy path scenarios
   - Edge cases and boundary conditions
   - Error handling paths
   - Null/empty input cases
4. Generate tests following the project testing framework
5. Use WriteFile to save test files

## Output Format
- Follow existing test patterns in the project
- Use descriptive test method names
- Include Arrange-Act-Assert pattern
- Add comments explaining test intent

## Keywords
unit test, test case, test coverage, integration test, tdd',
    'testing',
    'testing,unit-test,coverage',
    'python',
    TRUE
),
(
    'data-analyst',
    '数据分析专家，擅长数据清洗、统计分析和可视化建议',
    '---
name: data-analyst
description: "Performs data analysis including data cleaning, statistical analysis, and visualization recommendations. Use when asked to analyze data, create reports, find trends, or generate insights from datasets."
---

# Data Analysis Expert

## When to use this skill
- User provides data for analysis
- User asks for statistical analysis
- User wants data visualization recommendations
- User needs data cleaning or transformation

## How to use this skill

1. Use ReadFile to load data files (CSV, JSON, Excel)
2. Analyze data structure and quality
3. Perform statistical analysis
4. Identify patterns, trends, and anomalies
5. Generate analysis report with visualization suggestions

## Keywords
data analysis, statistics, visualization, data cleaning, trend analysis',
    'analysis',
    'data,analysis,statistics,visualization',
    'python',
    TRUE
);
