using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Application.Services;
using MAFStudio.Application.DTOs;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowExecutionController : ControllerBase
{
    private readonly IWorkflowExecutionService _executionService;
    private readonly ILogger<WorkflowExecutionController> _logger;

    public WorkflowExecutionController(
        IWorkflowExecutionService executionService,
        ILogger<WorkflowExecutionController> logger)
    {
        _executionService = executionService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartExecution([FromBody] StartExecutionRequest request)
    {
        try
        {
            var executionId = await _executionService.StartExecutionAsync(
                request.CollaborationId,
                request.TaskId,
                request.WorkflowType,
                request.Input
            );

            return Ok(new { executionId, message = "工作流已启动" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动工作流失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{executionId}")]
    public async Task<IActionResult> GetExecution(long executionId)
    {
        var execution = await _executionService.GetExecutionAsync(executionId);
        if (execution == null)
        {
            return NotFound(new { error = "执行记录不存在" });
        }

        return Ok(execution);
    }

    [HttpGet("collaboration/{collaborationId}")]
    public async Task<IActionResult> GetExecutionsByCollaboration(long collaborationId)
    {
        var executions = await _executionService.GetExecutionsByCollaborationAsync(collaborationId);
        return Ok(executions);
    }

    [HttpGet("{executionId}/messages")]
    public async Task<IActionResult> GetExecutionMessages(long executionId)
    {
        var messages = await _executionService.GetExecutionMessagesAsync(executionId);
        return Ok(messages);
    }

    [HttpGet("{executionId}/status")]
    public async Task<IActionResult> GetExecutionStatus(long executionId)
    {
        var execution = await _executionService.GetExecutionAsync(executionId);
        if (execution == null)
        {
            return NotFound(new { error = "执行记录不存在" });
        }

        var messages = await _executionService.GetExecutionMessagesAsync(executionId);

        return Ok(new
        {
            execution.Id,
            execution.Status,
            execution.StartedAt,
            execution.CompletedAt,
            execution.ErrorMessage,
            MessageCount = messages.Count
        });
    }
}

public class StartExecutionRequest
{
    public long CollaborationId { get; set; }
    public long? TaskId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
}
