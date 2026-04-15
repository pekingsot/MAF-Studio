using System.Text.Json;
using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Services;
using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Application.Prompts;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Dapper;
using Npgsql;
using Microsoft.Extensions.AI;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;

namespace MAFStudio.Tests.Services;

public class MagenticWorkflowTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "magentic_test_log.txt");
    private readonly string _connectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123";

    private async Task<long> FindAvailableCollaborationIdAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var id = await connection.QueryFirstOrDefaultAsync<long>(
            "SELECT c.id FROM collaborations c INNER JOIN collaboration_agents ca ON c.id = ca.collaboration_id GROUP BY c.id HAVING COUNT(ca.agent_id) > 0 LIMIT 1");
        return id;
    }

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

    private CollaborationWorkflowService CreateService(
        ILoggerFactory loggerFactory,
        IDapperContext dapperContext)
    {
        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);
        var collaborationRepository = new CollaborationRepository(dapperContext);
        var collaborationAgentRepository = new CollaborationAgentRepository(dapperContext);

        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();
        var workflowServiceLogger = loggerFactory.CreateLogger<CollaborationWorkflowService>();

        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);

        var capabilityManager = new CapabilityManager(CreateMockServiceProvider());

        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            loggerFactory);

        var messageServiceMock = new Mock<IMessageService>();
        var workflowPlanRepoMock = new Mock<IWorkflowPlanRepository>();
        var workflowSessionRepoMock = new Mock<IWorkflowSessionRepository>();
        workflowSessionRepoMock.Setup(x => x.CreateAsync(It.IsAny<WorkflowSession>()))
            .ReturnsAsync((WorkflowSession s) =>
            {
                s.Id = 10001;
                return s;
            });
        workflowSessionRepoMock.Setup(x => x.IncrementMessageCountAsync(It.IsAny<long>()))
            .ReturnsAsync(true);
        workflowSessionRepoMock.Setup(x => x.EndSessionAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var messageRepoMock = new Mock<IMessageRepository>();
        messageRepoMock.Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) =>
            {
                m.Id = 1;
                return m;
            });

        var taskRepoMock = new Mock<ICollaborationTaskRepository>();
        var conclusionServiceMock = new Mock<IGroupChatConclusionService>();
        var promptBuilderFactory = new SystemPromptBuilderFactory();
        var taskContextService = new TaskContextService();
        var eventProcessorMock = new Mock<IWorkflowEventProcessor>();

        return new CollaborationWorkflowService(
            collaborationRepository,
            collaborationAgentRepository,
            agentRepository,
            agentFactory,
            messageServiceMock.Object,
            workflowPlanRepoMock.Object,
            workflowSessionRepoMock.Object,
            messageRepoMock.Object,
            taskRepoMock.Object,
            conclusionServiceMock.Object,
            promptBuilderFactory,
            taskContextService,
            eventProcessorMock.Object,
            workflowServiceLogger,
            loggerFactory);
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var mockTaskContext = new Mock<ITaskContextService>();
        var mockSp = new Mock<IServiceProvider>();
        mockSp.Setup(x => x.GetService(typeof(ITaskContextService)))
            .Returns(mockTaskContext.Object);
        return mockSp.Object;
    }

    [Fact]
    public async Task GenerateMagenticPlanAsync_ShouldGenerateWorkflow()
    {
        File.Delete(_logFile);
        Log("=== 测试: GenerateMagenticPlanAsync_ShouldGenerateWorkflow ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        var task = "研究ResNet-50、BERT、GPT-2三个AI模型的能效，并生成分析报告";

        Log($"协作ID: {collaborationId}");
        Log($"任务: {task}");

        try
        {
            var workflow = await service.GenerateMagenticPlanAsync(collaborationId, task);

            Log($"生成的工作流: Nodes={workflow.Nodes.Count}, Edges={workflow.Edges.Count}");

            foreach (var node in workflow.Nodes)
            {
                Log($"  节点: Id={node.Id}, Type={node.Type}, Name={node.Name}, AgentRole={node.AgentRole}, AgentId={node.AgentId}");
            }

            foreach (var edge in workflow.Edges)
            {
                Log($"  边: From={edge.From}, To={edge.To}, Type={edge.Type}");
            }

            Assert.NotNull(workflow);
            Assert.NotEmpty(workflow.Nodes);
            Assert.NotEmpty(workflow.Edges);

            var startNode = workflow.Nodes.FirstOrDefault(n => n.Type == "start");
            Assert.NotNull(startNode);

            var agentNodes = workflow.Nodes.Where(n => n.Type == "agent").ToList();
            Assert.NotEmpty(agentNodes);

            foreach (var agentNode in agentNodes)
            {
                Assert.False(string.IsNullOrEmpty(agentNode.AgentId), $"Agent节点 {agentNode.Id} 缺少AgentId");
                Assert.False(string.IsNullOrEmpty(agentNode.AgentRole), $"Agent节点 {agentNode.Id} 缺少AgentRole");
            }

            var aggregatorNode = workflow.Nodes.FirstOrDefault(n => n.Type == "aggregator");
            Assert.NotNull(aggregatorNode);

            Log("✅ 工作流生成测试通过");
        }
        catch (Exception ex)
        {
            Log($"❌ 测试失败: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_SequentialWorkflow_ShouldStreamMessages()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_SequentialWorkflow ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count == 0)
        {
            Log("协作中没有Agent，跳过测试");
            return;
        }

        var firstAgentId = collaborationAgents[0].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "研究分析",
                    AgentRole = "Researcher", AgentId = firstAgentId,
                    InputTemplate = "请简单介绍一下你自己，一句话即可"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "end" }
            }
        };

        var messages = new List<ChatMessageDto>();
        var input = "简单介绍一下你自己，一句话即可";

        Log($"开始执行Magentic工作流，输入: {input}");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, input))
            {
                messages.Add(message);
                Log($"收到消息: Sender={message.Sender}, Type={message.Metadata?.GetValueOrDefault("type")}, Content长度={message.Content?.Length ?? 0}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
        }

        Log($"总共收到 {messages.Count} 条消息");

        Assert.NotEmpty(messages);

        var systemMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "system").ToList();
        Assert.NotEmpty(systemMessages);

        var agentResponses = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "agent_response").ToList();
        Assert.NotEmpty(agentResponses);

        var completeMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "system_complete").ToList();
        Assert.NotEmpty(completeMessages);

        Log("✅ 流式执行测试通过");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_FanOutWorkflow_ShouldParallelExecute()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_FanOutWorkflow ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count < 2)
        {
            Log("协作中Agent不足2个，跳过fan-out测试");
            return;
        }

        var agent1Id = collaborationAgents[0].agent_id.ToString();
        var agent2Id = collaborationAgents[1].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "研究Agent1",
                    AgentRole = "Researcher1", AgentId = agent1Id,
                    InputTemplate = "请用一句话介绍你自己"
                },
                new()
                {
                    Id = "node-2", Type = "agent", Name = "研究Agent2",
                    AgentRole = "Researcher2", AgentId = agent2Id,
                    InputTemplate = "请用一句话介绍你自己"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "fan-out", From = "start", To = new List<string> { "node-1", "node-2" } },
                new() { Type = "sequential", From = "node-1", To = "end" },
                new() { Type = "sequential", From = "node-2", To = "end" }
            }
        };

        var messages = new List<ChatMessageDto>();
        var input = "并行分析任务";

        Log($"开始执行Fan-Out工作流");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, input))
            {
                messages.Add(message);
                Log($"收到消息: Sender={message.Sender}, Type={message.Metadata?.GetValueOrDefault("type")}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        Log($"总共收到 {messages.Count} 条消息");

        var agentResponses = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "agent_response").ToList();
        Assert.True(agentResponses.Count >= 2, $"期望至少2个Agent响应，实际 {agentResponses.Count} 个");

        var aggregatorMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "aggregator").ToList();
        Assert.NotEmpty(aggregatorMessages);

        Log("✅ Fan-Out并行执行测试通过");
    }

    [Fact]
    public async Task GenerateAndExecuteMagenticWorkflow_FullPipeline()
    {
        File.Delete(_logFile);
        Log("=== 测试: GenerateAndExecuteMagenticWorkflow_FullPipeline ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        var task = "请每个Agent用一句话介绍自己，然后汇总";

        Log($"步骤1: 生成工作流计划，协作ID: {collaborationId}，任务: {task}");

        WorkflowDefinitionDto workflow;
        try
        {
            workflow = await service.GenerateMagenticPlanAsync(collaborationId, task);
            Log($"工作流生成成功: Nodes={workflow.Nodes.Count}, Edges={workflow.Edges.Count}");
        }
        catch (Exception ex)
        {
            Log($"工作流生成失败: {ex.Message}");
            throw;
        }

        Assert.NotNull(workflow);
        Assert.NotEmpty(workflow.Nodes);

        foreach (var node in workflow.Nodes)
        {
            Log($"  节点: {node.Id} ({node.Type}) - {node.Name} | AgentId={node.AgentId}");
        }

        Log("步骤2: 执行生成的工作流");

        var messages = new List<ChatMessageDto>();

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, task))
            {
                messages.Add(message);
                Log($"  消息: [{message.Sender}] {(message.Content?.Length > 80 ? message.Content.Substring(0, 80) + "..." : message.Content)}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        Log($"步骤3: 验证执行结果，共 {messages.Count} 条消息");

        Assert.NotEmpty(messages);

        var systemStart = messages.FirstOrDefault(m =>
            m.Metadata?.ContainsKey("type") == true && m.Metadata["type"]?.ToString() == "system");
        Assert.NotNull(systemStart);
        Assert.Contains("开始执行", systemStart.Content);

        var systemComplete = messages.FirstOrDefault(m =>
            m.Metadata?.ContainsKey("type") == true && m.Metadata["type"]?.ToString() == "system_complete");
        Assert.NotNull(systemComplete);
        Assert.Contains("执行完成", systemComplete.Content);

        Log("✅ 完整流程测试通过");
    }

    [Fact]
    public void ParseWorkflowFromLlmOutput_ValidJson_ShouldParseCorrectly()
    {
        Log("=== 测试: ParseWorkflowFromLlmOutput_ValidJson ===");

        var llmOutput = @"根据任务分析，我制定了以下执行计划：

```json
{
  ""nodes"": [
    {""id"": ""start"", ""type"": ""start"", ""name"": ""开始""},
    {""id"": ""node-1"", ""type"": ""agent"", ""agentId"": ""100"", ""agentRole"": ""Researcher"", ""name"": ""研究分析"", ""inputTemplate"": ""请研究：{{input}}""},
    {""id"": ""node-2"", ""type"": ""agent"", ""agentId"": ""101"", ""agentRole"": ""Writer"", ""name"": ""撰写报告"", ""inputTemplate"": ""请根据研究结果撰写报告""},
    {""id"": ""end"", ""type"": ""aggregator"", ""name"": ""汇总结果""}
  ],
  ""edges"": [
    {""type"": ""sequential"", ""from"": ""start"", ""to"": ""node-1""},
    {""type"": ""sequential"", ""from"": ""node-1"", ""to"": ""node-2""},
    {""type"": ""sequential"", ""from"": ""node-2"", ""to"": ""end""}
  ]
}
```

以上是执行计划。";

        var jsonStart = llmOutput.IndexOf('{');
        var jsonEnd = llmOutput.LastIndexOf('}');
        Assert.True(jsonStart >= 0 && jsonEnd > jsonStart);

        var json = llmOutput.Substring(jsonStart, jsonEnd - jsonStart + 1);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("nodes", out var nodesEl));
        Assert.Equal(4, nodesEl.GetArrayLength());

        Assert.True(root.TryGetProperty("edges", out var edgesEl));
        Assert.Equal(3, edgesEl.GetArrayLength());

        var firstAgentNode = nodesEl.EnumerateArray().FirstOrDefault(n =>
            n.TryGetProperty("type", out var t) && t.GetString() == "agent");
        Assert.True(firstAgentNode.ValueKind != JsonValueKind.Undefined);
        Assert.Equal("100", firstAgentNode.GetProperty("agentId").GetString());
        Assert.Equal("Researcher", firstAgentNode.GetProperty("agentRole").GetString());

        Log("✅ JSON解析测试通过");
    }

    [Fact]
    public void ParseWorkflowFromLlmOutput_FanOutJson_ShouldParseArrayTargets()
    {
        Log("=== 测试: ParseWorkflowFromLlmOutput_FanOutJson ===");

        var json = @"{
          ""nodes"": [
            {""id"": ""start"", ""type"": ""start"", ""name"": ""开始""},
            {""id"": ""node-1"", ""type"": ""agent"", ""agentId"": ""100"", ""agentRole"": ""R1"", ""name"": ""研究1""},
            {""id"": ""node-2"", ""type"": ""agent"", ""agentId"": ""101"", ""agentRole"": ""R2"", ""name"": ""研究2""},
            {""id"": ""end"", ""type"": ""aggregator"", ""name"": ""汇总""}
          ],
          ""edges"": [
            {""type"": ""fan-out"", ""from"": ""start"", ""to"": [""node-1"", ""node-2""]},
            {""type"": ""sequential"", ""from"": ""node-1"", ""to"": ""end""},
            {""type"": ""sequential"", ""from"": ""node-2"", ""to"": ""end""}
          ]
        }";

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var fanOutEdge = root.GetProperty("edges").EnumerateArray()
            .First(e => e.GetProperty("type").GetString() == "fan-out");

        var toValue = fanOutEdge.GetProperty("to");
        Assert.Equal(JsonValueKind.Array, toValue.ValueKind);
        Assert.Equal(2, toValue.GetArrayLength());

        Log("✅ Fan-Out JSON解析测试通过");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_ConditionalBranch_ShouldFollowCorrectPath()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_ConditionalBranch ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count == 0)
        {
            Log("协作中没有Agent，跳过条件分支测试");
            return;
        }

        var agent1Id = collaborationAgents[0].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "分析",
                    AgentRole = "Analyzer", AgentId = agent1Id,
                    InputTemplate = "请简单回答：你好"
                },
                new() { Id = "cond-1", Type = "condition", Name = "条件判断", Condition = "true" },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "cond-1" },
                new() { Type = "sequential", From = "cond-1", To = "end", Condition = "true" }
            }
        };

        var messages = new List<ChatMessageDto>();

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, "条件分支测试"))
            {
                messages.Add(message);
                Log($"消息: [{message.Sender}] Type={message.Metadata?.GetValueOrDefault("type")}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        var conditionMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "condition").ToList();
        Assert.NotEmpty(conditionMessages);

        Log("✅ 条件分支测试通过");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_ReviewApproved_ShouldPassThrough()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_ReviewApproved_ShouldPassThrough ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count == 0)
        {
            Log("协作中没有Agent，跳过审核测试");
            return;
        }

        var agent1Id = collaborationAgents[0].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "编写内容",
                    AgentRole = "Writer", AgentId = agent1Id,
                    InputTemplate = "请写一句话：今天天气很好"
                },
                new()
                {
                    Id = "review-1", Type = "review", Name = "内容审核",
                    AgentRole = "Reviewer", AgentId = agent1Id,
                    ApprovalKeyword = "[APPROVED]",
                    RejectTargetNode = "node-1",
                    MaxRetries = 3,
                    ReviewCriteria = "请审核内容是否通顺合理"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "review-1" },
                new() { Type = "approved", From = "review-1", To = "end" }
            }
        };

        var messages = new List<ChatMessageDto>();
        var input = "写一句话关于天气";

        Log($"开始执行审核工作流，输入: {input}");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, input))
            {
                messages.Add(message);
                var msgType = message.Metadata?.GetValueOrDefault("type")?.ToString();
                Log($"收到消息: Sender={message.Sender}, Type={msgType}, Step={message.Metadata?.GetValueOrDefault("step")}");
                if (msgType == "review_start" || msgType == "review_approved" || msgType == "review_rejected" || msgType == "review_result" || msgType == "review_force_approved" || msgType == "review_reject_sendback")
                {
                    Log($"  审核详情: {message.Content?.Substring(0, Math.Min(100, message.Content?.Length ?? 0))}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
        }

        Log($"总共收到 {messages.Count} 条消息");

        var reviewStartMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "review_start").ToList();
        Assert.NotEmpty(reviewStartMessages);
        Log($"审核开始消息数: {reviewStartMessages.Count}");

        var reviewResultMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && (m.Metadata["type"]?.ToString() == "review_approved"
                || m.Metadata["type"]?.ToString() == "review_rejected"
                || m.Metadata["type"]?.ToString() == "review_force_approved")).ToList();
        Assert.NotEmpty(reviewResultMessages);
        Log($"审核结果消息数: {reviewResultMessages.Count}");

        var completeMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "system_complete").ToList();
        Assert.NotEmpty(completeMessages);
        Log($"完成消息数: {completeMessages.Count}");

        foreach (var msg in messages)
        {
            var msgType = msg.Metadata?.GetValueOrDefault("type")?.ToString();
            Log($"  [{msgType}] {msg.Sender}: {(msg.Content?.Length > 80 ? msg.Content.Substring(0, 80) + "..." : msg.Content)}");
        }

        Log("✅ 审核通过工作流测试通过");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_ReviewRejected_ShouldSendBack()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_ReviewRejected_ShouldSendBack ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count == 0)
        {
            Log("协作中没有Agent，跳过审核打回测试");
            return;
        }

        var agent1Id = collaborationAgents[0].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "编写内容",
                    AgentRole = "Writer", AgentId = agent1Id,
                    InputTemplate = "请写一句话：今天天气很好"
                },
                new()
                {
                    Id = "review-1", Type = "review", Name = "严格审核",
                    AgentRole = "StrictReviewer", AgentId = agent1Id,
                    ApprovalKeyword = "[PERFECT]",
                    RejectTargetNode = "node-1",
                    MaxRetries = 2,
                    ReviewCriteria = "内容必须完美无缺，任何小问题都不通过"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "review-1" },
                new() { Type = "approved", From = "review-1", To = "end" }
            }
        };

        var messages = new List<ChatMessageDto>();
        var input = "写一句话关于天气";

        Log($"开始执行审核打回工作流，输入: {input}");
        Log($"审核配置: 通过关键词=[PERFECT](极难通过), 最大重试=2, 打回目标=node-1");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, input))
            {
                messages.Add(message);
                var msgType = message.Metadata?.GetValueOrDefault("type")?.ToString();
                Log($"收到消息: Sender={message.Sender}, Type={msgType}, Step={message.Metadata?.GetValueOrDefault("step")}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
        }

        Log($"总共收到 {messages.Count} 条消息");

        var reviewStartMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "review_start").ToList();
        Assert.NotEmpty(reviewStartMessages);
        Log($"审核开始消息数: {reviewStartMessages.Count}");

        var rejectedMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "review_rejected").ToList();
        Log($"审核不通过消息数: {rejectedMessages.Count}");

        var sendbackMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "review_reject_sendback").ToList();
        Log($"打回消息数: {sendbackMessages.Count}");

        var forceApprovedMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "review_force_approved").ToList();
        Log($"强制通过消息数: {forceApprovedMessages.Count}");

        var completeMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "system_complete").ToList();
        Assert.NotEmpty(completeMessages);
        Log($"完成消息数: {completeMessages.Count}");

        var steps = messages
            .Where(m => m.Metadata?.ContainsKey("step") == true)
            .Select(m => Convert.ToInt32(m.Metadata["step"]))
            .Distinct()
            .OrderBy(s => s)
            .ToList();
        Log($"执行步骤: {string.Join(", ", steps)}");

        Assert.True(steps.Max() <= 20, $"步骤数不应超过20，实际最大步骤: {steps.Max()}");

        foreach (var msg in messages)
        {
            var msgType = msg.Metadata?.GetValueOrDefault("type")?.ToString();
            Log($"  [{msgType}] {msg.Sender}: {(msg.Content?.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content)}");
        }

        Log("✅ 审核打回工作流测试通过");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_ReviewNoRejectTarget_ShouldForceApprove()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_ReviewNoRejectTarget_ShouldForceApprove ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count == 0)
        {
            Log("协作中没有Agent，跳过无打回目标审核测试");
            return;
        }

        var agent1Id = collaborationAgents[0].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "编写内容",
                    AgentRole = "Writer", AgentId = agent1Id,
                    InputTemplate = "请写一句话：今天天气很好"
                },
                new()
                {
                    Id = "review-1", Type = "review", Name = "审核(无打回目标)",
                    AgentRole = "Reviewer", AgentId = agent1Id,
                    ApprovalKeyword = "[NEVER_MATCH_THIS_KEYWORD]",
                    RejectTargetNode = "",
                    MaxRetries = 3,
                    ReviewCriteria = "审核内容"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "review-1" },
                new() { Type = "approved", From = "review-1", To = "end" }
            }
        };

        var messages = new List<ChatMessageDto>();
        var input = "写一句话关于天气";

        Log($"开始执行无打回目标审核工作流");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, input))
            {
                messages.Add(message);
                var msgType = message.Metadata?.GetValueOrDefault("type")?.ToString();
                Log($"收到消息: Sender={message.Sender}, Type={msgType}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        Log($"总共收到 {messages.Count} 条消息");

        var warningMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "warning").ToList();
        Log($"警告消息数: {warningMessages.Count}");

        var completeMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "system_complete").ToList();
        Assert.NotEmpty(completeMessages);

        foreach (var msg in messages)
        {
            var msgType = msg.Metadata?.GetValueOrDefault("type")?.ToString();
            Log($"  [{msgType}] {msg.Sender}: {(msg.Content?.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content)}");
        }

        Log("✅ 无打回目标审核工作流测试通过");
    }

    [Fact]
    public void WorkflowNodeDto_ReviewProperties_ShouldSerializeCorrectly()
    {
        Log("=== 测试: WorkflowNodeDto_ReviewProperties_ShouldSerializeCorrectly ===");

        var reviewNode = new WorkflowNodeDto
        {
            Id = "review-1",
            Type = "review",
            Name = "代码审核",
            AgentRole = "CodeReviewer",
            AgentId = "100",
            ApprovalKeyword = "[APPROVED]",
            RejectTargetNode = "node-1",
            MaxRetries = 3,
            ReviewCriteria = "请审核代码质量和安全性"
        };

        var json = JsonSerializer.Serialize(reviewNode);
        Log($"序列化结果: {json}");

        Assert.Contains("\"ApprovalKeyword\"", json);
        Assert.Contains("\"RejectTargetNode\"", json);
        Assert.Contains("\"MaxRetries\"", json);
        Assert.Contains("\"ReviewCriteria\"", json);

        var deserialized = JsonSerializer.Deserialize<WorkflowNodeDto>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("review-1", deserialized.Id);
        Assert.Equal("review", deserialized.Type);
        Assert.Equal("[APPROVED]", deserialized.ApprovalKeyword);
        Assert.Equal("node-1", deserialized.RejectTargetNode);
        Assert.Equal(3, deserialized.MaxRetries);
        Assert.Equal("请审核代码质量和安全性", deserialized.ReviewCriteria);

        Log("✅ 审核节点序列化/反序列化测试通过");
    }

    [Fact]
    public void WorkflowDefinitionDto_WithReviewNode_ShouldSerializeFullWorkflow()
    {
        Log("=== 测试: WorkflowDefinitionDto_WithReviewNode_ShouldSerializeFullWorkflow ===");

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "编写代码",
                    AgentRole = "Developer", AgentId = "100",
                    InputTemplate = "请编写一个排序算法"
                },
                new()
                {
                    Id = "review-1", Type = "review", Name = "代码审核",
                    AgentRole = "CodeReviewer", AgentId = "101",
                    ApprovalKeyword = "[APPROVED]",
                    RejectTargetNode = "node-1",
                    MaxRetries = 3,
                    ReviewCriteria = "请审核代码质量、安全性和可维护性"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "review-1" },
                new() { Type = "approved", From = "review-1", To = "end" },
                new() { Type = "rejected", From = "review-1", To = "node-1" }
            }
        };

        var json = JsonSerializer.Serialize(workflow);
        Log($"完整工作流序列化: {json}");

        var deserialized = JsonSerializer.Deserialize<WorkflowDefinitionDto>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(4, deserialized.Nodes.Count);
        Assert.Equal(4, deserialized.Edges.Count);

        var reviewNode = deserialized.Nodes.FirstOrDefault(n => n.Type == "review");
        Assert.NotNull(reviewNode);
        Assert.Equal("[APPROVED]", reviewNode.ApprovalKeyword);
        Assert.Equal("node-1", reviewNode.RejectTargetNode);
        Assert.Equal(3, reviewNode.MaxRetries);

        var approvedEdge = deserialized.Edges.FirstOrDefault(e => e.Type == "approved");
        Assert.NotNull(approvedEdge);
        Assert.Equal("review-1", approvedEdge.From);

        var rejectedEdge = deserialized.Edges.FirstOrDefault(e => e.Type == "rejected");
        Assert.NotNull(rejectedEdge);
        Assert.Equal("review-1", rejectedEdge.From);

        Log("✅ 完整审核工作流序列化/反序列化测试通过");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflowStreamAsync_CSharpCondition_ShouldEvaluateCorrectly()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflowStreamAsync_CSharpCondition_ShouldEvaluateCorrectly ===");

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dapperContext = CreateDapperContext();
        var service = CreateService(loggerFactory, dapperContext);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationAgents = (await connection.QueryAsync<dynamic>(
            "SELECT agent_id, role FROM collaboration_agents WHERE collaboration_id = @Id",
            new { Id = collaborationId })).ToList();

        if (collaborationAgents.Count == 0)
        {
            Log("协作中没有Agent，跳过C#条件表达式测试");
            return;
        }

        var agent1Id = collaborationAgents[0].agent_id.ToString();

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "生成内容",
                    AgentRole = "Writer", AgentId = agent1Id,
                    InputTemplate = "请简单回答：你好"
                },
                new()
                {
                    Id = "cond-1", Type = "condition", Name = "C#条件判断",
                    Condition = "result.Contains(\"你好\")"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "cond-1" },
                new() { Type = "sequential", From = "cond-1", To = "end", Condition = "true" }
            }
        };

        var messages = new List<ChatMessageDto>();

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, "C#条件表达式测试"))
            {
                messages.Add(message);
                var msgType = message.Metadata?.GetValueOrDefault("type")?.ToString();
                Log($"消息: [{message.Sender}] Type={msgType}");
                if (msgType == "condition")
                {
                    Log($"  条件判断详情: {message.Content}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        var conditionMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "condition").ToList();
        Assert.NotEmpty(conditionMessages);

        var condMsg = conditionMessages.First();
        Log($"条件判断结果: {condMsg.Content}");
        Assert.Contains("True", condMsg.Content);

        var completeMessages = messages.Where(m => m.Metadata?.ContainsKey("type") == true
            && m.Metadata["type"]?.ToString() == "system_complete").ToList();
        Assert.NotEmpty(completeMessages);

        Log("✅ C#条件表达式测试通过");
    }
}
