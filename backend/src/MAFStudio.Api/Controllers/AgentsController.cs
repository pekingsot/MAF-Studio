using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Application.VOs;
using MAFStudio.Application.Mappers;
using System.Security.Claims;
using MAFStudio.Application.DTOs.Requests;
using System.Text.Json;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IAgentTypeRepository _agentTypeRepository;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;

    public AgentsController(
        IAgentService agentService, 
        IAgentTypeRepository agentTypeRepository,
        IAuthService authService, 
        IOperationLogService logService)
    {
        _agentService = agentService;
        _agentTypeRepository = agentTypeRepository;
        _authService = authService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<AgentListVo>> GetAllAgents()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = await _authService.IsAdminAsync(userId!);
        
        var agents = await _agentService.GetByUserIdAsync(userId!, isAdmin);
        var agentTypes = await _agentTypeRepository.GetEnabledAsync();
        
        var result = new AgentListVo
        {
            Agents = agents.Select(a => a.ToListItemVo()).ToList(),
            AgentTypes = agentTypes.Select(at => at.ToVo()).ToList()
        };
        
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgentVo>> GetAgent(long id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = await _authService.IsAdminAsync(userId!);
        
        var agent = await _agentService.GetByIdAsync(id);
        if (agent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && agent.UserId != userId)
        {
            return Forbid();
        }
        
        return Ok(agent.ToVo());
    }

    [HttpPost]
    public async Task<ActionResult<Core.Entities.Agent>> CreateAgent([FromBody] CreateAgentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        string? fallbackModelsJson = null;
        if (request.FallbackModels != null && request.FallbackModels.Count > 0)
        {
            fallbackModelsJson = JsonSerializer.Serialize(request.FallbackModels);
        }
        
        var agent = await _agentService.CreateAsync(
            request.Name,
            request.Description,
            request.Type,
            request.SystemPrompt,
            request.Avatar,
            userId!,
            request.LlmConfigId,
            request.LlmModelConfigId,
            fallbackModelsJson
        );
        
        await _logService.LogAsync(userId!, "创建", "智能体", $"创建智能体: {request.Name}", 
            JsonSerializer.Serialize(request));
        
        return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, agent);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Core.Entities.Agent>> UpdateAgent(long id, [FromBody] UpdateAgentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = await _authService.IsAdminAsync(userId!);
        
        var existingAgent = await _agentService.GetByIdAsync(id);
        if (existingAgent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && existingAgent.UserId != userId)
        {
            return Forbid();
        }
        
        string? fallbackModelsJson = null;
        if (request.FallbackModels != null && request.FallbackModels.Count > 0)
        {
            fallbackModelsJson = JsonSerializer.Serialize(request.FallbackModels);
        }
        
        var agent = await _agentService.UpdateAsync(
            id, 
            request.Name, 
            request.Description, 
            request.SystemPrompt, 
            request.Avatar, 
            request.LlmConfigId, 
            request.LlmModelConfigId,
            fallbackModelsJson);
        
        await _logService.LogAsync(userId!, "修改", "智能体", $"修改智能体: {request.Name}",
            JsonSerializer.Serialize(request));
        
        return Ok(agent);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAgent(long id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = await _authService.IsAdminAsync(userId!);
        
        var existingAgent = await _agentService.GetByIdAsync(id);
        if (existingAgent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && existingAgent.UserId != userId)
        {
            return Forbid();
        }
        
        await _logService.LogAsync(userId!, "删除", "智能体", $"删除智能体: {existingAgent.Name}",
            JsonSerializer.Serialize(new { id, name = existingAgent.Name }));
        
        var result = await _agentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<Core.Entities.Agent>> UpdateAgentStatus(long id, [FromBody] UpdateAgentStatusRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = await _authService.IsAdminAsync(userId!);
        
        var existingAgent = await _agentService.GetByIdAsync(id);
        if (existingAgent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && existingAgent.UserId != userId)
        {
            return Forbid();
        }
        
        var agent = await _agentService.UpdateStatusAsync(id, request.Status);
        return Ok(agent);
    }
}
