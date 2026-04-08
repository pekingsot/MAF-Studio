using System.Text.Json;
using MAFStudio.Application.DTOs;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public partial class CollaborationWorkflowService
{
    public async Task<WorkflowPlanDto> GenerateAndSavePlanAsync(
        long collaborationId,
        string task,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await GenerateMagenticPlanAsync(collaborationId, task, cancellationToken);

        var plan = new WorkflowPlan
        {
            CollaborationId = collaborationId,
            Task = task,
            WorkflowDefinition = JsonSerializer.Serialize(workflow),
            Status = "pending",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var planId = await _workflowPlanRepository.CreateAsync(plan);
        plan.Id = planId;

        _logger.LogInformation("生成并保存工作流计划，ID: {PlanId}, 协作ID: {CollaborationId}", planId, collaborationId);

        return MapToDto(plan, workflow);
    }

    public async Task<WorkflowPlanDto?> GetPlanAsync(long planId)
    {
        var plan = await _workflowPlanRepository.GetByIdAsync(planId);
        if (plan == null)
            return null;

        var workflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(plan.WorkflowDefinition);
        return MapToDto(plan, workflow ?? new WorkflowDefinitionDto());
    }

    public async Task<List<WorkflowPlanDto>> GetPlansByCollaborationAsync(long collaborationId)
    {
        var plans = await _workflowPlanRepository.GetByCollaborationIdAsync(collaborationId);
        return plans.Select(p =>
        {
            var workflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(p.WorkflowDefinition);
            return MapToDto(p, workflow ?? new WorkflowDefinitionDto());
        }).ToList();
    }

    public async Task<WorkflowPlanDto> UpdatePlanAsync(long planId, WorkflowDefinitionDto workflow)
    {
        var plan = await _workflowPlanRepository.GetByIdAsync(planId);
        if (plan == null)
            throw new InvalidOperationException($"工作流计划 {planId} 不存在");

        plan.WorkflowDefinition = JsonSerializer.Serialize(workflow);
        plan.UpdatedAt = DateTime.UtcNow;

        await _workflowPlanRepository.UpdateAsync(plan);

        _logger.LogInformation("更新工作流计划，ID: {PlanId}", planId);

        return MapToDto(plan, workflow);
    }

    public async Task<CollaborationResult> ExecutePlanAsync(
        long planId,
        string input,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _workflowPlanRepository.GetByIdAsync(planId);
        if (plan == null)
        {
            return new CollaborationResult
            {
                Success = false,
                Error = $"工作流计划 {planId} 不存在"
            };
        }

        var workflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(plan.WorkflowDefinition);
        if (workflow == null)
        {
            return new CollaborationResult
            {
                Success = false,
                Error = "工作流定义无效"
            };
        }

        await _workflowPlanRepository.UpdateStatusAsync(planId, "executing", userId);

        try
        {
            var members = await _collaborationAgentRepository.GetByCollaborationIdAsync(plan.CollaborationId);
            var agentIds = members.Select(m => m.AgentId).ToList();

            foreach (var agentId in agentIds)
            {
                await _agentRepository.UpdateStatusAsync(agentId, AgentStatus.Busy);
            }
            _logger.LogInformation("已将 {Count} 个智能体状态改为Busy", agentIds.Count);

            var result = await ExecuteCustomWorkflowAsync(plan.CollaborationId, workflow, input, cancellationToken);

            foreach (var agentId in agentIds)
            {
                await _agentRepository.UpdateStatusAsync(agentId, AgentStatus.Active);
            }
            _logger.LogInformation("已将 {Count} 个智能体状态恢复为Active", agentIds.Count);

            if (result.Success)
            {
                await _workflowPlanRepository.UpdateStatusAsync(planId, "completed", userId);
            }
            else
            {
                await _workflowPlanRepository.UpdateStatusAsync(planId, "failed", userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行工作流计划失败，ID: {PlanId}", planId);
            await _workflowPlanRepository.UpdateStatusAsync(planId, "failed", userId);

            var members = await _collaborationAgentRepository.GetByCollaborationIdAsync(plan.CollaborationId);
            foreach (var member in members)
            {
                await _agentRepository.UpdateStatusAsync(member.AgentId, AgentStatus.Active);
            }

            return new CollaborationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<bool> DeletePlanAsync(long planId)
    {
        var result = await _workflowPlanRepository.DeleteAsync(planId);
        if (result)
        {
            _logger.LogInformation("删除工作流计划，ID: {PlanId}", planId);
        }
        return result;
    }

    private WorkflowPlanDto MapToDto(WorkflowPlan plan, WorkflowDefinitionDto workflow)
    {
        return new WorkflowPlanDto
        {
            Id = plan.Id,
            CollaborationId = plan.CollaborationId,
            Task = plan.Task,
            WorkflowDefinition = workflow,
            Status = plan.Status,
            CreatedBy = plan.CreatedBy,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            ApprovedAt = plan.ApprovedAt,
            ApprovedBy = plan.ApprovedBy,
            ExecutedAt = plan.ExecutedAt,
            CompletedAt = plan.CompletedAt
        };
    }
}
