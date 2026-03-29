using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/collaborations/{collaborationId}/workflow")]
public class CollaborationWorkflowController : ControllerBase
{
    private readonly ICollaborationWorkflowService _workflowService;
    private readonly ILogger<CollaborationWorkflowController> _logger;

    public CollaborationWorkflowController(
        ICollaborationWorkflowService workflowService,
        ILogger<CollaborationWorkflowController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    /// <summary>
    /// 执行工作流（关联任务）
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<CollaborationResult>> ExecuteWorkflow(
        long collaborationId,
        [FromBody] ExecuteWorkflowRequest request)
    {
        try
        {
            var result = request.WorkflowType.ToLower() switch
            {
                "sequential" => await _workflowService.ExecuteSequentialAsync(
                    collaborationId, 
                    request.Input ?? $"执行任务ID: {request.TaskId}"),
                    
                "concurrent" => await _workflowService.ExecuteConcurrentAsync(
                    collaborationId, 
                    request.Input ?? $"执行任务ID: {request.TaskId}"),
                    
                "handoffs" => await _workflowService.ExecuteHandoffsAsync(
                    collaborationId, 
                    request.Input ?? $"执行任务ID: {request.TaskId}"),
                    
                "reviewiterative" => await _workflowService.ExecuteReviewIterativeAsync(
                    collaborationId, 
                    request.Input ?? $"执行任务ID: {request.TaskId}",
                    request.Parameters != null ? 
                        System.Text.Json.JsonSerializer.Deserialize<ReviewIterativeParameters>(
                            System.Text.Json.JsonSerializer.Serialize(request.Parameters)) : null),
                    
                _ => throw new ArgumentException($"不支持的工作流类型: {request.WorkflowType}")
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 执行审阅迭代工作流
    /// </summary>
    [HttpPost("review-iterative")]
    public async Task<ActionResult<CollaborationResult>> ExecuteReviewIterative(
        long collaborationId,
        [FromBody] ReviewIterativeRequest request)
    {
        try
        {
            var result = await _workflowService.ExecuteReviewIterativeAsync(
                collaborationId,
                request.Input,
                new ReviewIterativeParameters
                {
                    MaxIterations = request.MaxIterations,
                    ReviewCriteria = request.ReviewCriteria,
                    ApprovalKeyword = request.ApprovalKeyword ?? "[APPROVED]",
                    SaveVersions = request.SaveVersions ?? true
                });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行审阅迭代工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 执行顺序工作流（旧接口，保持兼容）
    /// </summary>
    [HttpPost("sequential")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<CollaborationResult>> ExecuteSequential(
        long collaborationId,
        [FromBody] WorkflowRequest request)
    {
        try
        {
            var result = await _workflowService.ExecuteSequentialAsync(
                collaborationId,
                request.Input);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行顺序工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 执行并发工作流（旧接口，保持兼容）
    /// </summary>
    [HttpPost("concurrent")]
    [ApiExplorerSettings(IgnoreApi = true)]
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

    /// <summary>
    /// 执行任务移交工作流（旧接口，保持兼容）
    /// </summary>
    [HttpPost("handoffs")]
    [ApiExplorerSettings(IgnoreApi = true)]
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

    /// <summary>
    /// 执行群聊协作工作流（流式返回）
    /// </summary>
    [HttpPost("groupchat")]
    public async IAsyncEnumerable<ChatMessageDto> ExecuteGroupChat(
        long collaborationId,
        [FromBody] WorkflowRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in _workflowService.ExecuteGroupChatAsync(
            collaborationId,
            request.Input,
            cancellationToken))
        {
            yield return message;
        }
    }
}

/// <summary>
/// 执行工作流请求
/// </summary>
public class ExecuteWorkflowRequest
{
    /// <summary>
    /// 任务ID（可选，如果提供则从任务获取输入）
    /// </summary>
    public long? TaskId { get; set; }

    /// <summary>
    /// 工作流类型：Sequential, Concurrent, Handoffs, GroupChat
    /// </summary>
    public string WorkflowType { get; set; } = "Sequential";

    /// <summary>
    /// 输入内容（可选，如果未提供TaskId则必填）
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// 工作流参数
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// 工作流请求（旧接口，保持兼容）
/// </summary>
public class WorkflowRequest
{
    public string Input { get; set; } = string.Empty;
}

/// <summary>
/// 审阅迭代工作流请求
/// </summary>
public class ReviewIterativeRequest
{
    /// <summary>
    /// 输入内容
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// 最大迭代次数（默认10次）
    /// </summary>
    public int? MaxIterations { get; set; }

    /// <summary>
    /// 审阅标准
    /// </summary>
    public string? ReviewCriteria { get; set; }

    /// <summary>
    /// 通过关键词（默认 [APPROVED]）
    /// </summary>
    public string? ApprovalKeyword { get; set; }

    /// <summary>
    /// 是否保存版本（默认true）
    /// </summary>
    public bool? SaveVersions { get; set; }
}
