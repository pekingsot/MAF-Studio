using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Services;
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

public class CollaborationWorkflowServiceGroupChatTests
{
    private readonly string _logFile = "D:/trae/test_log.txt";
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
    public async Task ExecuteGroupChatAsync_RealExecution_ShouldReturnMessages()
    {
        File.Delete(_logFile);
        
        var dapperContext = CreateDapperContext();
        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);
        var collaborationRepository = new CollaborationRepository(dapperContext);
        var collaborationAgentRepository = new CollaborationAgentRepository(dapperContext);
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();
        var agentFactoryLogger = loggerFactory.CreateLogger<AgentFactoryService>();
        var workflowServiceLogger = loggerFactory.CreateLogger<CollaborationWorkflowService>();
        
        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);
        
        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory);

        var messageServiceMock = new Mock<IMessageService>();
        var workflowPlanRepoMock = new Mock<IWorkflowPlanRepository>();
        var coordinationSessionRepoMock = new Mock<ICoordinationSessionRepository>();
        var coordinationRoundRepoMock = new Mock<ICoordinationRoundRepository>();
        var coordinationParticipantRepoMock = new Mock<ICoordinationParticipantRepository>();

        var service = new CollaborationWorkflowService(
            collaborationRepository,
            collaborationAgentRepository,
            agentRepository,
            agentFactory,
            messageServiceMock.Object,
            workflowPlanRepoMock.Object,
            coordinationSessionRepoMock.Object,
            coordinationRoundRepoMock.Object,
            coordinationParticipantRepoMock.Object,
            workflowServiceLogger);

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
            "SELECT id FROM collaborations LIMIT 1");

        if (collaborationId == 0)
        {
            Log("数据库中没有协作记录，跳过测试");
            return;
        }

        Log($"测试协作ID: {collaborationId}");

        var messages = new List<ChatMessageDto>();
        var input = "你好，请简单介绍一下你自己";

        Log($"开始执行GroupChat工作流，输入: {input}");

        try
        {
            await foreach (var message in service.ExecuteGroupChatAsync(collaborationId, input))
            {
                messages.Add(message);
                Log($"收到消息: Sender={message.Sender}, Content长度={message.Content?.Length ?? 0}");
                if (!string.IsNullOrEmpty(message.Content))
                {
                    Log($"Content前100字符: {message.Content.Substring(0, Math.Min(100, message.Content.Length))}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"执行出错: {ex.Message}");
            Log($"堆栈: {ex.StackTrace}");
        }

        Log($"总共收到 {messages.Count} 条消息");
        
        foreach (var msg in messages)
        {
            Log($"消息详情: Sender={msg.Sender}, Role={msg.Role}, Content长度={msg.Content?.Length ?? 0}");
        }

        Assert.NotEmpty(messages);
        Assert.All(messages, m =>
        {
            Assert.NotNull(m.Sender);
            Assert.NotNull(m.Content);
            Assert.False(string.IsNullOrEmpty(m.Content), "消息内容不应为空");
        });
    }
}
