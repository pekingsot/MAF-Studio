using Moq;
using Dapper;
using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Application.Services;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

/// <summary>
/// MAF中间件迁移测试
/// 测试UseFunctionInvocation中间件替代ToolCallingChatClient
/// </summary>
public class MafMiddlewareMigrationTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_maf_middleware_log.txt");
    private readonly string _connectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123";
    private const long TestTaskId = 1006;

    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }

    private IDapperContext CreateDapperContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            })
            .Build();

        return new DapperContext(configuration);
    }

    [Fact]
    public async Task MafMiddleware_CapabilitiesChatClient_ShouldRegisterAllTools()
    {
        File.Delete(_logFile);
        Log("========== 测试: CapabilitiesChatClient 工具注册 ==========");
        Log($"测试时间: {DateTime.Now}");

        var capabilityManager = new CapabilityManager(CreateMockServiceProvider());
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var mockChatClient = new MockChatClient();
        var capabilitiesLogger = loggerFactory.CreateLogger<CapabilitiesChatClient>();
        
        var capabilitiesClient = new CapabilitiesChatClient(mockChatClient, capabilityManager, capabilitiesLogger);
        
        var tools = capabilitiesClient.GetTools();
        
        Log($"\n注册的工具数量: {tools.Count}");
        Assert.True(tools.Count >= 50, $"应该有至少50个工具，实际: {tools.Count}");
        
        foreach (var tool in tools.Take(10))
        {
            Log($"  - 工具类型: {tool.GetType().Name}");
        }
        
        Log("\n✓ CapabilitiesChatClient 工具注册测试通过");
    }

    [Fact]
    public async Task MafMiddleware_Task1006_CreateAgentWithFallback()
    {
        Log("\n========== 测试: 任务1006 Agent创建（带故障转移） ==========");
        Log($"测试时间: {DateTime.Now}");

        var dapperContext = CreateDapperContext();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var task = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, title, description FROM collaboration_tasks WHERE id = @Id",
            new { Id = TestTaskId });

        Assert.NotNull(task);
        Log($"任务: {task.title}");

        var agents = await connection.QueryAsync<dynamic>(
            @"SELECT a.id, a.name, a.type_name, a.llm_configs 
              FROM collaboration_agents ca 
              JOIN agents a ON ca.agent_id = a.id 
              WHERE ca.collaboration_id = (SELECT collaboration_id FROM collaboration_tasks WHERE id = @TaskId)",
            new { TaskId = TestTaskId });

        var agentList = agents.ToList();
        if (agentList.Count == 0)
        {
            var collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
                "SELECT collaboration_id FROM collaboration_tasks WHERE id = @Id",
                new { Id = TestTaskId });

            if (collaborationId > 0)
            {
                agents = await connection.QueryAsync<dynamic>(
                    @"SELECT a.id, a.name, a.type_name, a.llm_configs 
                      FROM collaboration_agents ca 
                      JOIN agents a ON ca.agent_id = a.id 
                      WHERE ca.collaboration_id = @CollaborationId",
                    new { CollaborationId = collaborationId });
                agentList = agents.ToList();
            }
        }

        Assert.True(agentList.Count > 0, "任务必须关联至少一个Agent");
        Log($"可用Agent: {string.Join(", ", agentList.Select(a => a.name))}");

        var capabilityManager = new CapabilityManager(CreateMockServiceProvider());
        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();
        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);

        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            loggerFactory);

        var firstAgent = agentList.First();
        Log($"\n创建Agent: {firstAgent.name}");

        var chatClient = await agentFactory.CreateAgentAsync((long)firstAgent.id);
        Assert.NotNull(chatClient);
        Log($"✓ ChatClient 创建成功: {chatClient.GetType().Name}");

        Log("\n✓ 任务1006 Agent创建测试通过");
    }

    [Fact]
    public async Task MafMiddleware_Task1006_SimpleToolCall()
    {
        Log("\n========== 测试: 任务1006 简单工具调用 ==========");
        Log($"测试时间: {DateTime.Now}");

        var dapperContext = CreateDapperContext();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var agents = await connection.QueryAsync<dynamic>(
            @"SELECT a.id, a.name, a.llm_configs 
              FROM collaboration_agents ca 
              JOIN agents a ON ca.agent_id = a.id 
              WHERE ca.collaboration_id = (SELECT collaboration_id FROM collaboration_tasks WHERE id = @TaskId)",
            new { TaskId = TestTaskId });

        var agentList = agents.ToList();
        if (agentList.Count == 0)
        {
            var collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
                "SELECT collaboration_id FROM collaboration_tasks WHERE id = @Id",
                new { Id = TestTaskId });

            if (collaborationId > 0)
            {
                agents = await connection.QueryAsync<dynamic>(
                    @"SELECT a.id, a.name, a.llm_configs 
                      FROM collaboration_agents ca 
                      JOIN agents a ON ca.agent_id = a.id 
                      WHERE ca.collaboration_id = @CollaborationId",
                    new { CollaborationId = collaborationId });
                agentList = agents.ToList();
            }
        }

        Assert.True(agentList.Count > 0, "任务必须关联至少一个Agent");

        var capabilityManager = new CapabilityManager(CreateMockServiceProvider());
        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();
        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);

        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            loggerFactory);

        var firstAgent = agentList.First();
        var chatClient = await agentFactory.CreateAgentAsync((long)firstAgent.id);

        var testDir = Path.Combine(Path.GetTempPath(), "maf_middleware_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        Log($"测试目录: {testDir}");

        try
        {
            var prompt = $@"请帮我完成以下任务：
1. 在目录 {testDir} 创建一个名为 'test.txt' 的文件
2. 文件内容写上 'Hello from MAF Middleware! 测试时间: {DateTime.Now}'
3. 读取文件内容并告诉我文件是否创建成功

请使用可用的工具来完成这些操作。";

            Log($"\n发送测试提示词...");
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, prompt)
            };

            var response = await chatClient.GetResponseAsync(messages);
            Assert.NotNull(response);
            Assert.NotEmpty(response.Messages);

            var responseText = response.Messages.LastOrDefault()?.Text ?? "";
            Log($"\n响应长度: {responseText.Length} 字符");
            Log($"响应预览: {(responseText.Length > 200 ? responseText[..200] + "..." : responseText)}");

            Assert.True(responseText.Length > 10, "响应内容应该有意义");

            Log("\n✓ 任务1006 简单工具调用测试通过");
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var mockTaskContext = new Mock<ITaskContextService>();
        var mockSp = new Mock<IServiceProvider>();
        mockSp.Setup(x => x.GetService(typeof(ITaskContextService)))
            .Returns(mockTaskContext.Object);
        return mockSp.Object;
    }
}

/// <summary>
/// 模拟ChatClient用于测试
/// </summary>
internal class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("mock", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var toolsCount = options?.Tools?.Count ?? 0;
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Mock response. Tools available: {toolsCount}"));
        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var toolsCount = options?.Tools?.Count ?? 0;
        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, $"Mock streaming response. Tools available: {toolsCount}")
        };
        return updates.ToAsyncEnumerable();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose() { }
}

internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
    }
}
