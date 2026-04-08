using MAFStudio.Application.DTOs;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public interface IWorkflowExecutionService
{
    Task<long> StartExecutionAsync(
        long collaborationId,
        long? taskId,
        string workflowType,
        string input);

    Task<WorkflowExecution?> GetExecutionAsync(long executionId);
    Task<List<WorkflowExecution>> GetExecutionsByCollaborationAsync(long collaborationId);
    Task<List<WorkflowExecutionMessage>> GetExecutionMessagesAsync(long executionId);
}

public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowExecutionService> _logger;

    public WorkflowExecutionService(
        IServiceProvider serviceProvider,
        ILogger<WorkflowExecutionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<long> StartExecutionAsync(
        long collaborationId,
        long? taskId,
        string workflowType,
        string input)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();

        var execution = new WorkflowExecution
        {
            Id = SnowflakeIdGenerator.Instance.NextId(),
            CollaborationId = collaborationId,
            TaskId = taskId,
            WorkflowType = workflowType,
            Input = input,
            Status = "Pending"
        };

        await executionRepo.CreateAsync(execution);

        _ = Task.Run(async () =>
        {
            using var innerScope = _serviceProvider.CreateScope();
            var innerExecutionRepo = innerScope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
            var workflowService = innerScope.ServiceProvider.GetRequiredService<CollaborationWorkflowService>();

            try
            {
                execution.Status = "Running";
                execution.StartedAt = DateTime.UtcNow;
                await innerExecutionRepo.UpdateAsync(execution);

                IAsyncEnumerable<ChatMessageDto> messages = workflowType.ToLower() switch
                {
                    "groupchat" => workflowService.ExecuteGroupChatAsync(collaborationId, input),
                    "magentic" => throw new NotSupportedException("Magentic工作流暂不支持后台执行"),
                    _ => throw new NotSupportedException($"不支持的工作流类型: {workflowType}")
                };

                await foreach (var message in messages)
                {
                    var executionMessage = new WorkflowExecutionMessage
                    {
                        Id = SnowflakeIdGenerator.Instance.NextId(),
                        ExecutionId = execution.Id,
                        Sender = message.Sender,
                        Content = message.Content,
                        Role = message.Role,
                        Timestamp = message.Timestamp
                    };

                    await innerExecutionRepo.AddMessageAsync(executionMessage);
                }

                execution.Status = "Completed";
                execution.CompletedAt = DateTime.UtcNow;
                await innerExecutionRepo.UpdateAsync(execution);

                _logger.LogInformation("工作流执行完成: {ExecutionId}", execution.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "工作流执行失败: {ExecutionId}", execution.Id);

                execution.Status = "Failed";
                execution.ErrorMessage = ex.Message;
                execution.CompletedAt = DateTime.UtcNow;
                await innerExecutionRepo.UpdateAsync(execution);
            }
        });

        return execution.Id;
    }

    public async Task<WorkflowExecution?> GetExecutionAsync(long executionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
        return await executionRepo.GetByIdAsync(executionId);
    }

    public async Task<List<WorkflowExecution>> GetExecutionsByCollaborationAsync(long collaborationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
        return await executionRepo.GetByCollaborationIdAsync(collaborationId);
    }

    public async Task<List<WorkflowExecutionMessage>> GetExecutionMessagesAsync(long executionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionRepository>();
        return await executionRepo.GetMessagesAsync(executionId);
    }
}
