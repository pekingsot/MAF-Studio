using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollaborationWorkflowController : ControllerBase
{
    private readonly ICollaborationWorkflowService _workflowService;
    private readonly ILogger<CollaborationWorkflowController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    public CollaborationWorkflowController(
        ICollaborationWorkflowService workflowService,
        ILogger<CollaborationWorkflowController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpPost("{collaborationId}/concurrent")]
    public async Task<ActionResult<CollaborationResult>> ExecuteConcurrent(
        long collaborationId,
        [FromBody] WorkflowRequest request)
    {
        try
        {
            var result = await _workflowService.ExecuteConcurrentAsync(
                collaborationId,
                request.Input);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行并发工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{collaborationId}/handoffs")]
    public async Task<ActionResult<CollaborationResult>> ExecuteHandoffs(
        long collaborationId,
        [FromBody] WorkflowRequest request)
    {
        try
        {
            var result = await _workflowService.ExecuteHandoffsAsync(
                collaborationId,
                request.Input);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行任务移交工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{collaborationId}/groupchat")]
    public async Task ExecuteGroupChat(
        long collaborationId,
        [FromBody] GroupChatWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var message in _workflowService.ExecuteGroupChatAsync(
                collaborationId,
                request.Input,
                request.Parameters,
                request.TaskId,
                cancellationToken))
            {
                var json = JsonSerializer.Serialize(message, JsonOptions);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行群聊工作流失败");
            var errorJson = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorJson}\n\n");
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpPost("{collaborationId}/review-iterative")]
    public async Task<ActionResult<CollaborationResult>> ExecuteReviewIterative(
        long collaborationId,
        [FromBody] ReviewIterativeRequest request)
    {
        try
        {
            var result = await _workflowService.ExecuteReviewIterativeAsync(
                collaborationId,
                request.Input,
                request.Parameters);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行审阅迭代工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{collaborationId}/magentic/generate")]
    public async Task<ActionResult<GenerateMagenticPlanResponse>> GenerateMagenticPlan(
        long collaborationId,
        [FromBody] GenerateMagenticPlanRequest request)
    {
        try
        {
            request.CollaborationId = collaborationId;
            var workflow = await _workflowService.GenerateMagenticPlanAsync(
                collaborationId,
                request.Task);

            return Ok(new GenerateMagenticPlanResponse
            {
                Success = true,
                Workflow = workflow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Magentic计划失败");
            return Ok(new GenerateMagenticPlanResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    [HttpPost("{collaborationId}/magentic/execute")]
    public async Task ExecuteMagenticWorkflow(
        long collaborationId,
        [FromBody] MagenticWorkflowExecuteRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var message in _workflowService.ExecuteMagenticWorkflowStreamAsync(
                collaborationId,
                request.Workflow,
                request.Input,
                request.TaskId,
                cancellationToken))
            {
                var json = JsonSerializer.Serialize(message, JsonOptions);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Magentic工作流失败");
            var errorJson = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"data: {errorJson}\n\n");
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}

public class WorkflowRequest
{
    public string Input { get; set; } = string.Empty;
}

public class GroupChatWorkflowRequest
{
    public string Input { get; set; } = string.Empty;
    public GroupChatParameters? Parameters { get; set; }
    public long? TaskId { get; set; }
}

public class ReviewIterativeRequest
{
    public string Input { get; set; } = string.Empty;
    public ReviewIterativeParameters? Parameters { get; set; }
}

public class MagenticWorkflowExecuteRequest
{
    public WorkflowDefinitionDto Workflow { get; set; } = new();
    public string Input { get; set; } = string.Empty;
    public long? TaskId { get; set; }
}
