using MAFStudio.Application.Services;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAFStudio.Tests.Services;

public class GroupChatConclusionServiceTests
{
    private readonly Mock<ICollaborationTaskRepository> _taskRepositoryMock;
    private readonly Mock<IWorkspaceService> _workspaceServiceMock;
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly Mock<ILogger<GroupChatConclusionService>> _loggerMock;
    private readonly GroupChatConclusionService _service;

    public GroupChatConclusionServiceTests()
    {
        _taskRepositoryMock = new Mock<ICollaborationTaskRepository>();
        _workspaceServiceMock = new Mock<IWorkspaceService>();
        _chatClientMock = new Mock<IChatClient>();
        _loggerMock = new Mock<ILogger<GroupChatConclusionService>>();
        
        _service = new GroupChatConclusionService(
            _taskRepositoryMock.Object,
            _workspaceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateAndCommitConclusionAsync_WhenTaskNotFound_ReturnsNull()
    {
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((CollaborationTask?)null);

        var result = await _service.GenerateAndCommitConclusionAsync(
            taskId: 1,
            collaborationId: 100,
            topic: "测试主题",
            messages: new List<Message>(),
            managerAgentId: 1,
            managerAgentName: "测试Agent",
            chatClient: _chatClientMock.Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAndCommitConclusionAsync_WhenTaskPromptIsEmpty_ReturnsNull()
    {
        var task = new CollaborationTask
        {
            Id = 1,
            Prompt = null
        };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(task);

        var result = await _service.GenerateAndCommitConclusionAsync(
            taskId: 1,
            collaborationId: 100,
            topic: "测试主题",
            messages: new List<Message>(),
            managerAgentId: 1,
            managerAgentName: "测试Agent",
            chatClient: _chatClientMock.Object);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAndCommitConclusionAsync_WhenTaskPromptExists_ExecutesTask()
    {
        var task = new CollaborationTask
        {
            Id = 1,
            Prompt = "请生成总结文档并提交到Git仓库"
        };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(task);
        
        _workspaceServiceMock.Setup(s => s.GetAgentDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1");
        _workspaceServiceMock.Setup(s => s.GetAgentRepoDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1/repo");

        var messages = new List<Message>
        {
            new() { FromAgentName = "Agent1", Content = "这是第一条消息", RoundNumber = 1 },
            new() { FromAgentName = "Agent2", Content = "这是第二条消息", RoundNumber = 2 }
        };

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "任务执行成功"));
        _chatClientMock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        var result = await _service.GenerateAndCommitConclusionAsync(
            taskId: 1,
            collaborationId: 100,
            topic: "测试主题",
            messages: messages,
            managerAgentId: 1,
            managerAgentName: "测试Agent",
            chatClient: _chatClientMock.Object);

        Assert.NotNull(result);
        Assert.Equal("任务执行成功", result);
        
        _chatClientMock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAndCommitConclusionAsync_WhenTaskPromptContainsGitInfo_PassesToAgent()
    {
        var gitPrompt = @"请生成总结文档并提交到Git仓库。

【Git配置】
- 仓库: http://192.168.1.250:5100/xxx/test.git
- 分支: main
- Token: ghp_xxxx
- 文档路径: docs/discussions/summary.md";

        var task = new CollaborationTask
        {
            Id = 1,
            Prompt = gitPrompt
        };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(task);
        
        _workspaceServiceMock.Setup(s => s.GetAgentDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1");
        _workspaceServiceMock.Setup(s => s.GetAgentRepoDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1/repo");

        var messages = new List<Message>
        {
            new() { FromAgentName = "Agent1", Content = "讨论内容", RoundNumber = 1 }
        };

        string? capturedPrompt = null;
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Git提交成功"));
        _chatClientMock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((msgs, _, _) =>
            {
                capturedPrompt = msgs.First().Text;
            })
            .ReturnsAsync(chatResponse);

        var result = await _service.GenerateAndCommitConclusionAsync(
            taskId: 1,
            collaborationId: 100,
            topic: "测试主题",
            messages: messages,
            managerAgentId: 1,
            managerAgentName: "测试Agent",
            chatClient: _chatClientMock.Object);

        Assert.NotNull(result);
        Assert.NotNull(capturedPrompt);
        Assert.Contains(gitPrompt, capturedPrompt);
        Assert.Contains("http://192.168.1.250:5100/xxx/test.git", capturedPrompt);
    }

    [Fact]
    public async Task GenerateAndCommitConclusionAsync_WhenTaskPromptContainsEmailInfo_PassesToAgent()
    {
        var emailPrompt = @"请生成总结文档并发送邮件。

【邮件配置】
- 收件人: team@example.com
- 主题: 项目讨论总结
- 附件: summary.md";

        var task = new CollaborationTask
        {
            Id = 1,
            Prompt = emailPrompt
        };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(task);
        
        _workspaceServiceMock.Setup(s => s.GetAgentDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1");
        _workspaceServiceMock.Setup(s => s.GetAgentRepoDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1/repo");

        var messages = new List<Message>
        {
            new() { FromAgentName = "Agent1", Content = "讨论内容", RoundNumber = 1 }
        };

        string? capturedPrompt = null;
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "邮件发送成功"));
        _chatClientMock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((msgs, _, _) =>
            {
                capturedPrompt = msgs.First().Text;
            })
            .ReturnsAsync(chatResponse);

        var result = await _service.GenerateAndCommitConclusionAsync(
            taskId: 1,
            collaborationId: 100,
            topic: "测试主题",
            messages: messages,
            managerAgentId: 1,
            managerAgentName: "测试Agent",
            chatClient: _chatClientMock.Object);

        Assert.NotNull(result);
        Assert.NotNull(capturedPrompt);
        Assert.Contains(emailPrompt, capturedPrompt);
        Assert.Contains("team@example.com", capturedPrompt);
    }

    [Fact]
    public async Task GenerateAndCommitConclusionAsync_WhenChatClientThrows_ReturnsErrorMessage()
    {
        var task = new CollaborationTask
        {
            Id = 1,
            Prompt = "测试提示词"
        };
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(task);
        
        _workspaceServiceMock.Setup(s => s.GetAgentDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1");
        _workspaceServiceMock.Setup(s => s.GetAgentRepoDirById(100, 1, 1))
            .Returns("D:/workspace/100/1/agents/1/repo");

        _chatClientMock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("模型调用失败"));

        var result = await _service.GenerateAndCommitConclusionAsync(
            taskId: 1,
            collaborationId: 100,
            topic: "测试主题",
            messages: new List<Message>(),
            managerAgentId: 1,
            managerAgentName: "测试Agent",
            chatClient: _chatClientMock.Object);

        Assert.NotNull(result);
        Assert.Contains("任务执行失败", result);
        Assert.Contains("模型调用失败", result);
    }
}
