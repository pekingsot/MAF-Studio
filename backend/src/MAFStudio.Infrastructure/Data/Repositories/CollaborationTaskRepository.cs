using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class CollaborationTaskRepository : ICollaborationTaskRepository
{
    private readonly IDapperContext _context;

    public CollaborationTaskRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<CollaborationTask?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM collaboration_tasks WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<CollaborationTask>(sql, new { Id = id });
    }

    public async Task<List<CollaborationTask>> GetByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM collaboration_tasks WHERE collaboration_id = @CollaborationId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<CollaborationTask>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }

    public async Task<CollaborationTask> CreateAsync(CollaborationTask task)
    {
        using var connection = _context.CreateConnection();
        task.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO collaboration_tasks (collaboration_id, title, description, prompt, status, assigned_to, created_at, completed_at, git_url, git_branch, git_credentials, config)
            VALUES (@CollaborationId, @Title, @Description, @Prompt, @Status, @AssignedTo, @CreatedAt, @CompletedAt, @GitUrl, @GitBranch, @GitCredentials, @Config)
            RETURNING *";
        return await connection.QueryFirstAsync<CollaborationTask>(sql, task);
    }

    public async Task<CollaborationTask> UpdateAsync(CollaborationTask task)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE collaboration_tasks SET 
                title = @Title,
                description = @Description,
                prompt = @Prompt,
                status = @Status,
                assigned_to = @AssignedTo,
                completed_at = @CompletedAt,
                git_url = @GitUrl,
                git_branch = @GitBranch,
                git_credentials = @GitCredentials,
                config = @Config
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<CollaborationTask>(sql, task);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM collaboration_tasks WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> UpdateStatusAsync(long id, CollaborationTaskStatus status)
    {
        using var connection = _context.CreateConnection();
        const string sql = "UPDATE collaboration_tasks SET status = @Status WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
        return rows > 0;
    }
}
