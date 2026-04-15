using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MAFStudio.Api.Controllers;
using MAFStudio.Application.DTOs.Requests;
using MAFStudio.Application.Services;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Xunit;

namespace MAFStudio.Tests.Controllers;

public class CollaborationsControllerTests : TestBase
{
    private readonly Mock<ICollaborationService> _mockCollaborationService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IOperationLogService> _mockLogService;
    private readonly Mock<IGroupMessageRepository> _mockGroupMessageRepository;
    private readonly Mock<ICollaborationTaskRepository> _mockCollaborationTaskRepository;
    private readonly Mock<ILogger<CollaborationsController>> _mockLogger;
    private readonly CollaborationsController _controller;
    private readonly long _testUserId = 1000000000000001;

    public CollaborationsControllerTests() : base()
    {
        _mockCollaborationService = new Mock<ICollaborationService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();
        _mockGroupMessageRepository = new Mock<IGroupMessageRepository>();
        _mockCollaborationTaskRepository = new Mock<ICollaborationTaskRepository>();
        _mockLogger = new Mock<ILogger<CollaborationsController>>();

        _controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            Mock.Of<MAFStudio.Application.Interfaces.IAgentFactoryService>(),
            Mock.Of<MAFStudio.Core.Interfaces.Repositories.IAgentRepository>(),
            Mock.Of<MAFStudio.Core.Interfaces.Repositories.ICollaborationAgentRepository>(),
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAllCollaborations_ShouldReturnOkWithCollaborations()
    {
        var collaboration1 = CreateTestCollaboration("Collaboration1", _testUserId);
        var collaboration2 = CreateTestCollaboration("Collaboration2", _testUserId);

        _mockCollaborationService
            .Setup(s => s.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Collaboration> { collaboration1, collaboration2 });

        var result = await _controller.GetAllCollaborations();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var collaborations = Assert.IsType<List<Application.VOs.CollaborationVo>>(okResult.Value);
        Assert.Equal(2, collaborations.Count);
    }

    [Fact]
    public async Task GetAllCollaborations_ShouldReturnCollaborationsWithoutTasks()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1009;

        _mockCollaborationService
            .Setup(s => s.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Collaboration> { collaboration });

        var result = await _controller.GetAllCollaborations();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var collaborations = Assert.IsType<List<Application.VOs.CollaborationVo>>(okResult.Value);
        Assert.Single(collaborations);
        Assert.Empty(collaborations[0].Tasks);
    }

    [Fact]
    public async Task GetCollaboration_ExistingId_ShouldReturnOkWithCollaboration()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1001;

        _mockCollaborationService
            .Setup(s => s.GetByIdAsync(collaboration.Id, _testUserId))
            .ReturnsAsync(collaboration);

        _mockCollaborationService
            .Setup(s => s.GetAgentsAsync(collaboration.Id))
            .ReturnsAsync(new List<CollaborationAgent>());

        var result = await _controller.GetCollaboration(collaboration.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var vo = Assert.IsType<Application.VOs.CollaborationVo>(okResult.Value);
        Assert.Equal("Collaboration1", vo.Name);
    }

    [Fact]
    public async Task GetCollaboration_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = 999999L;

        _mockCollaborationService
            .Setup(s => s.GetByIdAsync(nonExistingId, _testUserId))
            .ReturnsAsync((Collaboration?)null);

