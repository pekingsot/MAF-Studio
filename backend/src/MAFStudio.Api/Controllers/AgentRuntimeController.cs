using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Entities;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentRuntimeController : ControllerBase
{
    private readonly IAgentFactoryService _agentFactory;
    private readonly IAgentRepository _agentRepository;
    private readonly ILlmConfigRepository _llmConfigRepository;
    private readonly ILlmModelConfigRepository _llmModelConfigRepository;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ILogger<AgentRuntimeController> _logger;

    public AgentRuntimeController(
        IAgentFactoryService agentFactory,
        IAgentRepository agentRepository,
        ILlmConfigRepository llmConfigRepository,
        ILlmModelConfigRepository llmModelConfigRepository,
        IChatClientFactory chatClientFactory,
        ILogger<AgentRuntimeController> logger)
    {
        _agentFactory = agentFactory;
        _agentRepository = agentRepository;
        _llmConfigRepository = llmConfigRepository;
        _llmModelConfigRepository = llmModelConfigRepository;
        _chatClientFactory = chatClientFactory;
        _logger = logger;
    }

    [HttpGet("{agentId}/status")]
    public async Task<ActionResult<AgentRuntimeStatus>> GetStatus(long agentId)
    {
        try
        {
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null)
            {
                return NotFound(new { message = $"Agent {agentId} not found" });
            }

            var state = agent.Status switch
            {
                AgentStatus.Inactive => "Uninitialized",
                AgentStatus.Active => "Ready",
                AgentStatus.Busy => "Busy",
                AgentStatus.Error => "Error",
                _ => "Uninitialized"
            };

            var status = new AgentRuntimeStatus 
            { 
                AgentId = agentId, 
                State = state, 
                IsAlive = agent.Status == AgentStatus.Active,
                LastActiveTime = agent.UpdatedAt,
                TaskCount = 0
            };
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取智能体 {AgentId} 状态失败", agentId);
            return Ok(new AgentRuntimeStatus 
            { 
                AgentId = agentId, 
                State = "Uninitialized", 
                IsAlive = false,
                TaskCount = 0
            });
        }
    }

    [HttpPost("{agentId}/activate")]
    public async Task<ActionResult<AgentActivateResponse>> Activate(long agentId)
    {
        try
        {
            _logger.LogInformation("激活智能体 {AgentId}（测试所有大模型连通性）", agentId);
            
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null)
            {
                return NotFound(new { message = $"Agent {agentId} not found" });
            }

            if (string.IsNullOrEmpty(agent.LlmConfigs))
            {
                return BadRequest(new { message = "智能体未配置大模型" });
            }

            var llmConfigs = JsonSerializer.Deserialize<List<LlmConfigInfo>>(agent.LlmConfigs, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            if (llmConfigs == null || llmConfigs.Count == 0)
            {
                return BadRequest(new { message = "智能体未配置大模型" });
            }

            var testResults = new System.Collections.Concurrent.ConcurrentBag<ModelTestResult>();

            await Parallel.ForEachAsync(llmConfigs, async (config, cancellationToken) =>
            {
                var result = await TestModelConnectivityAsync(config.LlmConfigId, config.LlmModelConfigId ?? 0);
                
                config.IsValid = result.Success;
                config.Msg = result.Msg;
                config.LastChecked = DateTime.UtcNow;

                await _llmModelConfigRepository.UpdateTestStatusAsync(
                    config.LlmModelConfigId ?? 0, 
                    result.Success, 
                    result.Msg);

                testResults.Add(result);
            });

            var anySuccess = testResults.Any(r => r.Success);
            agent.LlmConfigs = JsonSerializer.Serialize(llmConfigs, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            agent.Status = anySuccess ? AgentStatus.Active : AgentStatus.Error;
            await _agentRepository.UpdateAsync(agent);

            _logger.LogInformation("智能体 {AgentId} 激活完成，成功 {Success}/{Total}", 
                agentId, testResults.Count(r => r.Success), testResults.Count);

            return Ok(new AgentActivateResponse
            {
                AgentId = agentId,
                State = anySuccess ? "Ready" : "Error",
                IsAlive = anySuccess,
                LastActiveTime = DateTime.UtcNow,
                TestResults = testResults.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活智能体 {AgentId} 失败", agentId);
            
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent != null)
            {
                agent.Status = AgentStatus.Error;
                await _agentRepository.UpdateAsync(agent);
            }
            
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ModelTestResult> TestModelConnectivityAsync(long llmConfigId, long llmModelConfigId)
    {
        var result = new ModelTestResult
        {
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId
        };

        try
        {
            var llmConfig = await _llmConfigRepository.GetByIdAsync(llmConfigId);
            if (llmConfig == null)
            {
                result.Success = false;
                result.Msg = "LLM配置不存在";
                return result;
            }

            if (string.IsNullOrEmpty(llmConfig.ApiKey))
            {
                result.Success = false;
                result.Msg = "API Key未配置";
                return result;
            }

            LlmModelConfig? modelConfig = null;
            if (llmModelConfigId > 0)
            {
                modelConfig = await _llmModelConfigRepository.GetByIdAsync(llmModelConfigId);
            }
            modelConfig ??= (await _llmModelConfigRepository.GetByLlmConfigIdAsync(llmConfigId))
                .FirstOrDefault(m => m.IsDefault) 
                ?? (await _llmModelConfigRepository.GetByLlmConfigIdAsync(llmConfigId)).FirstOrDefault();

            if (modelConfig == null)
            {
                result.Success = false;
                result.Msg = "模型配置不存在";
                return result;
            }

            result.ModelName = modelConfig.ModelName;

            using var client = _chatClientFactory.CreateClient(
                llmConfig.Provider,
                llmConfig.ApiKey,
                llmConfig.Endpoint,
                modelConfig.ModelName);

            var testMessage = new ChatMessage(ChatRole.User, "Hi");
            var options = new ChatOptions { MaxOutputTokens = 10 };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            await foreach (var update in client.GetStreamingResponseAsync(new[] { testMessage }, options))
            {
                stopwatch.Stop();
                break;
            }
            
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            result.Success = true;
            result.Msg = $"{stopwatch.ElapsedMilliseconds}ms";
            result.LatencyMs = (int)stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试模型连通性失败: LlmConfigId={LlmConfigId}, ModelConfigId={ModelConfigId}", 
                llmConfigId, llmModelConfigId);
            result.Success = false;
            result.Msg = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
        }

        return result;
    }

    [HttpPost("{agentId}/test")]
    public async Task<ActionResult<AgentTestResponse>> Test(long agentId, [FromBody] TestRequest? request = null)
    {
        try
        {
            _logger.LogInformation("测试智能体 {AgentId}", agentId);
            
            using var agent = request?.LlmConfigId.HasValue == true && request.LlmModelConfigId.HasValue
                ? await _agentFactory.CreateChatClientAsync(request.LlmConfigId.Value, request.LlmModelConfigId.Value)
                : await _agentFactory.CreateAgentAsync(agentId);

            var input = request?.Input ?? "你好，请简单介绍一下你自己。";
            var startTime = DateTime.UtcNow;
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };
            
            var responseText = new System.Text.StringBuilder();
            await foreach (var update in agent.GetStreamingResponseAsync(messages))
            {
                if (update.Text != null)
                {
                    responseText.Append(update.Text);
                }
            }
            
            var endTime = DateTime.UtcNow;
            var latencyMs = (int)(endTime - startTime).TotalMilliseconds;
            
            _logger.LogInformation("智能体 {AgentId} 测试成功，耗时 {LatencyMs}ms", agentId, latencyMs);
            
            return Ok(new AgentTestResponse
            {
                Success = true,
                Message = "测试成功",
                Response = responseText.ToString(),
                LatencyMs = latencyMs,
                State = "Ready"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试智能体 {AgentId} 失败", agentId);
            return Ok(new AgentTestResponse
            {
                Success = false,
                Message = ex.Message,
                LatencyMs = 0,
                State = "Error"
            });
        }
    }
}

public class AgentRuntimeStatus
{
    public long AgentId { get; set; }
    public string State { get; set; } = "Uninitialized";
    public DateTime? LastActiveTime { get; set; }
    public int TaskCount { get; set; }
    public string? LastError { get; set; }
    public bool IsAlive { get; set; }
}

public class AgentActivateResponse : AgentRuntimeStatus
{
    public List<ModelTestResult> TestResults { get; set; } = new();
}

public class ModelTestResult
{
    public long LlmConfigId { get; set; }
    public long LlmModelConfigId { get; set; }
    public string? ModelName { get; set; }
    public bool Success { get; set; }
    public string Msg { get; set; } = string.Empty;
    public int LatencyMs { get; set; }
}

public class AgentTestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Response { get; set; }
    public int LatencyMs { get; set; }
    public string? State { get; set; }
}

public class TestRequest
{
    public string? Input { get; set; }
    public long? LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
}

public class LlmConfigInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("llmConfigId")]
    public long LlmConfigId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("llmConfigName")]
    public string LlmConfigName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("llmModelConfigId")]
    public long? LlmModelConfigId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("modelName")]
    public string ModelName { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("isValid")]
    public bool IsValid { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("lastChecked")]
    public DateTime LastChecked { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;
}
