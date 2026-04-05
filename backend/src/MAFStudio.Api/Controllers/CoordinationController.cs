using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoordinationController : ControllerBase
{
    private readonly IWorkflowSessionRepository _sessionRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<CoordinationController> _logger;

    public CoordinationController(
        IWorkflowSessionRepository sessionRepository,
        IMessageRepository messageRepository,
        ILogger<CoordinationController> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    [HttpGet("collaboration/{collaborationId}/sessions")]
    [AllowAnonymous]
    public async Task<ActionResult> GetSessions(long collaborationId, [FromQuery] int limit = 20)
    {
        try
        {
            var sessions = await _sessionRepository.GetByCollaborationIdAsync(collaborationId, limit);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工作流会话列表失败: {CollaborationId}", collaborationId);
            return StatusCode(500, new { message = "获取工作流会话列表失败" });
        }
    }

    [HttpGet("task/{taskId}/sessions")]
    [AllowAnonymous]
    public async Task<ActionResult> GetSessionsByTaskId(long taskId, [FromQuery] int limit = 20)
    {
        try
        {
            var sessions = await _sessionRepository.GetByTaskIdAsync(taskId, limit);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务执行记录列表失败: {TaskId}", taskId);
            return StatusCode(500, new { message = "获取任务执行记录列表失败" });
        }
    }

    [HttpGet("sessions/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetSession(long id)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound(new { message = "工作流会话不存在" });
            }
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工作流会话失败: {Id}", id);
            return StatusCode(500, new { message = "获取工作流会话失败" });
        }
    }

    [HttpGet("sessions/{sessionId}/messages")]
    [AllowAnonymous]
    public async Task<ActionResult> GetMessages(long sessionId)
    {
        try
        {
            var messages = await _messageRepository.GetBySessionIdAsync(sessionId);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会话消息列表失败: {SessionId}", sessionId);
            return StatusCode(500, new { message = "获取会话消息列表失败" });
        }
    }

    [HttpGet("sessions/{sessionId}/detail")]
    [AllowAnonymous]
    public async Task<ActionResult> GetSessionDetail(long sessionId)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { message = "工作流会话不存在" });
            }

            var messages = await _messageRepository.GetBySessionIdAsync(sessionId);

            return Ok(new
            {
                session,
                messages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工作流会话详情失败: {SessionId}", sessionId);
            return StatusCode(500, new { message = "获取工作流会话详情失败" });
        }
    }
}
