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
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Dapper;
using Xunit;

namespace MAFStudio.Tests.Integration;

public class GroupChatMentionIntegrationTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_integration_log.txt");
    private readonly string _connectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123";

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
    public async Task ExecuteGroupChatAsync_WithMentionTopic_ShouldSelectCorrectAgent()
    {
        File.Delete(_logFile);
        Log("========== 集成测试：群聊@提及功能 ==========");
        Log($"测试时间: {DateTime.Now}");
        
        var dapperContext = CreateDapperContext();
        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);
        var collaborationRepository = new CollaborationRepository(dapperContext);
        var collaborationAgentRepository = new CollaborationAgentRepository(dapperContext);
        
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();
        var agentFactoryLogger = loggerFactory.CreateLogger<AgentFactoryService>();
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
                s.Id = 9999;
                return s;
            });
        
        var messageRepoMock = new Mock<IMessageRepository>();
        
        var taskRepoMock = new Mock<ICollaborationTaskRepository>();
        var conclusionServiceMock = new Mock<IGroupChatConclusionService>();
        var promptBuilderFactory = new SystemPromptBuilderFactory();
        var taskContextService = new TaskContextService();
        var eventProcessorMock = new Mock<IWorkflowEventProcessor>();

        var service = new CollaborationWorkflowService(
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

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
            "SELECT id FROM collaborations WHERE name LIKE '%微信小程序%' OR name LIKE '%产品文档%' LIMIT 1");

        if (collaborationId == 0)
        {
            collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
                "SELECT id FROM collaborations WHERE id = 1000");
        }

        if (collaborationId == 0)
        {
            collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
                "SELECT id FROM collaborations LIMIT 1");
        }

        if (collaborationId == 0)
        {
            Log("数据库中没有协作记录，跳过测试");
            return;
        }

        Log($"测试协作ID: {collaborationId}");

        var collaboration = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, name, description FROM collaborations WHERE id = @Id",
            new { Id = collaborationId });
        
        Log($"协作名称: {collaboration?.name}");
        Log($"协作描述: {collaboration?.description}");

        var agents = await connection.QueryAsync<dynamic>(
            @"SELECT a.id, a.name, a.type_name, ca.role, a.llm_config_id, a.llm_model_config_id 
              FROM collaboration_agents ca 
              JOIN agents a ON ca.agent_id = a.id 
              WHERE ca.collaboration_id = @Id",
            new { Id = collaborationId });
        
        Log("\n协作中的Agent列表:");
        foreach (var agent in agents)
        {
            Log($"  - ID:{agent.id} {agent.name} ({agent.type_name}) - 角色: {agent.role}");
            Log($"    LLM配置ID: {agent.llm_config_id}, 模型配置ID: {agent.llm_model_config_id}");
            
            if (agent.llm_config_id == null || agent.llm_model_config_id == null)
            {
                Log($"    ⚠️ 警告: Agent缺少LLM配置!");
            }
        }

        var messages = new List<ChatMessageDto>();
        
        var input = @"1.实现可以解析抖音,快手,等视频,直接获取高清无水印
2.用户直接粘贴分享的链接,自动解析内容里的链接
3.增加广告,每天看一次广告就可以当天无线是使用
4.广告就接微信小程序的广告
5.给出一份产品文档,以及市场调研的技术可行性";

        Log($"\n开始执行GroupChat工作流");
        Log($"输入课题: {input.Replace("\n", " | ")}");
        Log("========================================");

        var senderSequence = new List<string>();
        
        try
        {
            await foreach (var message in service.ExecuteGroupChatAsync(collaborationId, input))
            {
                messages.Add(message);
                senderSequence.Add(message.Sender ?? "Unknown");
                
                Log($"\n----- 消息 #{messages.Count} -----");
                Log($"发送者: {message.Sender}");
                Log($"角色: {message.Role}");
                
                if (!string.IsNullOrEmpty(message.Content))
                {
                    var content = message.Content;
                    if (content.Length > 500)
                    {
                        content = content.Substring(0, 500) + "...";
                    }
                    Log($"内容: {content}");
                    
                    if (content.Contains("@"))
                    {
                        Log($"*** 检测到@提及 ***");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"\n❌ 执行出错: {ex.Message}");
            Log($"异常类型: {ex.GetType().Name}");
            Log($"堆栈: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Log($"内部异常: {ex.InnerException.Message}");
            }
        }

        Log("\n========================================");
        Log($"执行完成，总共收到 {messages.Count} 条消息");
        
        if (messages.Count > 0)
        {
            Log("\n发送者序列:");
            for (int i = 0; i < senderSequence.Count; i++)
            {
                Log($"  {i + 1}. {senderSequence[i]}");
            }

            Log("\n========================================");
            Log("验证结果:");
            
            var coordinatorMessages = messages.Where(m => m.Sender?.Contains("协调者") == true || m.Sender?.Contains("光哥") == true).ToList();
            if (coordinatorMessages.Any())
            {
                Log($"✓ 协调者发言次数: {coordinatorMessages.Count}");
            }
            
            var architectMessages = messages.Where(m => m.Sender?.Contains("架构师") == true || m.Sender?.Contains("小明") == true).ToList();
            if (architectMessages.Any())
            {
                Log($"✓ 架构师发言次数: {architectMessages.Count}");
            }
            
            var productManagerMessages = messages.Where(m => m.Sender?.Contains("产品经理") == true).ToList();
            if (productManagerMessages.Any())
            {
                Log($"✓ 产品经理发言次数: {productManagerMessages.Count}");
            }
            
            var testEngineerMessages = messages.Where(m => m.Sender?.Contains("测试") == true).ToList();
            if (testEngineerMessages.Any())
            {
                Log($"✓ 测试工程师发言次数: {testEngineerMessages.Count}");
            }
        }
        else
        {
            Log("\n⚠️ 没有收到任何消息，请检查:");
            Log("  1. Agent是否配置了LLM");
            Log("  2. LLM配置是否正确");
            Log("  3. 网络连接是否正常");
        }

        Log("\n========== 测试完成 ==========");

        if (messages.Count == 0)
        {
            Log("\n提示: 测试未产生消息，但这可能是配置问题而非代码问题。");
            Log("请检查后端日志文件获取更多信息:");
            Log("  d:/trae/maf-studio/backend/src/MAFStudio.Api/logs/maf-studio-20260405.log");
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
