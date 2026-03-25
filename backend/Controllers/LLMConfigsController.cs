using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Providers;
using MAFStudio.Backend.Models;
using MAFStudio.Backend.Models.Requests;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    /// <summary>
    /// 大模型配置控制器
    /// 提供大模型配置的增删改查接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LLMConfigsController : ControllerBase
    {
        private readonly ILLMConfigService _llmConfigService;

        public LLMConfigsController(ILLMConfigService llmConfigService)
        {
            _llmConfigService = llmConfigService;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// 获取所有大模型配置
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<LLMConfig>>> GetAllConfigs()
        {
            var userId = GetUserId();
            var configs = await _llmConfigService.GetAllConfigsAsync(userId);
            return Ok(configs);
        }

        /// <summary>
        /// 根据ID获取配置
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LLMConfig>> GetConfig(Guid id)
        {
            var userId = GetUserId();
            var config = await _llmConfigService.GetConfigByIdAsync(id, userId);
            if (config == null)
            {
                return NotFound();
            }
            return Ok(config);
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        [HttpGet("default")]
        public async Task<ActionResult<LLMConfig>> GetDefaultConfig()
        {
            var userId = GetUserId();
            var config = await _llmConfigService.GetDefaultConfigAsync();
            if (config == null)
            {
                return NotFound();
            }
            return Ok(config);
        }

        /// <summary>
        /// 获取供应商列表
        /// </summary>
        [HttpGet("providers")]
        [AllowAnonymous]
        public ActionResult<List<ProviderInfo>> GetProviders()
        {
            var providers = _llmConfigService.GetProviders();
            return Ok(providers);
        }

        /// <summary>
        /// 获取供应商可用模型列表
        /// </summary>
        [HttpGet("providers/{providerId}/models")]
        [AllowAnonymous]
        public async Task<ActionResult<List<string>>> GetProviderModels(string providerId)
        {
            var models = await _llmConfigService.GetProviderModelsAsync(providerId);
            return Ok(models);
        }

        /// <summary>
        /// 创建大模型配置
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LLMConfig>> CreateConfig([FromBody] LLMConfigRequest request)
        {
            var userId = GetUserId();
            var config = new LLMConfig
            {
                Name = request.Name,
                Provider = request.Provider,
                ApiKey = request.ApiKey,
                Endpoint = request.Endpoint,
                IsDefault = request.IsDefault ?? false,
                IsEnabled = request.IsEnabled ?? true,
                Models = request.Models?.Select((m, i) => new LLMModelConfig
                {
                    ModelName = m.ModelName,
                    DisplayName = m.DisplayName,
                    Temperature = m.Temperature ?? 0.7,
                    MaxTokens = m.MaxTokens ?? 4096,
                    ContextWindow = m.ContextWindow ?? 8192,
                    TopP = m.TopP,
                    FrequencyPenalty = m.FrequencyPenalty,
                    PresencePenalty = m.PresencePenalty,
                    StopSequences = m.StopSequences,
                    IsDefault = i == 0,
                    IsEnabled = true,
                    SortOrder = i
                }).ToList() ?? new List<LLMModelConfig>()
            };

            var created = await _llmConfigService.CreateConfigAsync(config, userId);
            return CreatedAtAction(nameof(GetConfig), new { id = created.Id }, created);
        }

        /// <summary>
        /// 更新大模型配置
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LLMConfig>> UpdateConfig(Guid id, [FromBody] LLMConfigRequest request)
        {
            var userId = GetUserId();
            var config = new LLMConfig
            {
                Name = request.Name,
                Provider = request.Provider,
                ApiKey = request.ApiKey,
                Endpoint = request.Endpoint,
                IsDefault = request.IsDefault ?? false,
                IsEnabled = request.IsEnabled ?? true
            };

            var updated = await _llmConfigService.UpdateConfigAsync(id, config, userId);
            if (updated == null)
            {
                return NotFound(new { message = "配置不存在或无权限修改" });
            }
            return Ok(updated);
        }

        /// <summary>
        /// 删除大模型配置
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteConfig(Guid id)
        {
            var userId = GetUserId();
            var result = await _llmConfigService.DeleteConfigAsync(id, userId);
            if (!result)
            {
                return NotFound(new { message = "配置不存在或无权限删除" });
            }
            return NoContent();
        }

        /// <summary>
        /// 设置默认配置
        /// </summary>
        [HttpPost("{id}/set-default")]
        public async Task<ActionResult> SetDefault(Guid id)
        {
            var userId = GetUserId();
            var result = await _llmConfigService.SetDefaultAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// 测试大模型连通性
        /// </summary>
        [HttpPost("{id}/test")]
        public async Task<ActionResult<object>> TestConnection(Guid id)
        {
            var (success, message, latencyMs) = await _llmConfigService.TestConnectionAsync(id);
            return Ok(new { success, message, latencyMs });
        }

        /// <summary>
        /// 测试指定模型的连通性
        /// </summary>
        [HttpPost("{id}/models/{modelId}/test")]
        public async Task<ActionResult<object>> TestModelConnection(Guid id, Guid modelId)
        {
            var (success, message, latencyMs) = await _llmConfigService.TestModelConnectionAsync(id, modelId);
            return Ok(new { success, message, latencyMs });
        }

        /// <summary>
        /// 测试所有大模型连通性
        /// </summary>
        [HttpPost("test-all")]
        public async Task<ActionResult<Dictionary<string, object>>> TestAllConnections()
        {
            var userId = GetUserId();
            var results = await _llmConfigService.TestAllConnectionsAsync();
            
            var response = new Dictionary<string, object>();
            foreach (var kvp in results)
            {
                response[kvp.Key.ToString()] = new 
                { 
                    success = kvp.Value.success, 
                    message = kvp.Value.message, 
                    latencyMs = kvp.Value.latencyMs 
                };
            }
            
            return Ok(response);
        }

        /// <summary>
        /// 添加子模型配置
        /// </summary>
        [HttpPost("{id}/models")]
        public async Task<ActionResult<LLMModelConfig>> AddModelConfig(Guid id, [FromBody] LLMModelConfigRequest request)
        {
            var modelConfig = new LLMModelConfig
            {
                ModelName = request.ModelName,
                DisplayName = request.DisplayName,
                Temperature = request.Temperature ?? 0.7,
                MaxTokens = request.MaxTokens ?? 4096,
                ContextWindow = request.ContextWindow ?? 8192,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                StopSequences = request.StopSequences,
                IsDefault = request.IsDefault ?? false,
                IsEnabled = request.IsEnabled ?? true
            };

            var created = await _llmConfigService.AddModelConfigAsync(id, modelConfig);
            return Ok(created);
        }

        /// <summary>
        /// 更新子模型配置
        /// </summary>
        [HttpPut("models/{modelId}")]
        public async Task<ActionResult<LLMModelConfig>> UpdateModelConfig(Guid modelId, [FromBody] LLMModelConfigRequest request)
        {
            var modelConfig = new LLMModelConfig
            {
                ModelName = request.ModelName,
                DisplayName = request.DisplayName,
                Temperature = request.Temperature ?? 0.7,
                MaxTokens = request.MaxTokens ?? 4096,
                ContextWindow = request.ContextWindow ?? 8192,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                StopSequences = request.StopSequences,
                IsDefault = request.IsDefault ?? false,
                IsEnabled = request.IsEnabled ?? true
            };

            var updated = await _llmConfigService.UpdateModelConfigAsync(modelId, modelConfig);
            if (updated == null)
            {
                return NotFound();
            }
            return Ok(updated);
        }

        /// <summary>
        /// 删除子模型配置
        /// </summary>
        [HttpDelete("models/{modelId}")]
        public async Task<ActionResult> DeleteModelConfig(Guid modelId)
        {
            var result = await _llmConfigService.DeleteModelConfigAsync(modelId);
            if (!result)
            {
                return BadRequest("无法删除，至少需要保留一个模型配置");
            }
            return NoContent();
        }

        /// <summary>
        /// 设置默认模型
        /// </summary>
        [HttpPost("{id}/models/{modelId}/set-default")]
        public async Task<ActionResult> SetDefaultModel(Guid id, Guid modelId)
        {
            var result = await _llmConfigService.SetDefaultModelAsync(id, modelId);
            if (!result)
            {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// 获取测试记录
        /// </summary>
        [HttpGet("{id}/test-records")]
        public async Task<ActionResult<List<LLMTestRecord>>> GetTestRecords(Guid id, [FromQuery] int limit = 10)
        {
            var records = await _llmConfigService.GetTestRecordsAsync(id, limit);
            return Ok(records);
        }

        /// <summary>
        /// 获取所有测试记录
        /// </summary>
        [HttpGet("test-records")]
        public async Task<ActionResult<List<LLMTestRecord>>> GetAllTestRecords([FromQuery] int limit = 20)
        {
            var records = await _llmConfigService.GetTestRecordsAsync(null, limit);
            return Ok(records);
        }
    }
}
