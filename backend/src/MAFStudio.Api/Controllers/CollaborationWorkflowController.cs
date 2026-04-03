using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("{collaborationId}/sequential")]
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

    [HttpPost("{collaborationId}/concurrent")]
    public async Task<ActionResult<CollaborationResult>> ExecuteConcurrent(
        long collaborationId,
        [FromBody] ConcurrentWorkflowRequest request)
    {
        try
        {
            var result = await _workflowService.ExecuteConcurrentAsync(
                collaborationId,
                request.Input,
                request.ExecutorAgentIds,
                request.AggregatorAgentId,
                request.AggregationStrategy);

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
}

public class WorkflowRequest
{
    public string Input { get; set; } = string.Empty;
}

public class ConcurrentWorkflowRequest
{
    public string Input { get; set; } = string.Empty;
    
    /// <summary>
    /// 参与并发执行的Agent ID列表（如果不指定，则使用协作中的所有Agent）
    /// </summary>
    public List<long>? ExecutorAgentIds { get; set; }
    
    /// <summary>
    /// 聚合Agent ID（如果指定，则使用该Agent智能合并结果）
    /// </summary>
    public long? AggregatorAgentId { get; set; }
    
    /// <summary>
    /// 聚合策略：simple（简单拼接）或 intelligent（智能合并）
    /// </summary>
    public string AggregationStrategy { get; set; } = "simple";
}

public class ReviewIterativeRequest
{
    public string Input { get; set; } = string.Empty;
    public ReviewIterativeParameters? Parameters { get; set; }
}
