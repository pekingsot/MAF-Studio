using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Entities;
using MAFStudio.Application.VOs;
using MAFStudio.Application.Mappers;
using System.Security.Claims;
using MAFStudio.Application.DTOs.Requests;
using System.Text.Json;
using MAFStudio.Api.Extensions;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IAgentTypeRepository _agentTypeRepository;
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _llmModelConfigRepository;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;
    private readonly ILogger<AgentsController> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AgentsController(
        IAgentService agentService, 
        IAgentTypeRepository agentTypeRepository,
        ILlmConfigRepository llmConfigRepository,
        ILlmModelConfigRepository llmModelConfigRepository,
        IAuthService authService, 
        IOperationLogService logService,
        ILogger<AgentsController> logger)
    {
        _agentService = agentService;
        _agentTypeRepository = agentTypeRepository;
        _llmConfigRepository = llmConfigRepository;
        _llmModelConfigRepository = llmModelConfigRepository;
        _authService = authService;
        _logService = logService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AgentListItemVo>>> GetAllAgents()
    {
        var userId = User.GetUserId();
        var isAdmin = await _authService.IsAdminAsync(userId);
        
        var agents = await _agentService.GetByUserIdAsync(userId, isAdmin);
        
        return Ok(agents.Select(a => a.ToListItemVo()).ToList());
    }

    [HttpGet("types")]
    public async Task<ActionResult<List<AgentTypeVo>>> GetAgentTypes()
    {
        var agentTypes = await _agentTypeRepository.GetEnabledAsync();
        return Ok(agentTypes.Select(at => at.ToVo()).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AgentVo>> GetAgent(long id)
    {
        var userId = User.GetUserId();
        var isAdmin = await _authService.IsAdminAsync(userId);
        
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
    public async Task<ActionResult<AgentVo>> CreateAgent([FromBody] CreateAgentRequest request)
    {
        var userId = User.GetUserId();
        
        _logger.LogInformation("创建智能体请求: LlmConfigId={LlmConfigId}, LlmModelConfigId={LlmModelConfigId}", 
            request.LlmConfigId, request.LlmModelConfigId);
        
        var fallbackModelsJson = await BuildFallbackModelsJsonAsync(request.FallbackModels);
        
        var (llmConfigName, llmModelName) = await GetLlmNamesAsync(request.LlmConfigId, request.LlmModelConfigId);
        
        _logger.LogInformation("查询到的名称: llmConfigName={LlmConfigName}, llmModelName={LlmModelName}", 
            llmConfigName, llmModelName);
        
        var typeName = await GetTypeNameAsync(request.Type);
        
        var agent = await _agentService.CreateAsync(
            request.Name,
            request.Description,
            request.Type,
            request.SystemPrompt,
            request.Avatar,
            userId,
            request.LlmConfigId,
            request.LlmModelConfigId,
            fallbackModelsJson,
            typeName,
            llmConfigName,
            llmModelName
        );
        
        await _logService.LogAsync(userId, "创建", "智能体", $"创建智能体: {request.Name}", 
            JsonSerializer.Serialize(request));
        
        return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, agent.ToVo());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AgentVo>> UpdateAgent(long id, [FromBody] UpdateAgentRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = await _authService.IsAdminAsync(userId);
        
        var existingAgent = await _agentService.GetByIdAsync(id);
        if (existingAgent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && existingAgent.UserId != userId)
        {
            return Forbid();
        }
        
        var fallbackModelsJson = await BuildFallbackModelsJsonAsync(request.FallbackModels);
        
        var (llmConfigName, llmModelName) = await GetLlmNamesAsync(request.LlmConfigId, request.LlmModelConfigId);
        
        var typeName = await GetTypeNameAsync(existingAgent.Type);
        
        var agent = await _agentService.UpdateAsync(
            id, 
            request.Name, 
            request.Description, 
            request.SystemPrompt, 
            request.Avatar, 
            request.LlmConfigId, 
            request.LlmModelConfigId,
            fallbackModelsJson,
            typeName,
            llmConfigName,
            llmModelName);
        
        await _logService.LogAsync(userId, "修改", "智能体", $"修改智能体: {request.Name}",
            JsonSerializer.Serialize(request));
        
        return Ok(agent.ToVo());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAgent(long id)
    {
        var userId = User.GetUserId();
        var isAdmin = await _authService.IsAdminAsync(userId);
        
        var existingAgent = await _agentService.GetByIdAsync(id);
        if (existingAgent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && existingAgent.UserId != userId)
        {
            return Forbid();
        }
        
        await _logService.LogAsync(userId, "删除", "智能体", $"删除智能体: {existingAgent.Name}",
            JsonSerializer.Serialize(new { id, name = existingAgent.Name }));
        
        var result = await _agentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<AgentVo>> UpdateAgentStatus(long id, [FromBody] UpdateAgentStatusRequest request)
    {
        var userId = User.GetUserId();
        var isAdmin = await _authService.IsAdminAsync(userId);
        
        var existingAgent = await _agentService.GetByIdAsync(id);
        if (existingAgent == null)
        {
            return NotFound();
        }
        
        if (!isAdmin && existingAgent.UserId != userId)
        {
            return Forbid();
        }
        
        var success = await _agentService.UpdateStatusAsync(id, request.Status);
        if (!success)
        {
            return BadRequest(new { message = "更新状态失败" });
        }
        
        var updatedAgent = await _agentService.GetByIdAsync(id);
        return Ok(updatedAgent?.ToVo());
    }

    private async Task<(string? configName, string? modelName)> GetLlmNamesAsync(long? llmConfigId, long? llmModelConfigId)
    {
        if (!llmConfigId.HasValue)
        {
            return (null, null);
        }

        var config = await _llmConfigRepository.GetByIdAsync(llmConfigId.Value);
        if (config == null)
        {
            return (null, null);
        }

        string? modelName = null;
        if (llmModelConfigId.HasValue)
        {
            var model = await _llmModelConfigRepository.GetByIdAsync(llmModelConfigId.Value);
            modelName = model?.DisplayName ?? model?.ModelName;
        }

        return (config.Name, modelName);
    }

    private async Task<string?> GetTypeNameAsync(string typeCode)
    {
        var types = await _agentTypeRepository.GetEnabledAsync();
        var type = types.FirstOrDefault(t => t.Code == typeCode);
        return type?.Name;
    }

    private async Task<string?> BuildFallbackModelsJsonAsync(List<FallbackModelRequest>? fallbackModels)
    {
        if (fallbackModels == null || fallbackModels.Count == 0)
        {
            return null;
        }

        var fallbackModelsDetail = new List<object>();
        foreach (var fm in fallbackModels)
        {
            var config = await _llmConfigRepository.GetByIdAsync(fm.LlmConfigId);
            LlmModelConfig? model = null;
            if (fm.LlmModelConfigId.HasValue)
            {
                model = await _llmModelConfigRepository.GetByIdAsync(fm.LlmModelConfigId.Value);
            }

            fallbackModelsDetail.Add(new
            {
                llmConfigId = fm.LlmConfigId,
                llmConfigName = config?.Name,
                llmModelConfigId = fm.LlmModelConfigId,
                modelName = model?.DisplayName ?? model?.ModelName,
                priority = fm.Priority
            });
        }

        return JsonSerializer.Serialize(fallbackModelsDetail, JsonOptions);
    }
}
