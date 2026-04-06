using Dapper;
using Dapper.Contrib.Extensions;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;

namespace MAFStudio.Infrastructure.Repositories;

public class TaskAgentRepository : ITaskAgentRepository
{
    private readonly IDapperContext _context;

    public TaskAgentRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<TaskAgent?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT id, task_id AS taskid, agent_id AS agentid, role, created_at AS createdat FROM task_agents WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<TaskAgent>(sql, new { Id = id });
    }

    public async Task<List<TaskAgent>> GetByTaskIdAsync(long taskId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT id, task_id AS taskid, agent_id AS agentid, role, created_at AS createdat FROM task_agents WHERE task_id = @TaskId ORDER BY created_at";
        var result = await connection.QueryAsync<TaskAgent>(sql, new { TaskId = taskId });
        return result.ToList();
    }

    public async Task<List<TaskAgent>> GetByAgentIdAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT id, task_id AS taskid, agent_id AS agentid, role, created_at AS createdat FROM task_agents WHERE agent_id = @AgentId ORDER BY created_at";
        var result = await connection.QueryAsync<TaskAgent>(sql, new { AgentId = agentId });
        return result.ToList();
    }

    public async Task<TaskAgent> CreateAsync(TaskAgent taskAgent)
    {
        using var connection = _context.CreateConnection();
        var sql = @"INSERT INTO task_agents (task_id, agent_id, role, created_at) 
                    VALUES (@TaskId, @AgentId, @Role, @CreatedAt) 
                    RETURNING id";
        var id = await connection.ExecuteScalarAsync<long>(sql, taskAgent);
        taskAgent.Id = id;
        return taskAgent;
    }

    public async Task<List<TaskAgent>> CreateBatchAsync(List<TaskAgent> taskAgents)
    {
        using var connection = _context.CreateConnection();
        foreach (var taskAgent in taskAgents)
        {
            var sql = @"INSERT INTO task_agents (task_id, agent_id, role, created_at) 
                        VALUES (@TaskId, @AgentId, @Role, @CreatedAt) 
                        RETURNING id";
            var id = await connection.ExecuteScalarAsync<long>(sql, taskAgent);
            taskAgent.Id = id;
        }
        return taskAgents;
    }

    public async Task<bool> DeleteByTaskIdAsync(long taskId)
    {
        using var connection = _context.CreateConnection();
        var sql = "DELETE FROM task_agents WHERE task_id = @TaskId";
        var result = await connection.ExecuteAsync(sql, new { TaskId = taskId });
        return result > 0;
    }

    public async Task<bool> DeleteAsync(long taskId, long agentId)
    {
        using var connection = _context.CreateConnection();
        var sql = "DELETE FROM task_agents WHERE task_id = @TaskId AND agent_id = @AgentId";
        var result = await connection.ExecuteAsync(sql, new { TaskId = taskId, AgentId = agentId });
        return result > 0;
    }
}
