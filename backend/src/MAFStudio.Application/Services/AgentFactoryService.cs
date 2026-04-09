using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MAFStudio.Application.Services;

/// <summary>
/// Agent工厂服务
/// 使用MAF的UseFunctionInvocation()中间件处理工具调用
/// </summary>
public class AgentFactoryService : IAgentFactoryService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IChatClientFactory _chatClientFactory;
    private readonly CapabilityManager _capabilityManager;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<AgentFactoryService>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AgentFactoryService(
        IAgentRepository agentRepository,
        IChatClientFactory chatClientFactory,
        CapabilityManager capabilityManager,
        ILoggerFactory? loggerFactory = null,
        ILogger<AgentFactoryService>? logger = null)
    {
        _agentRepository = agentRepository;
        _chatClientFactory = chatClientFactory;
        _capabilityManager = capabilityManager;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<IChatClient> CreateAgentAsync(long agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        if (agent == null)
        {
            throw new NotFoundException($"Agent {agentId} not found");
        }

        if (!agent.LlmConfigId.HasValue)
        {
            throw new BusinessException("Agent缺少LLM配置");
        }

        var fallbackModels = ParseFallbackModels(agent.FallbackModels);

        _logger?.LogInformation(
            "创建Agent客户端: AgentId={AgentId}, 主模型LlmConfigId={LlmConfigId}, 副模型数量={FallbackCount}",
            agentId, agent.LlmConfigId.Value, fallbackModels?.Count ?? 0);

        var baseClient = await _chatClientFactory.CreateClientWithFallbackAsync(
            agent.LlmConfigId.Value,
            agent.LlmModelConfigId,
            fallbackModels);

        return BuildClientWithCapabilities(baseClient);
    }

    public async Task<IChatClient> CreateChatClientAsync(long llmConfigId, long llmModelConfigId)
    {
        var baseClient = await _chatClientFactory.CreateClientAsync(llmConfigId, llmModelConfigId);
        
        return BuildClientWithCapabilities(baseClient);
    }

    public async Task<IChatClient> CreateManagerClientAsync()
    {
        _logger?.LogInformation("创建Magentic Manager客户端");
        
        var defaultLlmConfig = await GetDefaultLlmConfigAsync();
        
        var baseClient = await _chatClientFactory.CreateClientAsync(
            defaultLlmConfig.LlmConfigId,
            defaultLlmConfig.LlmModelConfigId);

        var builder = new ChatClientBuilder(baseClient);
        
        builder.UseFunctionInvocation(_loggerFactory, configure: options =>
        {
            options.MaximumIterationsPerRequest = 5;
        });

        return builder.Build();
    }

    private async Task<(long LlmConfigId, long? LlmModelConfigId)> GetDefaultLlmConfigAsync()
    {
        var agents = await _agentRepository.GetAllAsync();
        var firstAgent = agents.FirstOrDefault(a => a.LlmConfigId.HasValue);
        
        if (firstAgent != null && firstAgent.LlmConfigId.HasValue)
        {
            return (firstAgent.LlmConfigId.Value, firstAgent.LlmModelConfigId);
        }

        throw new BusinessException("未找到可用的LLM配置，请先创建Agent");
    }

    /// <summary>
    /// 使用MAF中间件构建带工具调用能力的客户端
    /// 管道顺序: CapabilitiesChatClient(注入工具) -> UseFunctionInvocation(处理工具调用) -> baseClient
    /// </summary>
    private IChatClient BuildClientWithCapabilities(IChatClient baseClient)
    {
        var builder = new ChatClientBuilder(baseClient);
        
        builder.UseFunctionInvocation(_loggerFactory, configure: options =>
        {
            options.MaximumIterationsPerRequest = 10;
        });

        var clientWithFunctionInvocation = builder.Build();
        
        var capabilitiesLogger = _loggerFactory?.CreateLogger<CapabilitiesChatClient>();
        return new CapabilitiesChatClient(clientWithFunctionInvocation, _capabilityManager, capabilitiesLogger);
    }

    private List<FallbackModelConfig>? ParseFallbackModels(string? fallbackModelsJson)
    {
        if (string.IsNullOrEmpty(fallbackModelsJson))
        {
            return null;
        }

        try
        {
            var vos = JsonSerializer.Deserialize<List<FallbackModelVo>>(fallbackModelsJson, JsonOptions);
            if (vos == null || vos.Count == 0)
            {
                return null;
            }

            return vos.Select(v => new FallbackModelConfig
            {
                LlmConfigId = v.LlmConfigId,
                LlmModelConfigId = v.LlmModelConfigId,
                Priority = v.Priority
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "解析FallbackModels失败: {Json}", fallbackModelsJson);
            return null;
        }
    }
}

internal class FallbackModelVo
{
    public long LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public int Priority { get; set; }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
