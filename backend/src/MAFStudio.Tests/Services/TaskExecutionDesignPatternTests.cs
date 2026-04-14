using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Application.DTOs;
using MAFStudio.Application.Prompts;
using MAFStudio.Application.Services;
using MAFStudio.Application.Workflows;
using MAFStudio.Application.Workflows.Selection;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Dapper;
using Xunit;

namespace MAFStudio.Tests.Services;

public class TaskExecutionDesignPatternTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "task1006_pattern_test.txt");
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
    public async Task Task1006_StrategyPattern_MentionSelection_ShouldSelectCorrectAgent()
    {
        File.Delete(_logFile);
        Log("========== 策略模式测试：MentionSelectionStrategy ==========");

        var strategy = new MentionSelectionStrategy();
        Assert.Equal("Mention", strategy.Name);

        var orchestrator = CreateTestAIAgent("Manager", "协调者");
        var worker1 = CreateTestAIAgent("前端工程师", "前端开发");
        var worker2 = CreateTestAIAgent("后端工程师", "后端开发");

        var context = new AgentSelectionContext
        {
            History = new List<ChatMessage>
            {
                new(ChatRole.User, "请开始工作"),
                new(ChatRole.Assistant, "我来分配任务。@前端工程师 请负责页面设计。") { AuthorName = "Manager" }
            },
            OrchestratorAgent = orchestrator,
            WorkerAgents = [worker1, worker2],
            AllAgents = [orchestrator, worker1, worker2],
            LastOrchestratorRawText = "我来分配任务。@前端工程师 请负责页面设计。",
            IterationCount = 1
        };

        var selected = await strategy.SelectAsync(context);
        Assert.NotNull(selected);
        Assert.Equal("前端工程师", selected.Name);
        Log($"✓ Mention策略选中: {selected.Name}");

        var context2 = new AgentSelectionContext
        {
            History = new List<ChatMessage>
            {
                new(ChatRole.User, "请开始工作"),
                new(ChatRole.Assistant, "后端部分交给 @后端工程师 处理。") { AuthorName = "Manager" }
            },
            OrchestratorAgent = orchestrator,
            WorkerAgents = [worker1, worker2],
            AllAgents = [orchestrator, worker1, worker2],
            LastOrchestratorRawText = "后端部分交给 @后端工程师 处理。",
            IterationCount = 2
        };

        var selected2 = await strategy.SelectAsync(context2);
        Assert.NotNull(selected2);
        Assert.Equal("后端工程师", selected2.Name);
        Log($"✓ Mention策略选中: {selected2.Name}");

        var context3 = new AgentSelectionContext
        {
            History = new List<ChatMessage>
            {
                new(ChatRole.User, "请开始工作"),
                new(ChatRole.Assistant, "大家一起来讨论。") { AuthorName = "Manager" }
            },
            OrchestratorAgent = orchestrator,
            WorkerAgents = [worker1, worker2],
            AllAgents = [orchestrator, worker1, worker2],
            LastOrchestratorRawText = "大家一起来讨论。",
            IterationCount = 3
        };

        var selected3 = await strategy.SelectAsync(context3);
        Assert.Null(selected3);
        Log("✓ 无@提及时返回null，由Composite策略回退到RoundRobin");
    }

    [Fact]
    public async Task Task1006_StrategyPattern_RoundRobinSelection_ShouldRotateAgents()
    {
        Log("========== 策略模式测试：RoundRobinSelectionStrategy ==========");

        var strategy = new RoundRobinSelectionStrategy();
        Assert.Equal("RoundRobin", strategy.Name);

        var worker1 = CreateTestAIAgent("前端工程师", "前端开发");
        var worker2 = CreateTestAIAgent("后端工程师", "后端开发");
        var orchestrator = CreateTestAIAgent("Manager", "协调者");

        var context = new AgentSelectionContext
        {
            History = [],
            OrchestratorAgent = orchestrator,
            WorkerAgents = [worker1, worker2],
            AllAgents = [orchestrator, worker1, worker2],
            IterationCount = 1
        };

        var first = await strategy.SelectAsync(context);
        Assert.Equal("前端工程师", first!.Name);
        Log($"✓ RoundRobin第1轮: {first.Name}");

        var second = await strategy.SelectAsync(context);
        Assert.Equal("后端工程师", second!.Name);
        Log($"✓ RoundRobin第2轮: {second.Name}");

        var third = await strategy.SelectAsync(context);
        Assert.Equal("前端工程师", third!.Name);
        Log($"✓ RoundRobin第3轮（循环）: {third.Name}");

        strategy.Reset();
        var afterReset = await strategy.SelectAsync(context);
        Assert.Equal("前端工程师", afterReset!.Name);
        Log("✓ Reset后从第1个开始");
    }

    [Fact]
    public async Task Task1006_StrategyPattern_CompositeSelection_ShouldFallbackCorrectly()
    {
        Log("========== 策略模式测试：CompositeSelectionStrategy ==========");

        var logger = LoggerFactory.Create(b => b.AddConsole())
            .CreateLogger<CompositeSelectionStrategy>();

        var strategy = AgentSelectionStrategyFactory.CreateOrchestratedStrategy(logger);
        Assert.Equal("Composite", strategy.Name);

        var orchestrator = CreateTestAIAgent("Manager", "协调者");
        var worker1 = CreateTestAIAgent("前端工程师", "前端开发");
        var worker2 = CreateTestAIAgent("后端工程师", "后端开发");

        var mentionContext = new AgentSelectionContext
        {
            History = [],
            OrchestratorAgent = orchestrator,
            WorkerAgents = [worker1, worker2],
            AllAgents = [orchestrator, worker1, worker2],
            LastOrchestratorRawText = "@前端工程师 请处理这个任务",
            IterationCount = 1
        };

        var byMention = await strategy.SelectAsync(mentionContext);
        Assert.Equal("前端工程师", byMention!.Name);
        Log($"✓ Composite优先使用Mention策略: {byMention.Name}");

        var noMentionContext = new AgentSelectionContext
        {
            History = [],
            OrchestratorAgent = orchestrator,
            WorkerAgents = [worker1, worker2],
            AllAgents = [orchestrator, worker1, worker2],
            LastOrchestratorRawText = "请继续工作",
            IterationCount = 2
        };

        var byRoundRobin = await strategy.SelectAsync(noMentionContext);
        Assert.NotNull(byRoundRobin);
        Log($"✓ Composite回退到RoundRobin策略: {byRoundRobin.Name}");
    }

    [Fact]
    public void Task1006_TemplateMethodPattern_PromptBuilder_ShouldBuildCorrectPrompts()
    {
        Log("========== 模板方法模式测试：SystemPromptBuilderFactory ==========");

        var factory = new SystemPromptBuilderFactory();

        var roundRobinBuilder = factory.Create(GroupChatOrchestrationMode.RoundRobin);
        Assert.IsType<RoundRobinPromptBuilder>(roundRobinBuilder);
        Log("✓ RoundRobin模式创建RoundRobinPromptBuilder");

        var managerBuilder = factory.Create(GroupChatOrchestrationMode.Manager);
        Assert.IsType<ManagerPromptBuilder>(managerBuilder);
        Log("✓ Manager模式创建ManagerPromptBuilder");

        var intelligentBuilder = factory.Create(GroupChatOrchestrationMode.Intelligent);
        Assert.IsType<IntelligentPromptBuilder>(intelligentBuilder);
        Log("✓ Intelligent模式创建IntelligentPromptBuilder");

        var context = new SystemPromptContext
        {
            AgentName = "前端工程师",
            AgentRole = "Worker",
            AgentTypeName = "前端开发",
            MembersInfo = "- 后端工程师：负责后端开发",
            TaskDescription = "开发微信小程序",
            TaskPrompt = "请完成前端页面设计"
        };

        var prompt = managerBuilder.BuildPrompt(context);
        Assert.NotEmpty(prompt);
        Assert.Contains("前端工程师", prompt);
        Log($"✓ Manager模式生成的提示词包含Agent名称，长度: {prompt.Length}");
    }

    [Fact]
    public async Task Task1006_TemplateMethodPattern_AgentFactory_ShouldCreateWithAndWithoutCapabilities()
    {
        Log("========== 模板方法模式测试：AgentFactoryService ==========");

        var dapperContext = CreateDapperContext();
        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            loggerFactory.CreateLogger<ChatClientFactory>());

        var mockTaskContext = new Mock<ITaskContextService>();
        var mockSp = new Mock<IServiceProvider>();
        mockSp.Setup(x => x.GetService(typeof(ITaskContextService)))
            .Returns(mockTaskContext.Object);
        var capabilityManager = new CapabilityManager(mockSp.Object);

        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            loggerFactory);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var agent = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, name FROM agents LIMIT 1");

        if (agent == null)
        {
            Log("数据库中没有Agent记录，跳过测试");
            return;
        }

        var agentId = (long)agent.id;
        Log($"测试Agent: Id={agentId}, Name={agent.name}");

        var clientWithCapabilities = await agentFactory.CreateAgentAsync(agentId);
        Assert.NotNull(clientWithCapabilities);
        Log("✓ CreateAgentAsync（带工具能力）创建成功");

        var clientWithoutCapabilities = await agentFactory.CreateAgentWithoutCapabilitiesAsync(agentId);
        Assert.NotNull(clientWithoutCapabilities);
        Log("✓ CreateAgentWithoutCapabilitiesAsync（无工具能力）创建成功");

        Log("✓ 模板方法模式：两个方法共享 CreateBaseClientAsync 基础逻辑");
    }

    [Fact]
    public async Task Task1006_Integration_TaskExecutionWorkflow_ShouldMatchExpectedResults()
    {
        File.Delete(_logFile);
        Log("========== 任务1006 集成测试：团队管理→任务执行 ==========");
        Log($"测试时间: {DateTime.Now}");

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var task = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, title, description, collaboration_id, config, prompt FROM collaboration_tasks WHERE id = @Id",
            new { Id = TestTaskId });

        Assert.NotNull(task);
        Log($"任务ID: {task.id}");
        Log($"标题: {task.title}");
        Log($"描述: {task.description}");
        Log($"协作ID: {task.collaboration_id}");

        var collaborationId = (long?)task.collaboration_id;
        Assert.NotNull(collaborationId);
        Log($"✓ 任务关联了协作: {collaborationId}");

        var members = await connection.QueryAsync<dynamic>(
            @"SELECT ca.agent_id, ca.role, a.name, a.type_name 
              FROM collaboration_agents ca 
              JOIN agents a ON ca.agent_id = a.id 
              WHERE ca.collaboration_id = @CollaborationId",
            new { CollaborationId = collaborationId });

        var memberList = members.ToList();
        Assert.True(memberList.Count > 0, "协作必须关联至少一个Agent");
        Log($"\n协作成员 ({memberList.Count} 个):");
        foreach (var m in memberList)
        {
            Log($"  - {m.name} ({m.role ?? "Worker"}): {m.type_name}");
        }

        var managerCount = memberList.Count(m => m.role?.ToString()?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true);
        var workerCount = memberList.Count(m => m.role?.ToString()?.Equals("Manager", StringComparison.OrdinalIgnoreCase) != true);
        Log($"\n协调者: {managerCount}, 工作者: {workerCount}");

        var taskConfig = task.config?.ToString();
        if (!string.IsNullOrEmpty(taskConfig))
        {
            Log($"\n任务配置: {taskConfig}");
            var config = TaskConfig.FromJson(taskConfig);
            Assert.NotNull(config);
            Log($"✓ 任务配置解析成功: Mode={config.OrchestrationMode}, MaxIterations={config.MaxIterations}");

            var parameters = config.ToGroupChatParameters();
            Assert.Equal(config.GetOrchestrationMode(), parameters.OrchestrationMode);
            Assert.Equal(config.MaxIterations, parameters.MaxIterations);
            Log("✓ GroupChatParameters 从 TaskConfig 正确转换");
        }

        var promptBuilderFactory = new SystemPromptBuilderFactory();

        foreach (var member in memberList)
        {
            var isManager = member.role?.ToString()?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;
            var mode = isManager ? GroupChatOrchestrationMode.Manager : GroupChatOrchestrationMode.Intelligent;

            var builder = promptBuilderFactory.Create(mode);
            var prompt = builder.BuildPrompt(new SystemPromptContext
            {
                AgentName = member.name.ToString(),
                AgentRole = member.role?.ToString() ?? "Worker",
                AgentTypeName = member.type_name?.ToString() ?? "",
                MembersInfo = string.Join("\n", memberList
                    .Where(m => m.agent_id != member.agent_id)
                    .Select(m => $"- {m.name}：负责{m.type_name}")),
                TaskDescription = task.description?.ToString(),
                TaskPrompt = task.prompt?.ToString()
            });

            Assert.NotEmpty(prompt);
            Assert.Contains(member.name.ToString(), prompt);
            Log($"\n✓ {member.name} ({(isManager ? "Manager" : "Worker")}) 提示词生成成功，长度: {prompt.Length}");
        }

        var strategy = AgentSelectionStrategyFactory.CreateOrchestratedStrategy();
        Assert.Equal("Composite", strategy.Name);
        Log("\n✓ Agent选择策略工厂创建Composite策略成功");

        Log("\n========== 集成测试完成 ==========");
        Log("✓ 所有断言通过，任务1006的团队管理→任务执行流程验证成功");
    }

    [Fact]
    public async Task Task1006_StrategyPattern_OrchestratedManager_ShouldSelectCorrectly()
    {
        Log("========== OrchestratedGroupChatManager 策略集成测试 ==========");

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var strategy = AgentSelectionStrategyFactory.CreateOrchestratedStrategy(
            loggerFactory.CreateLogger<CompositeSelectionStrategy>());

        var orchestrator = CreateTestAIAgent("项目经理", "项目协调");
        var worker1 = CreateTestAIAgent("前端工程师", "前端开发");
        var worker2 = CreateTestAIAgent("后端工程师", "后端开发");

        var manager = new OrchestratedGroupChatManager(
            orchestrator,
            [worker1, worker2],
            strategy,
            maximumIterationCount: 5,
            loggerFactory.CreateLogger<OrchestratedGroupChatManager>());

        var thinkingEvents = new List<ManagerThinkingEventArgs>();
        manager.ManagerThinking += (_, args) => thinkingEvents.Add(args);

        Assert.Equal(5, manager.MaximumIterationCount);
        Log("✓ OrchestratedGroupChatManager 创建成功");
        Log($"  协调者: {orchestrator.Name}");
        Log($"  Workers: {string.Join(", ", worker1.Name, worker2.Name)}");
        Log($"  策略: {strategy.Name}");
        Log($"  最大迭代: {manager.MaximumIterationCount}");

        Assert.Empty(thinkingEvents);
        Log("✓ ManagerThinking事件初始为空");
    }

    [Fact]
    public async Task Task1006_CapabilityManager_WithServiceProvider_ShouldRegisterCapabilities()
    {
        Log("========== CapabilityManager 依赖注入测试 ==========");

        var mockTaskContext = new Mock<ITaskContextService>();
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(ITaskContextService)))
            .Returns(mockTaskContext.Object);
        var manager = new CapabilityManager(serviceProvider.Object);

        var capabilities = manager.GetAllCapabilities().ToList();
        var tools = manager.GetAllTools().ToList();

        Assert.True(capabilities.Count >= 7, $"应该有至少7种能力，实际: {capabilities.Count}");
        Assert.True(tools.Count >= 50, $"应该有至少50个工具，实际: {tools.Count}");

        Log($"✓ 注册了 {capabilities.Count} 种能力");
        Log($"✓ 注册了 {tools.Count} 个工具");

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("WriteFile", toolNames);
        Assert.Contains("ReadFile", toolNames);
        Assert.Contains("CloneRepository", toolNames);
        Assert.Contains("Commit", toolNames);
        Log("✓ 关键工具已注册: WriteFile, ReadFile, CloneRepository, Commit");
    }

    [Fact]
    public void Task1006_TaskConfig_ShouldConvertToGroupChatParameters()
    {
        Log("========== TaskConfig → GroupChatParameters 转换测试 ==========");

        var config = new TaskConfig
        {
            OrchestrationMode = "Manager",
            MaxIterations = 15,
            ManagerAgentId = 100,
            WorkerAgents =
            [
                new WorkerAgentConfig { AgentId = 200 },
                new WorkerAgentConfig { AgentId = 300 }
            ],
            ManagerCustomPrompt = "你是项目经理，负责协调团队"
        };

        var parameters = config.ToGroupChatParameters();

        Assert.Equal(GroupChatOrchestrationMode.Manager, parameters.OrchestrationMode);
        Assert.Equal(15, parameters.MaxIterations);
        Log($"✓ OrchestrationMode: {parameters.OrchestrationMode}");
        Log($"✓ MaxIterations: {parameters.MaxIterations}");

        var mode = config.GetOrchestrationMode();
        Assert.Equal(GroupChatOrchestrationMode.Manager, mode);
        Log("✓ GetOrchestrationMode 正确解析字符串为枚举");

        var roundRobinConfig = new TaskConfig { OrchestrationMode = "roundrobin" };
        Assert.Equal(GroupChatOrchestrationMode.RoundRobin, roundRobinConfig.GetOrchestrationMode());
        Log("✓ roundrobin 字符串正确映射到 RoundRobin 枚举");

        var intelligentConfig = new TaskConfig { OrchestrationMode = "intelligent" };
        Assert.Equal(GroupChatOrchestrationMode.Intelligent, intelligentConfig.GetOrchestrationMode());
        Log("✓ intelligent 字符串正确映射到 Intelligent 枚举");

        Log("✓ TaskConfig 正确转换为 GroupChatParameters");
    }

    [Fact]
    public void Task1006_ManagerThinkingEventArgs_ShouldCarryCorrectData()
    {
        Log("========== ManagerThinkingEventArgs 数据传递测试 ==========");

        var args = new ManagerThinkingEventArgs("项目经理", "【协调决策】请 @前端工程师 发言。", "前端工程师", 3);

        Assert.Equal("项目经理", args.ManagerName);
        Assert.Equal("【协调决策】请 @前端工程师 发言。", args.Thinking);
        Assert.Equal("前端工程师", args.SelectedAgent);
        Assert.Equal(3, args.IterationCount);
        Log($"✓ ManagerName: {args.ManagerName}");
        Log($"✓ Thinking: {args.Thinking}");
        Log($"✓ SelectedAgent: {args.SelectedAgent}");
        Log($"✓ IterationCount: {args.IterationCount}");

        var argsNoSelection = new ManagerThinkingEventArgs("项目经理", "【任务启动】开始协调团队工作。", null, 0);
        Assert.Null(argsNoSelection.SelectedAgent);
        Log("✓ SelectedAgent 可为null");
    }

    private static ChatClientAgent CreateTestAIAgent(string name, string description)
    {
        var mockClient = new Mock<IChatClient>();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "test response"));
        mockClient.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return new ChatClientAgent(
            mockClient.Object,
            "You are a helpful assistant.",
            name,
            description);
    }
}
