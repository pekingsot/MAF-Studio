using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Enums;
using Microsoft.Extensions.AI;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentRuntimeController : ControllerBase
{
    private readonly IAgentFactoryService _agentFactory;
    private readonly IAgentRepository _agentRepository;
    private readonly ILogger<AgentRuntimeController> _logger;

    public AgentRuntimeController(
        IAgentFactoryService agentFactory,
        IAgentRepository agentRepository,
        ILogger<AgentRuntimeController> logger)
    {
        _agentFactory = agentFactory;
        _agentRepository = agentRepository;
        _logger = logger;
    }

    [HttpGet("{agentId}/status")]
    public async Task<ActionResult<AgentRuntimeStatus>> GetStatus(long agentId)
    {
        try
        {
            // 从数据库获取Agent的实际状态
            var agent = await _agentRepository.GetByIdAsync(agentId);
            if (agent == null)
            {
                return NotFound(new { message = $"Agent {agentId} not found" });
            }

            // 将数据库状态映射为运行时状态
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
    public async Task<ActionResult<AgentRuntimeStatus>> Activate(long agentId)
    {
        try
        {
            _logger.LogInformation("激活智能体 {AgentId}（测试连通性）", agentId);
            
            // 使用using语句，测试完自动释放资源
            using var agent = await _agentFactory.CreateAgentAsync(agentId);
            
            // 测试连通性：发送简单的测试消息
            var testMessage = new List<ChatMessage>
            {
                new(ChatRole.User, "你好")
            };
            
            var startTime = DateTime.UtcNow;
            // 使用流式调用（某些LLM提供商只支持流式调用）
            await foreach (var update in agent.GetStreamingResponseAsync(testMessage))
            {
                // 只需要确认能收到响应即可，不需要处理内容
                break; // 收到第一个更新就退出，只需要测试连通性
            }
            var endTime = DateTime.UtcNow;
            var latencyMs = (int)(endTime - startTime).TotalMilliseconds;
            
            _logger.LogInformation("智能体 {AgentId} 激活成功（连通性测试通过），耗时 {LatencyMs}ms", agentId, latencyMs);
            
            // 更新Agent状态为Active
            await _agentRepository.UpdateStatusAsync(agentId, AgentStatus.Active);
            
            // 返回成功状态（不保存Agent实例）
            var status = new AgentRuntimeStatus
            {
                AgentId = agentId,
                State = "Ready",
                IsAlive = false,  // 不保持活跃，测试完就释放
                LastActiveTime = DateTime.UtcNow,
                TaskCount = 0
            };
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活智能体 {AgentId} 失败", agentId);
            
            // 更新Agent状态为Error
            await _agentRepository.UpdateStatusAsync(agentId, AgentStatus.Error);
            
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{agentId}/test")]
    public async Task<ActionResult<AgentTestResponse>> Test(long agentId, [FromBody] TestRequest? request = null)
    {
        try
        {
            _logger.LogInformation("测试智能体 {AgentId}", agentId);
            
            // 如果指定了模型配置，使用指定的模型；否则使用Agent的主模型
            using var agent = request?.LlmConfigId.HasValue == true && request.LlmModelConfigId.HasValue
                ? await _agentFactory.CreateChatClientAsync(request.LlmConfigId.Value, request.LlmModelConfigId.Value)
                : await _agentFactory.CreateAgentAsync(agentId);

            var input = request?.Input ?? "你好，请简单介绍一下你自己。";
            var startTime = DateTime.UtcNow;
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, input)
            };
            
            // 使用流式调用（某些LLM提供商只支持流式调用）
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
    
    /// <summary>
    /// 指定使用的LLM配置ID（可选，不指定则使用主模型）
    /// </summary>
    public long? LlmConfigId { get; set; }
    
    /// <summary>
    /// 指定使用的模型配置ID（可选，不指定则使用主模型）
    /// </summary>
    public long? LlmModelConfigId { get; set; }
}
