using System.Text.Json;
using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Services;
using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.Prompts;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
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

public class TaskFlowAndStepLimitTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "taskflow_test_log.txt");
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
                s.Id = 20001;
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
    public async Task TaskFlow_ShouldStoreAndRetrieveWorkflowDefinition()
    {
        File.Delete(_logFile);
        Log("=== 测试: TaskFlow_ShouldStoreAndRetrieveWorkflowDefinition ===");

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "研究分析",
                    AgentRole = "Researcher", AgentId = "1001",
                    InputTemplate = "请分析以下内容：{{input}}"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "end" }
            }
        };

        var taskFlowJson = JsonSerializer.Serialize(workflow);
        Log($"序列化后的TaskFlow JSON: {taskFlowJson}");

        Assert.False(string.IsNullOrEmpty(taskFlowJson));

        var deserialized = JsonSerializer.Deserialize<WorkflowDefinitionDto>(taskFlowJson);
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Nodes.Count);
        Assert.Equal(2, deserialized.Edges.Count);

        var agentNode = deserialized.Nodes.FirstOrDefault(n => n.Type == "agent");
        Assert.NotNull(agentNode);
        Assert.Equal("Researcher", agentNode.AgentRole);
        Assert.Equal("1001", agentNode.AgentId);
        Assert.Equal("请分析以下内容：{{input}}", agentNode.InputTemplate);

        Log("✅ TaskFlow序列化/反序列化测试通过");
    }

    [Fact]
    public async Task StepCounter_ShouldEnforceMaxStepLimit()
    {
        File.Delete(_logFile);
        Log("=== 测试: StepCounter_ShouldEnforceMaxStepLimit ===");

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

        var manyNodes = new List<WorkflowNodeDto>
        {
            new() { Id = "start", Type = "start", Name = "开始" }
        };

        var manyEdges = new List<WorkflowEdgeDto>();

        for (int i = 1; i <= 25; i++)
        {
            manyNodes.Add(new WorkflowNodeDto
            {
                Id = $"node-{i}",
                Type = "agent",
                Name = $"步骤{i}",
                AgentRole = "Worker",
                AgentId = firstAgentId,
                InputTemplate = $"请简单说一句话即可，这是第{i}步"
            });

            var fromId = i == 1 ? "start" : $"node-{i - 1}";
            manyEdges.Add(new WorkflowEdgeDto
            {
                Type = "sequential",
                From = fromId,
                To = $"node-{i}"
            });
        }

        manyNodes.Add(new WorkflowNodeDto
        {
            Id = "end",
            Type = "aggregator",
            Name = "汇总结果"
        });
        manyEdges.Add(new WorkflowEdgeDto
        {
            Type = "sequential",
            From = "node-25",
            To = "end"
        });

        var workflow = new WorkflowDefinitionDto
        {
            Nodes = manyNodes,
            Edges = manyEdges
        };

        var messages = new List<ChatMessageDto>();
        var input = "简单测试步骤限制";

        Log($"创建包含 {manyNodes.Count} 个节点的工作流，测试最大步骤20限制");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, workflow, input))
            {
                messages.Add(message);
                var msgType = message.Metadata?.GetValueOrDefault("type")?.ToString();
                Log($"消息: Sender={message.Sender}, Type={msgType}, Step={message.Metadata?.GetValueOrDefault("step")}, Content={message.Content?.Substring(0, Math.Min(100, message.Content?.Length ?? 0))}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        Log($"总共收到 {messages.Count} 条消息");

        var errorMessages = messages.Where(m =>
            m.Metadata?.ContainsKey("type") == true &&
            m.Metadata["type"]?.ToString() == "error").ToList();

        var stepStartMessages = messages.Where(m =>
            m.Metadata?.ContainsKey("type") == true &&
            m.Metadata["type"]?.ToString() == "step_start").ToList();

        Log($"步骤开始消息数: {stepStartMessages.Count}");
        Log($"错误消息数: {errorMessages.Count}");

        foreach (var err in errorMessages)
        {
            Log($"错误消息内容: {err.Content}");
        }

        var maxStep = stepStartMessages.Max(m =>
        {
            if (m.Metadata?.TryGetValue("step", out var val) == true && val is int s)
                return s;
            return 0;
        });

        Log($"最大步骤号: {maxStep}");
        Assert.True(maxStep <= 20, $"最大步骤号不应超过20，实际为 {maxStep}");

        var hasMaxStepError = errorMessages.Any(m =>
            m.Content != null && m.Content.Contains("最大步骤限制"));
        Log($"是否包含最大步骤限制错误: {hasMaxStepError}");

        if (stepStartMessages.Count >= 20)
        {
            Assert.True(hasMaxStepError, "当步骤达到20步时，应该输出最大步骤限制错误消息");
        }

        Log("✅ 步骤限制测试通过");
    }

    [Fact]
    public async Task CollaborationTask_TaskFlowField_ShouldPersistInDatabase()
    {
        File.Delete(_logFile);
        Log("=== 测试: CollaborationTask_TaskFlowField_ShouldPersistInDatabase ===");

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var columnExists = await connection.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'collaboration_tasks' AND column_name = 'task_flow'");

        Assert.True(columnExists > 0, "collaboration_tasks 表应该包含 task_flow 字段");
        Log("✅ task_flow 字段存在于 collaboration_tasks 表中");

        var testTaskFlow = JsonSerializer.Serialize(new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new() { Id = "node-1", Type = "agent", Name = "测试节点", AgentRole = "Worker", AgentId = "1" },
                new() { Id = "end", Type = "aggregator", Name = "汇总" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "end" }
            }
        });

        var collaborationId = await FindAvailableCollaborationIdAsync();
        if (collaborationId == 0)
        {
            Log("数据库中没有包含Agent的协作，跳过测试");
            return;
        }

        var testTitle = $"TaskFlow测试_{Guid.NewGuid():N}".Substring(0, 30);

        var insertedId = await connection.QueryFirstAsync<long>(
            @"INSERT INTO collaboration_tasks (collaboration_id, title, status, created_at, task_flow)
              VALUES (@CollaborationId, @Title, 0, @CreatedAt, @TaskFlow)
              RETURNING id",
            new
            {
                CollaborationId = collaborationId,
                Title = testTitle,
                CreatedAt = DateTime.UtcNow,
                TaskFlow = testTaskFlow
            });

        Log($"插入测试任务 ID: {insertedId}");

        var retrievedTaskFlow = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT task_flow FROM collaboration_tasks WHERE id = @Id",
            new { Id = insertedId });

        Assert.False(string.IsNullOrEmpty(retrievedTaskFlow), "task_flow 字段应该能正确读取");
        Assert.Equal(testTaskFlow, retrievedTaskFlow);

        Log($"读取到的 task_flow: {retrievedTaskFlow}");

        var deserializedFlow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(retrievedTaskFlow);
        Assert.NotNull(deserializedFlow);
        Assert.Equal(3, deserializedFlow.Nodes.Count);
        Assert.Equal(2, deserializedFlow.Edges.Count);

        Log("✅ TaskFlow 数据库持久化测试通过");

        await connection.ExecuteAsync("DELETE FROM collaboration_tasks WHERE id = @Id", new { Id = insertedId });
        Log($"清理测试数据: 删除任务 ID {insertedId}");
    }

    [Fact]
    public async Task ExecuteMagenticWorkflow_WithTaskFlow_ShouldUseStoredWorkflow()
    {
        File.Delete(_logFile);
        Log("=== 测试: ExecuteMagenticWorkflow_WithTaskFlow_ShouldUseStoredWorkflow ===");

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

        var taskFlow = new WorkflowDefinitionDto
        {
            Nodes = new List<WorkflowNodeDto>
            {
                new() { Id = "start", Type = "start", Name = "开始" },
                new()
                {
                    Id = "node-1", Type = "agent", Name = "执行任务",
                    AgentRole = "Worker", AgentId = firstAgentId,
                    InputTemplate = "请简单说一句话"
                },
                new() { Id = "end", Type = "aggregator", Name = "汇总结果" }
            },
            Edges = new List<WorkflowEdgeDto>
            {
                new() { Type = "sequential", From = "start", To = "node-1" },
                new() { Type = "sequential", From = "node-1", To = "end" }
            }
        };

        var taskFlowJson = JsonSerializer.Serialize(taskFlow);

        var testTitle = $"TaskFlow执行测试_{Guid.NewGuid():N}".Substring(0, 30);

        var insertedId = await connection.QueryFirstAsync<long>(
            @"INSERT INTO collaboration_tasks (collaboration_id, title, status, created_at, task_flow)
              VALUES (@CollaborationId, @Title, 0, @CreatedAt, @TaskFlow)
              RETURNING id",
            new
            {
                CollaborationId = collaborationId,
                Title = testTitle,
                CreatedAt = DateTime.UtcNow,
                TaskFlow = taskFlowJson
            });

        Log($"创建带TaskFlow的任务 ID: {insertedId}");

        var retrievedTaskFlow = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT task_flow FROM collaboration_tasks WHERE id = @Id",
            new { Id = insertedId });

        Assert.False(string.IsNullOrEmpty(retrievedTaskFlow), "task_flow 应该已存储");

        var storedWorkflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(retrievedTaskFlow);
        Assert.NotNull(storedWorkflow);

        var messages = new List<ChatMessageDto>();
        var input = "简单测试";

        Log($"使用存储的TaskFlow执行工作流");

        try
        {
            await foreach (var message in service.ExecuteMagenticWorkflowStreamAsync(
                collaborationId, storedWorkflow, input, insertedId))
            {
                messages.Add(message);
                var msgType = message.Metadata?.GetValueOrDefault("type")?.ToString();
                Log($"消息: Sender={message.Sender}, Type={msgType}, Step={message.Metadata?.GetValueOrDefault("step")}");
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
        }

        Assert.NotEmpty(messages);

        var agentResponses = messages.Where(m =>
            m.Metadata?.ContainsKey("type") == true &&
            m.Metadata["type"]?.ToString() == "agent_response").ToList();

        Log($"Agent响应数: {agentResponses.Count}");
        Assert.NotEmpty(agentResponses);

        var completeMessages = messages.Where(m =>
            m.Metadata?.ContainsKey("type") == true &&
            m.Metadata["type"]?.ToString() == "system_complete").ToList();

        Log($"完成消息数: {completeMessages.Count}");

        if (completeMessages.Any())
        {
            var stepCount = completeMessages.First().Content;
            Log($"完成消息内容: {stepCount}");
            Assert.Contains("执行完成", stepCount);
        }

        Log("✅ 使用TaskFlow执行工作流测试通过");

        await connection.ExecuteAsync("DELETE FROM collaboration_tasks WHERE id = @Id", new { Id = insertedId });
        Log($"清理测试数据: 删除任务 ID {insertedId}");
    }
}
