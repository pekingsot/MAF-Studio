using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.VOs;
using MAFStudio.Application.Mappers;
using MAFStudio.Application.DTOs.Requests;
using System.Security.Claims;
using MAFStudio.Core.Enums;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollaborationsController : ControllerBase
{
    private readonly ICollaborationService _collaborationService;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;

    public CollaborationsController(ICollaborationService collaborationService, IAuthService authService, IOperationLogService logService)
    {
        _collaborationService = collaborationService;
        _authService = authService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CollaborationVo>>> GetAllCollaborations()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var collaborations = await _collaborationService.GetByUserIdAsync(userId!);
        var vos = collaborations.Select(c => c.ToVo()).ToList();
        return Ok(vos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CollaborationVo>> GetCollaboration(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var collaboration = await _collaborationService.GetByIdAsync(id, userId!);
        if (collaboration == null)
        {
            return NotFound();
        }

        var vo = collaboration.ToVo();
        var agents = await _collaborationService.GetAgentsAsync(id);
        vo.Agents = agents.Select(a => new CollaborationAgentVo
        {
            AgentId = a.AgentId,
            Role = a.Role,
            JoinedAt = a.JoinedAt
        }).ToList();

        return Ok(vo);
    }

    [HttpPost]
    public async Task<ActionResult<Core.Entities.Collaboration>> CreateCollaboration([FromBody] CreateCollaborationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var collaboration = await _collaborationService.CreateAsync(
            request.Name,
            request.Description,
            request.Path,
            request.GitRepositoryUrl,
            request.GitBranch,
            request.GitUsername,
            request.GitEmail,
            request.GitAccessToken,
            userId!
        );
        
        await _logService.LogAsync(userId!, "创建", "协作项目", $"创建协作项目: {request.Name}", null);
        
        return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaboration);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCollaboration(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var result = await _collaborationService.DeleteAsync(id, userId!);
        if (!result)
        {
            return NotFound();
        }
        
        await _logService.LogAsync(userId!, "删除", "协作项目", $"删除协作项目: {id}", null);
        
        return NoContent();
    }

    [HttpPost("{id}/agents")]
    public async Task<ActionResult> AddAgentToCollaboration(Guid id, [FromBody] AddAgentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var result = await _collaborationService.AddAgentAsync(id, request.AgentId, request.Role, userId!);
        if (!result)
        {
            return BadRequest();
        }
        
        return Ok();
    }

    [HttpDelete("{id}/agents/{agentId}")]
    public async Task<ActionResult> RemoveAgentFromCollaboration(Guid id, Guid agentId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var result = await _collaborationService.RemoveAgentAsync(id, agentId, userId!);
        if (!result)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    [HttpPost("{id}/tasks")]
    public async Task<ActionResult<Core.Entities.CollaborationTask>> CreateTask(Guid id, [FromBody] CreateTaskRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var task = await _collaborationService.CreateTaskAsync(id, request.Title, request.Description, userId!);
        
        return CreatedAtAction(nameof(GetCollaboration), new { id }, task);
    }

    [HttpPatch("tasks/{taskId}/status")]
    public async Task<ActionResult<Core.Entities.CollaborationTask>> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!Enum.TryParse<CollaborationTaskStatus>(request.Status, out var status))
        {
            return BadRequest("Invalid status value");
        }
        
        var task = await _collaborationService.UpdateTaskStatusAsync(taskId, status, userId!);
        
        return Ok(task);
    }

    [HttpDelete("tasks/{taskId}")]
    public async Task<ActionResult> DeleteTask(Guid taskId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var result = await _collaborationService.DeleteTaskAsync(taskId, userId!);
        if (!result)
        {
            return NotFound();
        }
        
        return NoContent();
    }
}
