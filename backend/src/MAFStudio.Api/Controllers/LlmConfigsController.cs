using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Interfaces.Repositories;
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
    private readonly ILlmModelConfigRepository _modelConfigRepository;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;
    private readonly ILogger<LlmConfigsController> _logger;

    public LlmConfigsController(
        ILlmConfigService llmConfigService, 
        ILlmModelConfigRepository modelConfigRepository,
        IAuthService authService, 
        IOperationLogService _logService,
        ILogger<LlmConfigsController> logger)
    {
        _llmConfigService = llmConfigService;
        _modelConfigRepository = modelConfigRepository;
        _authService = authService;
        this._logService = _logService;
        _logger = logger;
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

    [HttpGet("providers")]
    [AllowAnonymous]
    public ActionResult<List<ProviderInfo>> GetProviders()
    {
        var providers = new List<ProviderInfo>
        {
            new ProviderInfo { Id = "qwen", DisplayName = "阿里千问", DefaultEndpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1", DefaultModel = "qwen-max" },
            new ProviderInfo { Id = "zhipu", DisplayName = "智谱AI", DefaultEndpoint = "https://open.bigmodel.cn/api/paas/v4", DefaultModel = "glm-4" },
            new ProviderInfo { Id = "openai", DisplayName = "OpenAI", DefaultEndpoint = "https://api.openai.com/v1", DefaultModel = "gpt-4o" },
            new ProviderInfo { Id = "deepseek", DisplayName = "DeepSeek", DefaultEndpoint = "https://api.deepseek.com/v1", DefaultModel = "deepseek-chat" },
            new ProviderInfo { Id = "anthropic", DisplayName = "Anthropic", DefaultEndpoint = "https://api.anthropic.com/v1", DefaultModel = "claude-3-opus-20240229" },
            new ProviderInfo { Id = "openai_compatible", DisplayName = "OpenAI兼容", DefaultEndpoint = "", DefaultModel = "gpt-4o" }
        };
        return Ok(providers);
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
            request.DefaultModel
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

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(long id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _llmConfigService.SetDefaultAsync(id, userId!);
        return Ok(new { message = "设置成功" });
    }

    [HttpPost("{id}/test")]
    public async Task<ActionResult> TestConnection(long id)
    {
        try
        {
            var result = await _llmConfigService.TestConnectionAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试连接失败: {Id}", id);
            return Ok(new { success = false, message = ex.Message, latencyMs = 0 });
        }
    }

    [HttpGet("{id}/models")]
    public async Task<ActionResult<List<Core.Entities.LlmModelConfig>>> GetModels(long id)
    {
        var models = await _modelConfigRepository.GetByLlmConfigIdAsync(id);
        return Ok(models);
    }

    [HttpPost("{id}/models")]
    public async Task<ActionResult<Core.Entities.LlmModelConfig>> CreateModel(long id, [FromBody] CreateModelConfigRequest request)
    {
        var model = new Core.Entities.LlmModelConfig
        {
            LlmConfigId = id,
            ModelName = request.ModelName,
            DisplayName = request.DisplayName,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            ContextWindow = request.ContextWindow,
            TopP = request.TopP,
            FrequencyPenalty = request.FrequencyPenalty,
            PresencePenalty = request.PresencePenalty,
            StopSequences = request.StopSequences,
            IsDefault = request.IsDefault,
            IsEnabled = request.IsEnabled,
            SortOrder = request.SortOrder
        };
        
        var created = await _modelConfigRepository.CreateAsync(model);
        return CreatedAtAction(nameof(GetModels), new { id }, created);
    }

    [HttpPut("{id}/models/{modelId}")]
    public async Task<ActionResult<Core.Entities.LlmModelConfig>> UpdateModel(long id, long modelId, [FromBody] UpdateModelConfigRequest request)
    {
        var model = await _modelConfigRepository.GetByIdAsync(modelId);
        if (model == null || model.LlmConfigId != id)
        {
            return NotFound();
        }

        model.ModelName = request.ModelName;
        model.DisplayName = request.DisplayName;
        model.Temperature = request.Temperature;
        model.MaxTokens = request.MaxTokens;
        model.ContextWindow = request.ContextWindow;
        model.TopP = request.TopP;
        model.FrequencyPenalty = request.FrequencyPenalty;
        model.PresencePenalty = request.PresencePenalty;
        model.StopSequences = request.StopSequences;
        model.IsEnabled = request.IsEnabled;
        model.SortOrder = request.SortOrder;

        var updated = await _modelConfigRepository.UpdateAsync(model);
        return Ok(updated);
    }

    [HttpDelete("{id}/models/{modelId}")]
    public async Task<ActionResult> DeleteModel(long id, long modelId)
    {
        var result = await _modelConfigRepository.DeleteAsync(modelId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{id}/models/{modelId}/set-default")]
    public async Task<ActionResult> SetModelDefault(long id, long modelId)
    {
        await _modelConfigRepository.SetDefaultAsync(id, modelId);
        return Ok(new { message = "设置成功" });
    }

    [HttpPost("{id}/models/{modelId}/test")]
    public async Task<ActionResult> TestModelConnection(long id, long modelId)
    {
        try
        {
            var result = await _llmConfigService.TestModelConnectionAsync(id, modelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试模型连接失败: {ConfigId}, {ModelId}", id, modelId);
            return Ok(new { success = false, message = ex.Message, latencyMs = 0 });
        }
    }
}

public class ProviderInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DefaultEndpoint { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
}

public class CreateModelConfigRequest
{
    public string ModelName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public decimal Temperature { get; set; } = 0.7m;
    public int MaxTokens { get; set; } = 4096;
    public int ContextWindow { get; set; } = 8192;
    public decimal? TopP { get; set; }
    public decimal? FrequencyPenalty { get; set; }
    public decimal? PresencePenalty { get; set; }
    public string? StopSequences { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

public class UpdateModelConfigRequest
{
    public string ModelName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public decimal Temperature { get; set; } = 0.7m;
    public int MaxTokens { get; set; } = 4096;
    public int ContextWindow { get; set; } = 8192;
    public decimal? TopP { get; set; }
    public decimal? FrequencyPenalty { get; set; }
    public decimal? PresencePenalty { get; set; }
    public string? StopSequences { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