        var result = await _controller.GetCollaboration(nonExistingId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateCollaboration_ValidRequest_ShouldReturnCreatedAtAction()
    {
        var request = new CreateCollaborationRequest
        {
            Name = "New Collaboration",
            Description = "Test Description"
        };

        var createdCollaboration = CreateTestCollaboration(request.Name, _testUserId);

        _mockCollaborationService
            .Setup(s => s.CreateAsync(
                request.Name, request.Description, request.Path,
                request.GitRepositoryUrl, request.GitBranch, request.GitUsername,
                request.GitEmail, request.GitAccessToken, request.Config, _testUserId))
            .ReturnsAsync(createdCollaboration);

        var result = await _controller.CreateCollaboration(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedCollaboration = Assert.IsType<Collaboration>(createdResult.Value);
        Assert.Equal(request.Name, returnedCollaboration.Name);
    }

    [Fact]
    public async Task DeleteCollaboration_ExistingId_ShouldReturnNoContent()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1002;

        _mockCollaborationService
            .Setup(s => s.DeleteAsync(collaboration.Id, _testUserId))
            .ReturnsAsync(true);

        var result = await _controller.DeleteCollaboration(collaboration.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCollaboration_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = 999999L;

        _mockCollaborationService
            .Setup(s => s.DeleteAsync(nonExistingId, _testUserId))
            .ReturnsAsync(false);

        var result = await _controller.DeleteCollaboration(nonExistingId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddAgentToCollaboration_ValidRequest_ShouldReturnOk()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1003;
        
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.Id = 2001;

        var request = new AddAgentRequest
        {
            AgentId = agent.Id,
            Role = "Assistant"
        };

        _mockCollaborationService
            .Setup(s => s.AddAgentAsync(collaboration.Id, request.AgentId, request.Role, null, _testUserId))
            .ReturnsAsync(true);

        var result = await _controller.AddAgentToCollaboration(collaboration.Id, request);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemoveAgentFromCollaboration_ValidRequest_ShouldReturnNoContent()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1004;
        
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.Id = 2002;

        _mockCollaborationService
            .Setup(s => s.RemoveAgentAsync(collaboration.Id, agent.Id, _testUserId))
            .ReturnsAsync(true);

        var result = await _controller.RemoveAgentFromCollaboration(collaboration.Id, agent.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CreateTask_ValidRequest_ShouldReturnCreatedAtAction()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1005;

        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Task Description",
            Prompt = "【任务要求】请各位团队成员积极参与讨论。"
        };

        var createdTask = CreateTestTask(collaboration.Id, request.Title, request.Prompt);

        _mockCollaborationService
            .Setup(s => s.CreateTaskAsync(collaboration.Id, request.Title, request.Description, _testUserId, request.Prompt, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<long>?>(), It.IsAny<string?>()))
            .ReturnsAsync(createdTask);

        var result = await _controller.CreateTask(collaboration.Id, request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(createdResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
        Assert.Equal(request.Prompt, returnedTask.Prompt);
    }

    [Fact]
    public async Task CreateTask_WithPrompt_ShouldSavePrompt()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1006;

        var request = new CreateTaskRequest
        {
            Title = "Task With Prompt",
            Description = "Task Description",
            Prompt = "【任务要求】\n1. 积极参与讨论\n2. 提交文档到Git\n3. 必须真实调用工具"
        };

        var createdTask = CreateTestTask(collaboration.Id, request.Title, request.Prompt);

        _mockCollaborationService
            .Setup(s => s.CreateTaskAsync(collaboration.Id, request.Title, request.Description, _testUserId, request.Prompt, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<long>?>(), It.IsAny<string?>()))
            .ReturnsAsync(createdTask);

        var result = await _controller.CreateTask(collaboration.Id, request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(createdResult.Value);
        Assert.Equal(request.Prompt, returnedTask.Prompt);
    }

    [Fact]
    public async Task UpdateTask_ValidRequest_ShouldReturnOk()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1007;
        
        var task = CreateTestTask(collaboration.Id, "Original Task");
        task.Id = 3002;

        var request = new UpdateTaskRequest
        {
            Title = "Updated Task",
            Description = "Updated Description",
            Prompt = "【更新后的任务要求】请重新讨论并提交文档。"
        };

        var updatedTask = new CollaborationTask
        {
            Id = task.Id,
            CollaborationId = task.CollaborationId,
            Title = request.Title,
            Description = request.Description,
            Prompt = request.Prompt,
            Status = CollaborationTaskStatus.Pending,
            CreatedAt = task.CreatedAt
        };

        _mockCollaborationService
            .Setup(s => s.UpdateTaskAsync(task.Id, request.Title, request.Description, request.Prompt, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<long>?>(), It.IsAny<string?>()))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateTask(task.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(okResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
        Assert.Equal(request.Prompt, returnedTask.Prompt);
    }

    [Fact]
    public async Task UpdateTask_WithPrompt_ShouldUpdatePrompt()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1008;
        
        var task = CreateTestTask(collaboration.Id, "Task1", "Old Prompt");
        task.Id = 3003;

        var request = new UpdateTaskRequest
        {
            Title = "Task1",
            Description = "Description",
            Prompt = "【新的任务要求】\n- 必须提交文档\n- 必须调用Git工具"
        };

        var updatedTask = new CollaborationTask
        {
            Id = task.Id,
            CollaborationId = task.CollaborationId,
            Title = request.Title,
            Description = request.Description,
            Prompt = request.Prompt,
            Status = CollaborationTaskStatus.Pending,
            CreatedAt = task.CreatedAt
        };

        _mockCollaborationService
            .Setup(s => s.UpdateTaskAsync(task.Id, request.Title, request.Description, request.Prompt, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<long>?>(), It.IsAny<string?>()))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateTask(task.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(okResult.Value);
        Assert.Equal(request.Prompt, returnedTask.Prompt);
        Assert.NotEqual("Old Prompt", returnedTask.Prompt);
    }

    [Fact]
    public async Task UpdateTaskStatus_ValidRequest_ShouldReturnOk()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1006;
        
        var task = CreateTestTask(collaboration.Id, "Task1");
        task.Id = 3001;

        var request = new UpdateTaskStatusRequest
        {
            Status = "InProgress"
        };

        var updatedTask = new CollaborationTask
        {
            Id = task.Id,
            CollaborationId = task.CollaborationId,
            Title = task.Title,
            Status = CollaborationTaskStatus.InProgress,
            CreatedAt = task.CreatedAt
        };

        _mockCollaborationService
            .Setup(s => s.UpdateTaskStatusAsync(task.Id, CollaborationTaskStatus.InProgress, _testUserId))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateTaskStatus(task.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(okResult.Value);
        Assert.Equal(CollaborationTaskStatus.InProgress, returnedTask.Status);
    }
    
    [Fact]
    public async Task CreateCollaboration_WithConfig_ShouldSaveConfig()
    {
        var config = "{\"smtp\":{\"server\":\"smtp.qq.com\",\"port\":587,\"username\":\"test@qq.com\",\"password\":\"test123\",\"fromEmail\":\"test@qq.com\",\"enableSsl\":true}}";
        
        var request = new CreateCollaborationRequest
        {
            Name = "Test Collaboration with Config",
            Description = "Test Description",
            Config = config
        };

        var createdCollaboration = CreateTestCollaboration(request.Name, _testUserId);
        createdCollaboration.Config = config;

        _mockCollaborationService
            .Setup(s => s.CreateAsync(
                request.Name, request.Description, request.Path,
                request.GitRepositoryUrl, request.GitBranch, request.GitUsername,
                request.GitEmail, request.GitAccessToken, request.Config, _testUserId))
            .ReturnsAsync(createdCollaboration);

        var result = await _controller.CreateCollaboration(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedCollaboration = Assert.IsType<Collaboration>(createdResult.Value);
        Assert.Equal(request.Name, returnedCollaboration.Name);
        Assert.Equal(config, returnedCollaboration.Config);
        
        _mockCollaborationService.Verify(s => s.CreateAsync(
            request.Name, request.Description, request.Path,
            request.GitRepositoryUrl, request.GitBranch, request.GitUsername,
            request.GitEmail, request.GitAccessToken, request.Config, _testUserId), Times.Once);
    }
    
    [Fact]
    public async Task UpdateCollaboration_WithConfig_ShouldUpdateConfig()
    {
        var collaborationId = 1003L;
        var config = "{\"smtp\":{\"server\":\"smtp.test.com\",\"port\":587,\"username\":\"user@test.com\",\"password\":\"pass123\",\"fromEmail\":\"user@test.com\",\"enableSsl\":true}}";
        
        var request = new CreateCollaborationRequest
        {
            Name = "Updated Collaboration",
            Description = "Updated Description",
            Config = config
        };

        var existingCollaboration = CreateTestCollaboration("Old Name", _testUserId);
        existingCollaboration.Id = collaborationId;

        var updatedCollaboration = CreateTestCollaboration(request.Name, _testUserId);
        updatedCollaboration.Id = collaborationId;
        updatedCollaboration.Description = request.Description;
        updatedCollaboration.Config = config;

        _mockCollaborationService
            .Setup(s => s.GetByIdAsync(collaborationId, _testUserId))
            .ReturnsAsync(existingCollaboration);
        
        _mockCollaborationService
            .Setup(s => s.UpdateAsync(It.IsAny<Collaboration>()))
            .ReturnsAsync(updatedCollaboration);

        var result = await _controller.UpdateCollaboration(collaborationId, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCollaboration = Assert.IsType<Collaboration>(okResult.Value);
        Assert.Equal(request.Name, returnedCollaboration.Name);
        Assert.Equal(config, returnedCollaboration.Config);
    }

    [Fact]
    public async Task CreateTask_WithConfig_ShouldSaveConfig()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1009;

        var config = "{\"workflowType\":\"GroupChat\",\"orchestrationMode\":\"Manager\",\"maxIterations\":10,\"managerAgentId\":123,\"managerCustomPrompt\":\"这是一个测试提示词\"}";

        var request = new CreateTaskRequest
        {
            Title = "Task With Config",
            Description = "Task Description",
            Config = config
        };

        var createdTask = new CollaborationTask
        {
            Id = 4001,
            CollaborationId = collaboration.Id,
            Title = request.Title,
            Description = request.Description,
            Config = config,
            Status = CollaborationTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _mockCollaborationService
            .Setup(s => s.CreateTaskAsync(
                collaboration.Id, 
                request.Title, 
                request.Description, 
                _testUserId, 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<List<long>?>(), 
                It.IsAny<string?>()))
            .ReturnsAsync(createdTask);

        var result = await _controller.CreateTask(collaboration.Id, request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(createdResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
        Assert.Equal(config, returnedTask.Config);
    }

    [Fact]
    public async Task UpdateTask_WithConfig_ShouldUpdateConfig()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1010;
        
        var task = CreateTestTask(collaboration.Id, "Task1");
        task.Id = 4002;

        var config = "{\"workflowType\":\"ReviewIterative\",\"orchestrationMode\":\"Manager\",\"maxIterations\":15,\"managerAgentId\":456,\"managerCustomPrompt\":\"更新后的提示词\"}";

        var request = new UpdateTaskRequest
        {
            Title = "Task1",
            Description = "Description",
            Config = config
        };

        var updatedTask = new CollaborationTask
        {
            Id = task.Id,
            CollaborationId = task.CollaborationId,
            Title = request.Title,
            Description = request.Description,
            Config = config,
            Status = CollaborationTaskStatus.Pending,
            CreatedAt = task.CreatedAt
        };

        _mockCollaborationService
            .Setup(s => s.UpdateTaskAsync(
                task.Id, 
                request.Title, 
                request.Description, 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<string?>(), 
                It.IsAny<List<long>?>(), 
                It.IsAny<string?>()))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateTask(task.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(okResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
        Assert.Equal(config, returnedTask.Config);
    }

    [Fact]
    public async Task Chat_WithMention_ShouldRouteToMentionedAgent()
    {
        var managerAgentId = 100L;
        var workerAgentId = 200L;

        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" },
            new() { AgentId = workerAgentId, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var workerEntity = new Agent { Id = workerAgentId, Name = "架构师", TypeName = "Architect", SystemPrompt = "你是架构师" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(workerAgentId))
            .ReturnsAsync(workerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();

        var mockChatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(), It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.Assistant, "我是架构师，我来回答")
            }));

        mockFactory
            .Setup(f => f.CreateAgentAsync(workerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(new List<GroupMessage>());

        _mockGroupMessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .ReturnsAsync((GroupMessage msg) => { msg.Id = 1; return msg; });

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            mockFactory.Object,
            mockAgentRepo.Object,
            mockCollabAgentRepo.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new CollaborationChatRequest
        {
            Content = "@架构师 请分析一下",
            MentionedAgentIds = new List<string> { "200" }
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        mockFactory.Verify(f => f.CreateAgentAsync(workerAgentId), Times.Once);
        mockFactory.Verify(f => f.CreateAgentAsync(managerAgentId), Times.Never);
        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.IsAny<GroupMessage>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Chat_WithoutMention_ShouldRouteToManager()
    {
        var managerAgentId = 100L;
        var workerAgentId = 200L;

        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" },
            new() { AgentId = workerAgentId, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var managerEntity = new Agent { Id = managerAgentId, Name = "协调者", TypeName = "Manager", SystemPrompt = "你是协调者" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();

        var mockChatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(), It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.Assistant, "我是协调者，我来协调")
            }));

        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(new List<GroupMessage>());

        _mockGroupMessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .ReturnsAsync((GroupMessage msg) => { msg.Id = 1; return msg; });

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            mockFactory.Object,
            mockAgentRepo.Object,
            mockCollabAgentRepo.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new CollaborationChatRequest
        {
            Content = "大家好，讨论一下",
            MentionedAgentIds = null
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        mockFactory.Verify(f => f.CreateAgentAsync(managerAgentId), Times.Once);
        mockFactory.Verify(f => f.CreateAgentAsync(workerAgentId), Times.Never);
        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.IsAny<GroupMessage>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Chat_WithContext_ShouldIncludeRecentHistory()
    {
        var managerAgentId = 100L;

        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var managerEntity = new Agent { Id = managerAgentId, Name = "协调者", TypeName = "Manager", SystemPrompt = "你是协调者" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var existingMessages = new List<GroupMessage>
        {
            new() { Id = 1, Content = "你好", SenderType = "User", FromAgentName = "我", CollaborationId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = 2, Content = "你好！有什么可以帮你的？", SenderType = "Agent", FromAgentName = "协调者", FromAgentId = managerAgentId, CollaborationId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-4) },
        };

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(existingMessages);

        _mockGroupMessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .ReturnsAsync((GroupMessage msg) => { msg.Id = 3; return msg; });

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();

        List<ChatMessage> capturedHistory = null!;
        var mockChatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(), It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, CancellationToken>((msgs, _, _) =>
            {
                capturedHistory = msgs.ToList();
            })
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.Assistant, "我记得你之前说过你好")
            }));

        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            mockFactory.Object,
            mockAgentRepo.Object,
            mockCollabAgentRepo.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new CollaborationChatRequest
        {
            Content = "你还记得我刚才说了什么吗？",
            MentionedAgentIds = null
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.NotNull(capturedHistory);
        Assert.True(capturedHistory.Count >= 4);
        Assert.Contains(capturedHistory, m => m.Text == "你好" && m.Role == Microsoft.Extensions.AI.ChatRole.User);
        Assert.Contains(capturedHistory, m => m.Text == "你好！有什么可以帮你的？" && m.Role == Microsoft.Extensions.AI.ChatRole.Assistant);
    }

    [Fact]
    public async Task GetChatHistory_ShouldReturnMessages()
    {
        var existingMessages = new List<GroupMessage>
        {
            new() { Id = 1, Content = "消息1", SenderType = "User", FromAgentName = "我", CollaborationId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { Id = 2, Content = "回复1", SenderType = "Agent", FromAgentName = "协调者", FromAgentId = 100, CollaborationId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-9) },
            new() { Id = 3, Content = "消息2", SenderType = "User", FromAgentName = "我", CollaborationId = 1, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
        };

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(existingMessages);

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            Mock.Of<MAFStudio.Application.Interfaces.IAgentFactoryService>(),
            Mock.Of<IAgentRepository>(),
            Mock.Of<ICollaborationAgentRepository>(),
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var result = await controller.GetChatHistory(1L, 20, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Chat_ShouldPersistUserAndAgentMessages()
    {
        var managerAgentId = 100L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var managerEntity = new Agent { Id = managerAgentId, Name = "协调者", TypeName = "Manager", SystemPrompt = "你是协调者", Avatar = "🤖" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(), It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.Assistant, "这是回复")
            }));
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(new List<GroupMessage>());

        _mockGroupMessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .ReturnsAsync((GroupMessage msg) => { msg.Id = DateTime.UtcNow.Ticks; return msg; });

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            mockFactory.Object,
            mockAgentRepo.Object,
            mockCollabAgentRepo.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new CollaborationChatRequest
        {
            Content = "你好"
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "User" && m.Content == "你好" && m.CollaborationId == 1L
        )), Times.Once, "用户消息应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.Content == "这是回复" && m.FromAgentId == managerAgentId && m.FromAgentName == "协调者"
        )), Times.Once, "Agent回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.IsAny<GroupMessage>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Chat_ShouldSaveAgentAvatar()
    {
        var managerAgentId = 100L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var managerEntity = new Agent { Id = managerAgentId, Name = "协调者", TypeName = "Manager", SystemPrompt = "你是协调者", Avatar = "https://example.com/avatar.png" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(), It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.Assistant, "回复")
            }));
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(new List<GroupMessage>());

        _mockGroupMessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .ReturnsAsync((GroupMessage msg) => { msg.Id = DateTime.UtcNow.Ticks; return msg; });

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            mockFactory.Object,
            mockAgentRepo.Object,
            mockCollabAgentRepo.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new CollaborationChatRequest { Content = "你好" };
        await controller.Chat(1L, request, CancellationToken.None);

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.FromAgentAvatar == "https://example.com/avatar.png"
        )), Times.Once, "Agent头像应该保存到消息中");
    }

    [Fact]
    public async Task Chat_ShouldIncludeHistoryInContext()
    {
        var managerAgentId = 100L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var managerEntity = new Agent { Id = managerAgentId, Name = "协调者", TypeName = "Manager", SystemPrompt = "你是协调者" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var historyMessages = new List<GroupMessage>
        {
            new() { Id = 1, SenderType = "User", Content = "之前的问题", CollaborationId = 1 },
            new() { Id = 2, SenderType = "Agent", Content = "之前的回答", CollaborationId = 1 },
        };

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
            .ReturnsAsync(historyMessages);

        _mockGroupMessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .ReturnsAsync((GroupMessage msg) => { msg.Id = DateTime.UtcNow.Ticks; return msg; });

        List<Microsoft.Extensions.AI.ChatMessage> capturedMessages = null!;
        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<Microsoft.Extensions.AI.IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<Microsoft.Extensions.AI.ChatMessage>>(), It.IsAny<Microsoft.Extensions.AI.ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, CancellationToken>((msgs, _, _) =>
            {
                capturedMessages = msgs.ToList();
            })
            .ReturnsAsync(new Microsoft.Extensions.AI.ChatResponse(new List<Microsoft.Extensions.AI.ChatMessage>
            {
                new(Microsoft.Extensions.AI.ChatRole.Assistant, "基于历史的回复")
            }));
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            mockFactory.Object,
            mockAgentRepo.Object,
            mockCollabAgentRepo.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var request = new CollaborationChatRequest { Content = "新问题" };
        await controller.Chat(1L, request, CancellationToken.None);

        Assert.NotNull(capturedMessages);
        var userMsgs = capturedMessages.Where(m => m.Role == Microsoft.Extensions.AI.ChatRole.User).ToList();
        var assistantMsgs = capturedMessages.Where(m => m.Role == Microsoft.Extensions.AI.ChatRole.Assistant).ToList();

        Assert.True(userMsgs.Any(m => m.Text.Contains("之前的问题")), "历史用户消息应包含在上下文中");
        Assert.True(assistantMsgs.Any(m => m.Text.Contains("之前的回答")), "历史Agent回复应包含在上下文中");
        Assert.True(userMsgs.Any(m => m.Text.Contains("新问题")), "当前用户消息应包含在上下文中");
    }

    [Fact]
    public async Task GetChatHistory_WithPagination_ShouldPassBeforeId()
    {
        var existingMessages = new List<GroupMessage>
        {
            new() { Id = 5, Content = "消息5", SenderType = "User", CollaborationId = 1, CreatedAt = DateTime.UtcNow },
        };

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 20, 10L))
            .ReturnsAsync(existingMessages);

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            Mock.Of<MAFStudio.Application.Interfaces.IAgentFactoryService>(),
            Mock.Of<IAgentRepository>(),
            Mock.Of<ICollaborationAgentRepository>(),
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var result = await controller.GetChatHistory(1L, 20, 10L);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockGroupMessageRepository.Verify(r => r.GetByCollaborationIdAsync(1L, 20, 10L), Times.Once);
    }
}
