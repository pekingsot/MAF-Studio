using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly IDapperContext _context;

    public SystemConfigRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<SystemConfig?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM system_configs WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<SystemConfig>(sql, new { Id = id });
    }

    public async Task<SystemConfig?> GetByKeyAsync(string key)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM system_configs WHERE key = @Key";
        return await connection.QueryFirstOrDefaultAsync<SystemConfig>(sql, new { Key = key });
    }

    public async Task<List<SystemConfig>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM system_configs ORDER BY key";
        var result = await connection.QueryAsync<SystemConfig>(sql);
        return result.ToList();
    }

    public async Task<SystemConfig> CreateAsync(SystemConfig config)
    {
        using var connection = _context.CreateConnection();
        config.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO system_configs (key, value, description, created_at, updated_at)
            VALUES (@Key, @Value, @Description, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<SystemConfig>(sql, config);
    }

    public async Task<SystemConfig> UpdateAsync(SystemConfig config)
    {
        using var connection = _context.CreateConnection();
        config.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE system_configs SET value = @Value, description = @Description, updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<SystemConfig>(sql, config);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM system_configs WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}

public class RagDocumentRepository : IRagDocumentRepository
{
    private readonly IDapperContext _context;

    public RagDocumentRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<RagDocument?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM rag_documents WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<RagDocument>(sql, new { Id = id });
    }

    public async Task<List<RagDocument>> GetByUserIdAsync(string userId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM rag_documents WHERE user_id = @UserId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<RagDocument>(sql, new { UserId = long.Parse(userId) });
        return result.ToList();
    }

    public async Task<RagDocument> CreateAsync(RagDocument document)
    {
        using var connection = _context.CreateConnection();
        document.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO rag_documents (file_name, file_path, file_type, file_size, status, error_message, user_id, created_at, processed_at)
            VALUES (@FileName, @FilePath, @FileType, @FileSize, @Status, @ErrorMessage, @UserId, @CreatedAt, @ProcessedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<RagDocument>(sql, document);
    }

    public async Task<RagDocument> UpdateAsync(RagDocument document)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE rag_documents SET 
                file_name = @FileName, file_path = @FilePath, file_type = @FileType,
                file_size = @FileSize, status = @Status, error_message = @ErrorMessage,
                processed_at = @ProcessedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<RagDocument>(sql, document);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM rag_documents WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}

public class RagDocumentChunkRepository : IRagDocumentChunkRepository
{
    private readonly IDapperContext _context;

    public RagDocumentChunkRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<RagDocumentChunk?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM rag_document_chunks WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<RagDocumentChunk>(sql, new { Id = id });
    }

    public async Task<List<RagDocumentChunk>> GetByDocumentIdAsync(long documentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM rag_document_chunks WHERE document_id = @DocumentId ORDER BY chunk_index";
        var result = await connection.QueryAsync<RagDocumentChunk>(sql, new { DocumentId = documentId });
        return result.ToList();
    }

    public async Task<RagDocumentChunk> CreateAsync(RagDocumentChunk chunk)
    {
        using var connection = _context.CreateConnection();
        chunk.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO rag_document_chunks (document_id, chunk_index, content, metadata, created_at)
            VALUES (@DocumentId, @ChunkIndex, @Content, @Metadata, @CreatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<RagDocumentChunk>(sql, chunk);
    }

    public async Task<List<RagDocumentChunk>> CreateBatchAsync(List<RagDocumentChunk> chunks)
    {
        using var connection = _context.CreateConnection();
        var results = new List<RagDocumentChunk>();
        const string sql = @"
            INSERT INTO rag_document_chunks (document_id, chunk_index, content, metadata, created_at)
            VALUES (@DocumentId, @ChunkIndex, @Content, @Metadata, @CreatedAt)
            RETURNING *";
        foreach (var chunk in chunks)
        {
            chunk.CreatedAt = DateTime.UtcNow;
            var result = await connection.QueryFirstAsync<RagDocumentChunk>(sql, chunk);
            results.Add(result);
        }
        return results;
    }

    public async Task<bool> DeleteByDocumentIdAsync(long documentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM rag_document_chunks WHERE document_id = @DocumentId";
        var rows = await connection.ExecuteAsync(sql, new { DocumentId = documentId });
        return rows > 0;
    }
}
