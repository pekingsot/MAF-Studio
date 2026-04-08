using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;
using Npgsql;

namespace MAFStudio.Infrastructure.Repositories;

public class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly IDapperContext _context;

    public WorkflowExecutionRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<WorkflowExecution> CreateAsync(WorkflowExecution execution)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            INSERT INTO workflow_executions 
                (id, collaboration_id, task_id, workflow_type, input, status, started_at, completed_at, error_message)
            VALUES 
                (@Id, @CollaborationId, @TaskId, @WorkflowType, @Input, @Status, @StartedAt, @CompletedAt, @ErrorMessage)";

        await connection.ExecuteAsync(sql, execution);
        return execution;
    }

    public async Task<WorkflowExecution?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM workflow_executions WHERE id = @Id";
        return await connection.QuerySingleOrDefaultAsync<WorkflowExecution>(sql, new { Id = id });
    }

    public async Task<List<WorkflowExecution>> GetByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM workflow_executions WHERE collaboration_id = @CollaborationId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<WorkflowExecution>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }

    public async Task<List<WorkflowExecution>> GetByStatusAsync(string status)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM workflow_executions WHERE status = @Status ORDER BY created_at DESC";
        var result = await connection.QueryAsync<WorkflowExecution>(sql, new { Status = status });
        return result.ToList();
    }

    public async Task UpdateAsync(WorkflowExecution execution)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            UPDATE workflow_executions 
            SET status = @Status, 
                started_at = @StartedAt, 
                completed_at = @CompletedAt, 
                error_message = @ErrorMessage,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, execution);
    }

    public async Task AddMessageAsync(WorkflowExecutionMessage message)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            INSERT INTO workflow_execution_messages 
                (id, execution_id, sender, content, role, timestamp)
            VALUES 
                (@Id, @ExecutionId, @Sender, @Content, @Role, @Timestamp)";

        await connection.ExecuteAsync(sql, message);
    }

    public async Task<List<WorkflowExecutionMessage>> GetMessagesAsync(long executionId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM workflow_execution_messages WHERE execution_id = @ExecutionId ORDER BY timestamp ASC";
        var result = await connection.QueryAsync<WorkflowExecutionMessage>(sql, new { ExecutionId = executionId });
        return result.ToList();
    }
}
