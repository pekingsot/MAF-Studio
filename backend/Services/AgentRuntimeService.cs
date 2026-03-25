using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Providers;
using MAFStudio.Backend.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 智能体运行时服务实现
    /// 负责管理智能体的生命周期、资源分配和回收
    /// 使用单例模式管理所有智能体运行时实例
    /// </summary>
    public class AgentRuntimeService : IAgentRuntimeService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentRuntimeService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _lifetime;

        /// <summary>
        /// 智能体运行时实例字典
        /// 使用线程安全的 ConcurrentDictionary 存储
        /// </summary>
        private readonly ConcurrentDictionary<Guid, AgentRuntimeInstance> _runtimeInstances = new();
        
        /// <summary>
        /// 初始化锁，防止并发初始化
        /// </summary>
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        private readonly int _idleTimeoutMinutes;
        private readonly int _sleepTimeoutMinutes;
        private readonly int _maxActiveAgents;
        private readonly TimeSpan _cleanupInterval;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AgentRuntimeService(
            IServiceProvider serviceProvider,
            ILogger<AgentRuntimeService> logger,
            IConfiguration configuration,
            IHostApplicationLifetime lifetime)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _lifetime = lifetime;

            _idleTimeoutMinutes = configuration.GetValue("AgentRuntime:IdleTimeoutMinutes", 30);
            _sleepTimeoutMinutes = configuration.GetValue("AgentRuntime:SleepTimeoutMinutes", 60);
            _maxActiveAgents = configuration.GetValue("AgentRuntime:MaxActiveAgents", 100);
            _cleanupInterval = TimeSpan.FromMinutes(configuration.GetValue("AgentRuntime:CleanupIntervalMinutes", 5));
        }

        /// <summary>
        /// 初始化智能体
        /// 创建 AIAgent 实例并建立与大模型的连接
        /// </summary>
        public async Task<AgentRuntimeInstance> InitializeAgentAsync(Guid agentId)
        {
            await _initializationLock.WaitAsync();
            try
            {
                if (_runtimeInstances.TryGetValue(agentId, out var existingInstance))
                {
                    if (existingInstance.State == AgentRuntimeState.Ready ||
                        existingInstance.State == AgentRuntimeState.Busy)
                    {
                        return existingInstance;
                    }

                    if (existingInstance.State == AgentRuntimeState.Sleeping)
                    {
                        return await ActivateAgentAsync(agentId);
                    }
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var agent = await dbContext.Agents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == agentId);

                if (agent == null)
                {
                    throw new InvalidOperationException($"智能体 {agentId} 不存在");
                }

                var llmConfig = agent.LLMConfigId.HasValue
                    ? await dbContext.LLMConfigs
                        .Include(c => c.Models)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == agent.LLMConfigId.Value)
                    : null;

                if (llmConfig == null)
                {
                    throw new InvalidOperationException($"智能体 {agentId} 未配置大模型");
                }

                _logger.LogInformation("加载 LLMConfig - Id: {Id}, Provider: {Provider}, Endpoint: {Endpoint}, ModelsCount: {Count}", 
                    llmConfig.Id, llmConfig.Provider, llmConfig.Endpoint ?? "(null)", llmConfig.Models?.Count ?? 0);
                
                if (llmConfig.Models != null)
                {
                    foreach (var model in llmConfig.Models)
                    {
                        _logger.LogInformation("  Model - Name: {Name}, DisplayName: {DisplayName}", model.ModelName, model.DisplayName);
                    }
                }

                var chatClient = CreateChatClient(llmConfig);

                var configuration = ParseConfiguration(agent.Configuration);
                var systemPrompt = configuration.SystemPrompt ?? $"你是一个{agent.Type}类型的智能体，名称是{agent.Name}。";

                var aiAgent = CreateAIAgent(chatClient, agent.Name, systemPrompt, configuration);

                var instance = new AgentRuntimeInstance
                {
                    AgentId = agentId,
                    AIAgent = aiAgent,
                    ChatClient = chatClient,
                    State = AgentRuntimeState.Ready,
                    LastActiveTime = DateTime.UtcNow,
                    InitializedTime = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Name"] = agent.Name,
                        ["Type"] = agent.Type,
                        ["LLMConfigId"] = llmConfig.Id,
                        ["Provider"] = llmConfig.Provider
                    }
                };

                _runtimeInstances[agentId] = instance;

                _logger.LogInformation("智能体 {AgentId} ({Name}) 初始化成功", agentId, agent.Name);

                await UpdateAgentStatusAsync(agentId, AgentStatus.Active);

                return instance;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// 创建聊天客户端
        /// 根据不同的供应商创建对应的客户端
        /// </summary>
        private IChatClient CreateChatClient(LLMConfig llmConfig)
        {
            var endpoint = llmConfig.Endpoint ?? GetDefaultEndpoint(llmConfig.Provider);

            return llmConfig.Provider.ToLowerInvariant() switch
            {
                "openai" => CreateOpenAIChatClient(llmConfig.ApiKey, endpoint, llmConfig),
                "deepseek" => CreateOpenAIChatClient(llmConfig.ApiKey, endpoint, llmConfig),
                "qwen" => CreateOpenAIChatClient(llmConfig.ApiKey, endpoint, llmConfig),
                "zhipu" => CreateOpenAIChatClient(llmConfig.ApiKey, endpoint, llmConfig),
                "moonshot" => CreateOpenAIChatClient(llmConfig.ApiKey, endpoint, llmConfig),
                _ => CreateOpenAIChatClient(llmConfig.ApiKey, endpoint, llmConfig)
            };
        }

        /// <summary>
        /// 获取供应商默认端点
        /// </summary>
        private string GetDefaultEndpoint(string provider)
        {
            return provider.ToLowerInvariant() switch
            {
                "openai" => "https://api.openai.com",
                "deepseek" => "https://api.deepseek.com",
                "qwen" => "https://dashscope.aliyuncs.com/compatible-mode",
                "zhipu" => "https://open.bigmodel.cn/api/paas/v4",
                "moonshot" => "https://api.moonshot.cn",
                _ => throw new NotSupportedException($"不支持的供应商: {provider}")
            };
        }

        /// <summary>
        /// 创建 OpenAI 兼容的聊天客户端
        /// </summary>
        private IChatClient CreateOpenAIChatClient(string apiKey, string endpoint, LLMConfig config)
        {
            var modelName = "gpt-4o-mini";
            if (config.Models != null && config.Models.Any())
            {
                modelName = config.Models.First().ModelName;
            }

            _logger.LogInformation("创建 ChatClient - Provider: {Provider}, Endpoint: {Endpoint}, Model: {Model}", 
                config.Provider, endpoint, modelName);

            var chatClient = new ChatClient(
                model: modelName,
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions()
                {
                    Endpoint = new Uri(endpoint)
                });

            return chatClient.AsIChatClient();
        }

        /// <summary>
        /// 创建 AI 智能体
        /// </summary>
        private AIAgent CreateAIAgent(IChatClient chatClient, string name, string systemPrompt, AgentConfiguration configuration)
        {
            return new ChatClientAgent(chatClient, systemPrompt, name);
        }

        /// <summary>
        /// 获取运行时实例
        /// </summary>
        public Task<AgentRuntimeInstance?> GetRuntimeInstanceAsync(Guid agentId)
        {
            _runtimeInstances.TryGetValue(agentId, out var instance);
            return Task.FromResult(instance);
        }

        /// <summary>
        /// 激活智能体
        /// 从休眠状态唤醒智能体
        /// </summary>
        public async Task<AgentRuntimeInstance> ActivateAgentAsync(Guid agentId)
        {
            if (!_runtimeInstances.TryGetValue(agentId, out var instance))
            {
                return await InitializeAgentAsync(agentId);
            }

            if (instance.State == AgentRuntimeState.Sleeping)
            {
                _logger.LogInformation("唤醒休眠中的智能体 {AgentId}", agentId);
                return await InitializeAgentAsync(agentId);
            }

            instance.LastActiveTime = DateTime.UtcNow;
            return instance;
        }

        /// <summary>
        /// 让智能体休眠
        /// 释放资源但保留配置
        /// </summary>
        public async Task SleepAgentAsync(Guid agentId)
        {
            if (_runtimeInstances.TryRemove(agentId, out var instance))
            {
                instance.State = AgentRuntimeState.Sleeping;
                instance.AIAgent = null;
                instance.ChatClient = null;

                _logger.LogInformation("智能体 {AgentId} 进入休眠状态", agentId);

                await UpdateAgentStatusAsync(agentId, AgentStatus.Inactive);
            }
        }

        /// <summary>
        /// 销毁智能体运行时实例
        /// 完全释放资源
        /// </summary>
        public async Task DestroyAgentAsync(Guid agentId)
        {
            if (_runtimeInstances.TryRemove(agentId, out var instance))
            {
                instance.AIAgent = null;
                instance.ChatClient = null;
                instance.State = AgentRuntimeState.Uninitialized;

                _logger.LogInformation("智能体 {AgentId} 运行时实例已销毁", agentId);

                await UpdateAgentStatusAsync(agentId, AgentStatus.Inactive);
            }
        }

        /// <summary>
        /// 执行智能体任务
        /// </summary>
        public async Task<string> ExecuteAsync(Guid agentId, string input)
        {
            var instance = await GetRuntimeInstanceAsync(agentId);

            if (instance == null || instance.AIAgent == null)
            {
                instance = await InitializeAgentAsync(agentId);
            }

            if (instance.State == AgentRuntimeState.Sleeping)
            {
                instance = await ActivateAgentAsync(agentId);
            }

            try
            {
                instance.State = AgentRuntimeState.Busy;
                instance.LastActiveTime = DateTime.UtcNow;
                instance.TaskCount++;

                var response = await instance.AIAgent!.RunAsync(input);

                instance.State = AgentRuntimeState.Ready;
                instance.LastActiveTime = DateTime.UtcNow;

                return response.Text ?? string.Empty;
            }
            catch (Exception ex)
            {
                instance.State = AgentRuntimeState.Error;
                instance.LastError = ex.Message;

                _logger.LogError(ex, "智能体 {AgentId} 执行任务失败", agentId);

                throw;
            }
        }

        /// <summary>
        /// 测试智能体连接
        /// 复用已有的 LLM Provider 架构进行连通性测试
        /// </summary>
        public async Task<(bool success, string message, int latencyMs)> TestAgentAsync(Guid agentId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var providerFactory = scope.ServiceProvider.GetRequiredService<LLMProviderFactory>();

            var agent = await dbContext.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == agentId);

            if (agent == null)
            {
                return (false, "智能体不存在", 0);
            }

            if (!agent.LLMConfigId.HasValue)
            {
                return (false, "智能体未配置大模型", 0);
            }

            var llmConfig = await dbContext.LLMConfigs
                .Include(c => c.Models)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == agent.LLMConfigId.Value);

            if (llmConfig == null)
            {
                return (false, "大模型配置不存在", 0);
            }

            var modelConfig = llmConfig.Models?.FirstOrDefault(m => m.IsDefault) ?? llmConfig.Models?.FirstOrDefault();
            if (modelConfig == null)
            {
                return (false, "未配置模型", 0);
            }

            var provider = providerFactory.GetProvider(llmConfig.Provider?.ToLower() ?? "");
            if (provider == null)
            {
                return (false, $"不支持的供应商: {llmConfig.Provider}", 0);
            }

            _logger.LogInformation("测试智能体 {AgentId} 连接 - Provider: {Provider}, Model: {Model}", 
                agentId, llmConfig.Provider, modelConfig.ModelName);

            return await provider.TestModelConnectionAsync(llmConfig, modelConfig);
        }

        /// <summary>
        /// 获取所有活跃的智能体
        /// </summary>
        public IReadOnlyDictionary<Guid, AgentRuntimeInstance> GetActiveAgents()
        {
            return _runtimeInstances;
        }

        /// <summary>
        /// 启动空闲检测服务
        /// 定期检查并清理长时间未使用的智能体
        /// </summary>
        public async Task StartIdleDetectionAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("智能体空闲检测服务已启动，检测间隔: {Interval}，空闲超时: {IdleTimeout}，休眠超时: {SleepTimeout}",
                _cleanupInterval, TimeSpan.FromMinutes(_idleTimeoutMinutes), TimeSpan.FromMinutes(_sleepTimeoutMinutes));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, cancellationToken);

                    await CleanupIdleAgentsAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "空闲检测服务发生错误");
                }
            }

            _logger.LogInformation("智能体空闲检测服务已停止");
        }

        /// <summary>
        /// 清理空闲智能体
        /// 根据配置的超时时间进行休眠或销毁
        /// </summary>
        private async Task CleanupIdleAgentsAsync()
        {
            var now = DateTime.UtcNow;
            var agentsToRemove = new List<Guid>();

            foreach (var kvp in _runtimeInstances)
            {
                var instance = kvp.Value;
                var idleTime = now - instance.LastActiveTime;

                if (instance.State == AgentRuntimeState.Ready && idleTime.TotalMinutes > _idleTimeoutMinutes)
                {
                    _logger.LogInformation("智能体 {AgentId} 空闲超过 {Minutes} 分钟，准备休眠",
                        kvp.Key, _idleTimeoutMinutes);

                    await SleepAgentAsync(kvp.Key);
                }
                else if (instance.State == AgentRuntimeState.Sleeping && idleTime.TotalMinutes > _sleepTimeoutMinutes)
                {
                    agentsToRemove.Add(kvp.Key);
                }
            }

            foreach (var agentId in agentsToRemove)
            {
                _logger.LogInformation("智能体 {AgentId} 休眠超过 {Minutes} 分钟，准备销毁",
                    agentId, _sleepTimeoutMinutes);

                await DestroyAgentAsync(agentId);
            }

            if (agentsToRemove.Count > 0)
            {
                _logger.LogInformation("本次清理共销毁 {Count} 个长时间未使用的智能体", agentsToRemove.Count);
            }
        }

        /// <summary>
        /// 获取智能体状态
        /// </summary>
        public AgentRuntimeState GetAgentState(Guid agentId)
        {
            if (_runtimeInstances.TryGetValue(agentId, out var instance))
            {
                return instance.State;
            }
            return AgentRuntimeState.Uninitialized;
        }

        /// <summary>
        /// 更新数据库中的智能体状态
        /// </summary>
        private async Task UpdateAgentStatusAsync(Guid agentId, AgentStatus status)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var agent = await dbContext.Agents.FindAsync(agentId);
                if (agent != null)
                {
                    agent.Status = status;
                    agent.LastActiveAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新智能体 {AgentId} 状态失败", agentId);
            }
        }

        /// <summary>
        /// 解析配置
        /// </summary>
        private AgentConfiguration ParseConfiguration(string configuration)
        {
            try
            {
                return JsonSerializer.Deserialize<AgentConfiguration>(configuration) ?? new AgentConfiguration();
            }
            catch
            {
                return new AgentConfiguration();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _initializationLock.Dispose();

            foreach (var instance in _runtimeInstances.Values)
            {
                instance.AIAgent = null;
                instance.ChatClient = null;
            }

            _runtimeInstances.Clear();
        }
    }

    /// <summary>
    /// 智能体配置类
    /// 包含智能体的各种配置参数
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// 系统提示词
        /// </summary>
        public string? SystemPrompt { get; set; }
        
        /// <summary>
        /// 温度参数
        /// </summary>
        public double? Temperature { get; set; }
        
        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int? MaxTokens { get; set; }
        
        /// <summary>
        /// TopP 参数
        /// </summary>
        public double? TopP { get; set; }
        
        /// <summary>
        /// 工具列表
        /// </summary>
        public List<string>? Tools { get; set; }
    }
}
