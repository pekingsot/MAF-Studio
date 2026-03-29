-- V24: 将被清零表的自增ID序列设置为从1000开始

-- agents 表
SELECT setval('agents_id_seq', 1000, false);

-- agent_messages 表
SELECT setval('agent_messages_id_seq', 1000, false);

-- agent_models 表
SELECT setval('agent_models_id_seq', 1000, false);

-- collaborations 表
SELECT setval('collaborations_id_seq', 1000, false);

-- collaboration_agents 表
SELECT setval('collaboration_agents_id_seq', 1000, false);

-- collaboration_tasks 表
SELECT setval('collaboration_tasks_id_seq', 1000, false);

-- llm_configs 表
SELECT setval('llm_configs_id_seq', 1000, false);

-- llm_model_configs 表
SELECT setval('llm_model_configs_id_seq', 1000, false);

-- llm_test_records 表
SELECT setval('llm_test_records_id_seq', 1000, false);

-- rag_documents 表
SELECT setval('rag_documents_id_seq', 1000, false);

-- system_logs 表
SELECT setval('system_logs_id_seq', 1000, false);

-- operation_logs 表
SELECT setval('operation_logs_id_seq', 1000, false);

-- system_configs 表
SELECT setval('system_configs_id_seq', 1000, false);
