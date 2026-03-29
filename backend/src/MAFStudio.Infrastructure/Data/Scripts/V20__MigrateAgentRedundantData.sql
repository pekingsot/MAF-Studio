-- 迁移现有智能体数据，填充冗余字段
-- 从关联表中查询名称并更新到冗余字段

-- 1. 更新智能体类型名称
UPDATE agents a
SET type_name = at.name
FROM agent_types at
WHERE a.type = at.code
  AND a.type_name IS NULL;

-- 2. 更新主模型配置名称
UPDATE agents a
SET llm_config_name = lc.name
FROM llm_configs lc
WHERE a.llm_config_id = lc.id
  AND a.llm_config_name IS NULL;

-- 3. 更新主模型名称
UPDATE agents a
SET llm_model_name = COALESCE(lmc.display_name, lmc.model_name)
FROM llm_model_configs lmc
WHERE a.llm_model_config_id = lmc.id
  AND a.llm_model_name IS NULL;

-- 4. 更新副模型详细信息（需要从JSON中提取并查询名称）
-- 这个比较复杂，需要在应用层处理或使用存储过程
-- 暂时留空，新创建的智能体会自动填充
