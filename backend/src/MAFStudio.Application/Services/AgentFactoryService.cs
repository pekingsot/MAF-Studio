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
        var (baseClient, _) = await CreateBaseClientAsync(agentId);
        return BuildClientWithCapabilities(baseClient);
    }

    public async Task<IChatClient> CreateAgentWithoutCapabilitiesAsync(long agentId)
    {
        var (baseClient, _) = await CreateBaseClientAsync(agentId);
        return baseClient;
    }

    public async Task<IChatClient> CreateChatClientAsync(long llmConfigId, long llmModelConfigId)
    {
        var baseClient = await _chatClientFactory.CreateClientAsync(llmConfigId, llmModelConfigId);
        return BuildClientWithCapabilities(baseClient);
    }

    public async Task<IChatClient> CreateManagerClientAsync()
    {
        _logger?.LogInformation("创建Manager客户端");
        var (llmConfigId, llmModelConfigId) = await GetDefaultLlmConfigAsync();
        var baseClient = await _chatClientFactory.CreateClientAsync(llmConfigId, llmModelConfigId);

        var builder = new ChatClientBuilder(baseClient);
        builder.UseFunctionInvocation(_loggerFactory, configure: options =>
        {
            options.MaximumIterationsPerRequest = 5;
        });

        return builder.Build();
    }

    private async Task<(IChatClient Client, Agent AgentEntity)> CreateBaseClientAsync(long agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId)
            ?? throw new NotFoundException($"Agent {agentId} not found");

        var (llmConfigId, llmModelConfigId, fallbackModels) = ParseLlmConfig(agent);

        var baseClient = await _chatClientFactory.CreateClientWithFallbackAsync(
            llmConfigId, llmModelConfigId, fallbackModels);

        return (baseClient, agent);
    }

    private (long LlmConfigId, long? LlmModelConfigId, List<FallbackModelConfig>? FallbackModels) ParseLlmConfig(Agent agent)
    {
        if (string.IsNullOrEmpty(agent.LlmConfigs))
        {
            throw new BusinessException("Agent缺少LLM配置，请先配置大模型");
        }

        var llmConfigs = JsonSerializer.Deserialize<List<LlmConfigVo>>(agent.LlmConfigs, JsonOptions);
        if (llmConfigs == null || llmConfigs.Count == 0)
        {
            throw new BusinessException("Agent的LlmConfigs配置无效");
        }

        var primaryModel = llmConfigs.FirstOrDefault(c => c.IsPrimary) ?? llmConfigs.First();

        var fallbackModels = llmConfigs
            .Where(c => !c.IsPrimary)
            .OrderBy(c => c.Priority)
            .Select(c => new FallbackModelConfig
            {
                LlmConfigId = c.LlmConfigId,
                LlmModelConfigId = c.LlmModelConfigId,
                Priority = c.Priority
            })
            .ToList();

        _logger?.LogInformation(
            "解析LLM配置: AgentId={AgentId}, 主模型={LlmConfigId}/{ModelConfigId}, 副模型={FallbackCount}",
            agent.Id, primaryModel.LlmConfigId, primaryModel.LlmModelConfigId,
            fallbackModels is { Count: > 0 } ? fallbackModels.Count : 0);

        return (primaryModel.LlmConfigId, primaryModel.LlmModelConfigId,
            fallbackModels is { Count: > 0 } ? fallbackModels : null);
    }

    private IChatClient BuildClientWithCapabilities(IChatClient baseClient)
    {
        var builder = new ChatClientBuilder(baseClient);

        builder.UseFunctionInvocation(_loggerFactory, configure: options =>
        {
            options.MaximumIterationsPerRequest = 10;
            options.IncludeDetailedErrors = true;
        });

        var clientWithFunctionInvocation = builder.Build();

        var capabilitiesLogger = _loggerFactory?.CreateLogger<CapabilitiesChatClient>();
        return new CapabilitiesChatClient(clientWithFunctionInvocation, _capabilityManager, capabilitiesLogger);
    }

    private async Task<(long LlmConfigId, long? LlmModelConfigId)> GetDefaultLlmConfigAsync()
    {
        var agents = await _agentRepository.GetAllAsync();
        var firstAgent = agents.FirstOrDefault(a => !string.IsNullOrEmpty(a.LlmConfigs));

        if (firstAgent != null && !string.IsNullOrEmpty(firstAgent.LlmConfigs))
        {
            var llmConfigs = JsonSerializer.Deserialize<List<LlmConfigVo>>(firstAgent.LlmConfigs, JsonOptions);
            if (llmConfigs != null && llmConfigs.Count > 0)
            {
                var primaryModel = llmConfigs.FirstOrDefault(c => c.IsPrimary) ?? llmConfigs.First();
                return (primaryModel.LlmConfigId, primaryModel.LlmModelConfigId);
            }
        }

        throw new BusinessException("未找到可用的LLM配置，请先创建Agent");
    }
}

internal class FallbackModelVo
{
    public long LlmConfigId { get; set; }
    public long? LlmModelConfigId { get; set; }
    public int Priority { get; set; }
}

internal class LlmConfigVo
{
    public long LlmConfigId { get; set; }
    public string LlmConfigName { get; set; } = string.Empty;
    public long? LlmModelConfigId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int Priority { get; set; }
    public bool IsValid { get; set; }
    public string Msg { get; set; } = string.Empty;
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
