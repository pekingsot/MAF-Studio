-- 再次迁移现有智能体数据，填充冗余字段
-- 确保所有现有记录都有冗余数据

-- 1. 更新智能体类型名称
UPDATE agents a
SET type_name = at.name
FROM agent_types at
WHERE a.type = at.code
  AND (a.type_name IS NULL OR a.type_name = '');

-- 2. 更新主模型配置名称
UPDATE agents a
SET llm_config_name = lc.name
FROM llm_configs lc
WHERE a.llm_config_id = lc.id
  AND (a.llm_config_name IS NULL OR a.llm_config_name = '');

-- 3. 更新主模型名称
UPDATE agents a
SET llm_model_name = COALESCE(lmc.display_name, lmc.model_name)
FROM llm_model_configs lmc
WHERE a.llm_model_config_id = lmc.id
  AND (a.llm_model_name IS NULL OR a.llm_model_name = '');
