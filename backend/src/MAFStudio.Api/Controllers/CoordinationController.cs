using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoordinationController : ControllerBase
{
    private readonly ICoordinationSessionRepository _sessionRepository;
    private readonly ICoordinationRoundRepository _roundRepository;
    private readonly ICoordinationParticipantRepository _participantRepository;
    private readonly ILogger<CoordinationController> _logger;

    public CoordinationController(
        ICoordinationSessionRepository sessionRepository,
        ICoordinationRoundRepository roundRepository,
        ICoordinationParticipantRepository participantRepository,
        ILogger<CoordinationController> logger)
    {
        _sessionRepository = sessionRepository;
        _roundRepository = roundRepository;
        _participantRepository = participantRepository;
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
            _logger.LogError(ex, "获取协调会话列表失败: {CollaborationId}", collaborationId);
            return StatusCode(500, new { message = "获取协调会话列表失败" });
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
                return NotFound(new { message = "协调会话不存在" });
            }
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取协调会话失败: {Id}", id);
            return StatusCode(500, new { message = "获取协调会话失败" });
        }
    }

    [HttpGet("sessions/{sessionId}/rounds")]
    [AllowAnonymous]
    public async Task<ActionResult> GetRounds(long sessionId)
    {
        try
        {
            var rounds = await _roundRepository.GetBySessionIdAsync(sessionId);
            return Ok(rounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取协调轮次列表失败: {SessionId}", sessionId);
            return StatusCode(500, new { message = "获取协调轮次列表失败" });
        }
    }

    [HttpGet("sessions/{sessionId}/participants")]
    [AllowAnonymous]
    public async Task<ActionResult> GetParticipants(long sessionId)
    {
        try
        {
            var participants = await _participantRepository.GetBySessionIdAsync(sessionId);
            return Ok(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取协调参与者列表失败: {SessionId}", sessionId);
            return StatusCode(500, new { message = "获取协调参与者列表失败" });
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
                return NotFound(new { message = "协调会话不存在" });
            }

            var rounds = await _roundRepository.GetBySessionIdAsync(sessionId);
            var participants = await _participantRepository.GetBySessionIdAsync(sessionId);

            return Ok(new
            {
                session,
                rounds,
                participants
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取协调会话详情失败: {SessionId}", sessionId);
            return StatusCode(500, new { message = "获取协调会话详情失败" });
        }
    }
}
