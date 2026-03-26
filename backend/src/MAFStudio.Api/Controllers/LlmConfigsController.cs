using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.VOs;
using MAFStudio.Application.Mappers;
using MAFStudio.Application.DTOs.Requests;
using System.Security.Claims;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LlmConfigsController : ControllerBase
{
    private readonly ILlmConfigService _llmConfigService;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;

    public LlmConfigsController(ILlmConfigService llmConfigService, IAuthService authService, IOperationLogService logService)
    {
        _llmConfigService = llmConfigService;
        _authService = authService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LlmConfigVo>>> GetAllLlmConfigs()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = await _authService.IsAdminAsync(userId!);
        
        var configs = isAdmin 
            ? await _llmConfigService.GetAllAsync()
            : await _llmConfigService.GetByUserIdAsync(userId!);
        
        var vos = configs.Select(c => c.ToVo()).ToList();
        return Ok(vos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LlmConfigVo>> GetLlmConfig(long id)
    {
        var config = await _llmConfigService.GetByIdAsync(id);
        if (config == null)
        {
            return NotFound();
        }
        
        return Ok(config.ToVo());
    }

    [HttpPost]
    public async Task<ActionResult<Core.Entities.LlmConfig>> CreateLlmConfig([FromBody] CreateLlmConfigRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var config = await _llmConfigService.CreateAsync(
            request.Name,
            request.Provider,
            request.ApiKey,
            request.Endpoint,
            request.DefaultModel,
            request.ExtraConfig,
            userId!
        );
        
        await _logService.LogAsync(userId!, "创建", "LLM配置", $"创建LLM配置: {request.Name}", null);
        
        return CreatedAtAction(nameof(GetLlmConfig), new { id = config.Id }, config);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Core.Entities.LlmConfig>> UpdateLlmConfig(long id, [FromBody] UpdateLlmConfigRequest request)
    {
        var config = await _llmConfigService.UpdateAsync(
            id,
            request.Name,
            request.ApiKey,
            request.Endpoint,
            request.DefaultModel,
            request.ExtraConfig
        );
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _logService.LogAsync(userId!, "修改", "LLM配置", $"修改LLM配置: {request.Name}", null);
        
        return Ok(config);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLlmConfig(long id)
    {
        var result = await _llmConfigService.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }
        
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _logService.LogAsync(userId!, "删除", "LLM配置", $"删除LLM配置: {id}", null);
        
        return NoContent();
    }
}
