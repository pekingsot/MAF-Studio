using Dapper;
using Dapper.Contrib.Extensions;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class WorkflowPlanRepository : IWorkflowPlanRepository
{
    private readonly IDapperContext _context;
    private readonly ILogger<WorkflowPlanRepository> _logger;

    public WorkflowPlanRepository(IDapperContext context, ILogger<WorkflowPlanRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowPlan?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        return await connection.GetAsync<WorkflowPlan>(id);
    }

    public async Task<List<WorkflowPlan>> GetByCollaborationIdAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM workflow_plans WHERE collaboration_id = @CollaborationId ORDER BY created_at DESC";
        var result = await connection.QueryAsync<WorkflowPlan>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }

    public async Task<List<WorkflowPlan>> GetPendingPlansAsync(long collaborationId)
    {
        using var connection = _context.CreateConnection();
        var sql = "SELECT * FROM workflow_plans WHERE collaboration_id = @CollaborationId AND status = 'pending' ORDER BY created_at DESC";
        var result = await connection.QueryAsync<WorkflowPlan>(sql, new { CollaborationId = collaborationId });
        return result.ToList();
    }

    public async Task<long> CreateAsync(WorkflowPlan plan)
    {
        using var connection = _context.CreateConnection();
        var id = await connection.InsertAsync(plan);
        _logger.LogInformation("创建工作流计划，ID: {Id}, 协作ID: {CollaborationId}", id, plan.CollaborationId);
        return id;
    }

    public async Task<bool> UpdateAsync(WorkflowPlan plan)
    {
        using var connection = _context.CreateConnection();
        plan.UpdatedAt = DateTime.UtcNow;
        var result = await connection.UpdateAsync(plan);
        _logger.LogInformation("更新工作流计划，ID: {Id}, 状态: {Status}", plan.Id, plan.Status);
        return result;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        var result = await connection.DeleteAsync(new WorkflowPlan { Id = id });
        _logger.LogInformation("删除工作流计划，ID: {Id}", id);
        return result;
    }

    public async Task<bool> UpdateStatusAsync(long id, string status, long? approvedBy = null)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            UPDATE workflow_plans 
            SET status = @Status, 
                updated_at = @UpdatedAt,
                approved_at = CASE WHEN @Status IN ('approved', 'executing') THEN @Now ELSE approved_at END,
                approved_by = CASE WHEN @Status IN ('approved', 'executing') THEN @ApprovedBy ELSE approved_by END,
                executed_at = CASE WHEN @Status = 'executing' THEN @Now ELSE executed_at END,
                completed_at = CASE WHEN @Status = 'completed' THEN @Now ELSE completed_at END
            WHERE id = @Id";
        
        var result = await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            Status = status, 
            UpdatedAt = DateTime.UtcNow,
            Now = DateTime.UtcNow,
            ApprovedBy = approvedBy
        });
        
        _logger.LogInformation("更新工作流计划状态，ID: {Id}, 状态: {Status}", id, status);
        return result > 0;
    }
}
