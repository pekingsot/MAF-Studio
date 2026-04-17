INSERT INTO system_configs (key, value, description, created_at) VALUES
('embedding_endpoint', 'http://localhost:7997', '向量化接口地址 (Infinity Embedding)', CURRENT_TIMESTAMP),
('embedding_model', 'BAAI/bge-m3', '向量化模型名称', CURRENT_TIMESTAMP),
('rerank_endpoint', 'http://localhost:7997', '重排序接口地址 (Infinity Rerank)', CURRENT_TIMESTAMP),
('rerank_model', 'BAAI/bge-reranker-v2-m3', '重排序模型名称', CURRENT_TIMESTAMP),
('vector_db_endpoint', 'http://localhost:6333', '向量库接口地址 (Qdrant)', CURRENT_TIMESTAMP),
('vector_db_collection', 'maf_documents', '向量库集合名称', CURRENT_TIMESTAMP),
('default_split_method', 'recursive', '默认分割方式 (recursive/character/separator)', CURRENT_TIMESTAMP),
('default_chunk_size', '500', '默认分块大小', CURRENT_TIMESTAMP),
('default_chunk_overlap', '50', '默认分块重叠', CURRENT_TIMESTAMP),
('skip_extensions', '.xml,.json,.yml,.yaml,.toml,.ini,.conf,.env,.sh,.bat,.ps1', '跳过分割的文件扩展名', CURRENT_TIMESTAMP),
('vectorization_endpoint', 'http://localhost:7997', '向量化接口地址(兼容)', CURRENT_TIMESTAMP),
('rerank_endpoint_alt', 'http://localhost:7997', '重排序接口地址(兼容)', CURRENT_TIMESTAMP)
ON CONFLICT (key) DO NOTHING;
