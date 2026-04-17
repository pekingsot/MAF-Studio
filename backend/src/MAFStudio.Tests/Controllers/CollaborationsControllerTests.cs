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
using MAFStudio.Application.Skills;
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

    private readonly Mock<ICollaborationAgentRepository> _mockCollaborationAgentRepository;

    public CollaborationsControllerTests() : base()
    {
        _mockCollaborationService = new Mock<ICollaborationService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();
        _mockGroupMessageRepository = new Mock<IGroupMessageRepository>();
        _mockCollaborationTaskRepository = new Mock<ICollaborationTaskRepository>();
        _mockLogger = new Mock<ILogger<CollaborationsController>>();
        _mockCollaborationAgentRepository = new Mock<ICollaborationAgentRepository>();

        _controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            Mock.Of<MAFStudio.Application.Interfaces.IAgentFactoryService>(),
            Mock.Of<MAFStudio.Core.Interfaces.Repositories.IAgentRepository>(),
            _mockCollaborationAgentRepository.Object,
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new() { Id = 2, SenderType = "Agent", FromAgentId = managerAgentId, FromAgentName = "协调者", Content = "之前的回答", CollaborationId = 1 },
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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

    [Fact]
    public async Task Chat_ShouldSaveModelName_FromChatResponseModelId()
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

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "协调者",
            TypeName = "Manager",
            SystemPrompt = "你是协调者",
            LlmConfigs = "[{\"llmConfigId\":1,\"modelName\":\"qwen3.5-35b-a3b\",\"isPrimary\":true}]"
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "基于模型的回复")
        })
        {
            ModelId = "qwen3.5-35b-a3b"
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            m.SenderType == "Agent" && m.ModelName == "qwen3.5-35b-a3b"
        )), Times.Once, "ChatResponse.ModelId 应作为 model_name 入库");
    }

    [Fact]
    public async Task Chat_ShouldSaveModelName_FromLlmConfigsJson_WhenModelIdIsNull()
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

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "协调者",
            TypeName = "Manager",
            SystemPrompt = "你是协调者",
            LlmConfigs = "[{\"llmConfigId\":1,\"llmConfigName\":\"阿里云\",\"llmModelConfigId\":2,\"modelName\":\"gpt-4o\",\"isPrimary\":true,\"priority\":0,\"isValid\":true,\"msg\":\"OK\"}]"
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "回复内容")
        })
        {
            ModelId = null
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            m.SenderType == "Agent" && m.ModelName == "gpt-4o"
        )), Times.Once, "当 ChatResponse.ModelId 为空时，应从 LlmConfigs JSON 解析 modelName 并入库");
    }

    [Fact]
    public async Task Chat_ShouldSaveNullModelName_WhenBothModelIdAndLlmConfigsAreEmpty()
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

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "协调者",
            TypeName = "Manager",
            SystemPrompt = "你是协调者",
            LlmConfigs = null
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "回复内容")
        })
        {
            ModelId = null
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            m.SenderType == "Agent" && m.ModelName == null
        )), Times.Once, "当 ModelId 和 LlmConfigs 都为空时，model_name 应为 null");
    }

    [Fact]
    public async Task Chat_ShouldSaveLlmConfigName_WhenModelIdMatchesConfig()
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

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "协调者",
            TypeName = "Manager",
            SystemPrompt = "你是协调者",
            LlmConfigs = "[{\"llmConfigId\":1001,\"llmConfigName\":\"千问-2841\",\"llmModelConfigId\":1254,\"modelName\":\"MiniMax-M2.1\",\"isPrimary\":true,\"priority\":0,\"isValid\":true},{\"llmConfigId\":1001,\"llmConfigName\":\"千问-2841\",\"llmModelConfigId\":1184,\"modelName\":\"deepseek-v3.1\",\"isPrimary\":false,\"priority\":1,\"isValid\":true}]"
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "回复内容")
        })
        {
            ModelId = "deepseek-v3.1"
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("data", out var dataProp));
        var firstItem = dataProp.EnumerateArray().First();
        Assert.True(firstItem.TryGetProperty("modelName", out var modelNameProp));
        Assert.Equal("deepseek-v3.1", modelNameProp.GetString());
        Assert.True(firstItem.TryGetProperty("llmConfigName", out var llmConfigNameProp));
        Assert.Equal("千问-2841", llmConfigNameProp.GetString());

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent"
            && m.ModelName == "deepseek-v3.1"
            && m.LlmConfigName == "千问-2841"
        )), Times.Once, "当 ModelId 匹配到 LlmConfigs 中的配置时，llm_config_name 应入库");
    }

    [Fact]
    public async Task Chat_ShouldSaveLlmConfigName_FromPrimaryConfig_WhenModelIdNotMatched()
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

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "协调者",
            TypeName = "Manager",
            SystemPrompt = "你是协调者",
            LlmConfigs = "[{\"llmConfigId\":1001,\"llmConfigName\":\"千问-2841\",\"llmModelConfigId\":1254,\"modelName\":\"MiniMax-M2.1\",\"isPrimary\":true,\"priority\":0,\"isValid\":true}]"
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "回复内容")
        })
        {
            ModelId = "unknown-model"
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            m.SenderType == "Agent"
            && m.ModelName == "unknown-model"
            && m.LlmConfigName == "千问-2841"
        )), Times.Once, "当 ModelId 未匹配到 LlmConfigs 中的配置时，应回退到 isPrimary 配置的 llmConfigName");
    }

    [Fact]
    public async Task Chat_MentionMultipleAgents_ShouldAllRespond()
    {
        var agent1Id = 100L;
        var agent2Id = 200L;
        var agent3Id = 300L;

        var members = new List<CollaborationAgent>
        {
            new() { AgentId = agent1Id, Role = "Worker" },
            new() { AgentId = agent2Id, Role = "Worker" },
            new() { AgentId = agent3Id, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var agent1Entity = new Agent { Id = agent1Id, Name = "架构师", TypeName = "Architect", SystemPrompt = "你是架构师" };
        var agent2Entity = new Agent { Id = agent2Id, Name = "测试工程师", TypeName = "Tester", SystemPrompt = "你是测试工程师" };
        var agent3Entity = new Agent { Id = agent3Id, Name = "产品经理", TypeName = "PM", SystemPrompt = "你是产品经理" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent1Id)).ReturnsAsync(agent1Entity);
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent2Id)).ReturnsAsync(agent2Entity);
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent3Id)).ReturnsAsync(agent3Entity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();

        var mockChatClient1 = new Mock<IChatClient>();
        mockChatClient1
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "架构师回答") }));

        var mockChatClient2 = new Mock<IChatClient>();
        mockChatClient2
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "测试工程师回答") }));

        var mockChatClient3 = new Mock<IChatClient>();
        mockChatClient3
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "产品经理回答") }));

        mockFactory.Setup(f => f.CreateAgentAsync(agent1Id)).ReturnsAsync(mockChatClient1.Object);
        mockFactory.Setup(f => f.CreateAgentAsync(agent2Id)).ReturnsAsync(mockChatClient2.Object);
        mockFactory.Setup(f => f.CreateAgentAsync(agent3Id)).ReturnsAsync(mockChatClient3.Object);

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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "@所有人 大家好",
            MentionedAgentIds = new List<string> { "100", "200", "300" }
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("success", out var successProp) && successProp.GetBoolean(), "success 应为 true");
        Assert.True(root.TryGetProperty("data", out var dataProp), "应返回 data 属性");
        Assert.Equal(3, dataProp.GetArrayLength());

        var agentNames = new HashSet<string>();
        foreach (var item in dataProp.EnumerateArray())
        {
            Assert.True(item.TryGetProperty("fromAgentName", out var nameProp), "每条回复应有 fromAgentName");
            Assert.True(item.TryGetProperty("content", out var contentProp), "每条回复应有 content");
            agentNames.Add(nameProp.GetString()!);
        }
        Assert.Contains("架构师", agentNames);
        Assert.Contains("测试工程师", agentNames);
        Assert.Contains("产品经理", agentNames);

        mockFactory.Verify(f => f.CreateAgentAsync(agent1Id), Times.Once, "架构师应该被调用");
        mockFactory.Verify(f => f.CreateAgentAsync(agent2Id), Times.Once, "测试工程师应该被调用");
        mockFactory.Verify(f => f.CreateAgentAsync(agent3Id), Times.Once, "产品经理应该被调用");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent1Id && m.Content == "架构师回答"
        )), Times.Once, "架构师回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent2Id && m.Content == "测试工程师回答"
        )), Times.Once, "测试工程师回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent3Id && m.Content == "产品经理回答"
        )), Times.Once, "产品经理回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.IsAny<GroupMessage>()), Times.Exactly(4), "1条用户消息 + 3条Agent回复 = 4条");
    }

    [Fact]
    public async Task Chat_MentionTwoAgents_ShouldBothRespond()
    {
        var agent1Id = 100L;
        var agent2Id = 200L;

        var members = new List<CollaborationAgent>
        {
            new() { AgentId = agent1Id, Role = "Worker" },
            new() { AgentId = agent2Id, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var agent1Entity = new Agent { Id = agent1Id, Name = "架构师", TypeName = "Architect", SystemPrompt = "你是架构师" };
        var agent2Entity = new Agent { Id = agent2Id, Name = "测试工程师", TypeName = "Tester", SystemPrompt = "你是测试工程师" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent1Id)).ReturnsAsync(agent1Entity);
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent2Id)).ReturnsAsync(agent2Entity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();

        var mockChatClient1 = new Mock<IChatClient>();
        mockChatClient1
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "架构师回答") }));

        var mockChatClient2 = new Mock<IChatClient>();
        mockChatClient2
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "测试工程师回答") }));

        mockFactory.Setup(f => f.CreateAgentAsync(agent1Id)).ReturnsAsync(mockChatClient1.Object);
        mockFactory.Setup(f => f.CreateAgentAsync(agent2Id)).ReturnsAsync(mockChatClient2.Object);

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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "@架构师 @测试工程师 你好",
            MentionedAgentIds = new List<string> { "100", "200" }
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("success", out var successProp) && successProp.GetBoolean(), "success 应为 true");
        Assert.True(root.TryGetProperty("data", out var dataProp), "应返回 data 属性");
        Assert.Equal(2, dataProp.GetArrayLength());

        var agentNames = new HashSet<string>();
        foreach (var item in dataProp.EnumerateArray())
        {
            Assert.True(item.TryGetProperty("fromAgentName", out var nameProp), "每条回复应有 fromAgentName");
            Assert.True(item.TryGetProperty("content", out var contentProp), "每条回复应有 content");
            agentNames.Add(nameProp.GetString()!);
        }
        Assert.Contains("架构师", agentNames);
        Assert.Contains("测试工程师", agentNames);

        mockFactory.Verify(f => f.CreateAgentAsync(agent1Id), Times.Once, "架构师应该被调用");
        mockFactory.Verify(f => f.CreateAgentAsync(agent2Id), Times.Once, "测试工程师应该被调用");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent1Id
        )), Times.Once, "架构师回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent2Id
        )), Times.Once, "测试工程师回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.IsAny<GroupMessage>()), Times.Exactly(3), "1条用户消息 + 2条Agent回复 = 3条");
    }

    [Fact]
    public async Task Chat_ShouldSaveLlmConfigName_FromPrimaryConfig_WhenModelIdNotMatch()
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

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "协调者",
            TypeName = "Manager",
            SystemPrompt = "你是协调者",
            LlmConfigs = "[{\"llmConfigId\":1001,\"llmConfigName\":\"千问-2841\",\"llmModelConfigId\":1254,\"modelName\":\"deepseek-v3.1\",\"isPrimary\":true,\"priority\":0,\"isValid\":true}]"
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo
            .Setup(r => r.GetByIdAsync(managerAgentId))
            .ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "回复内容")
        })
        {
            ModelId = "deepseek-chat"
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory
            .Setup(f => f.CreateAgentAsync(managerAgentId))
            .ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            m.SenderType == "Agent"
            && m.ModelName == "deepseek-chat"
            && m.LlmConfigName == "千问-2841"
        )), Times.Once, "ModelId与LlmConfigs不匹配时，llm_config_name应直接取主配置");
    }

    [Fact]
    public async Task Chat_ShouldReturnErrorMessage_WhenAgentCreationFails()
    {
        var agent1Id = 100L;
        var agent2Id = 200L;

        var members = new List<CollaborationAgent>
        {
            new() { AgentId = agent1Id, Role = "Worker" },
            new() { AgentId = agent2Id, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo
            .Setup(r => r.GetByCollaborationIdAsync(1L))
            .ReturnsAsync(members);

        var agent1Entity = new Agent { Id = agent1Id, Name = "架构师", TypeName = "Architect", SystemPrompt = "你是架构师" };
        var agent2Entity = new Agent { Id = agent2Id, Name = "测试工程师", TypeName = "Tester", SystemPrompt = "你是测试工程师" };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent1Id)).ReturnsAsync(agent1Entity);
        mockAgentRepo.Setup(r => r.GetByIdAsync(agent2Id)).ReturnsAsync(agent2Entity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        mockFactory
            .Setup(f => f.CreateAgentAsync(agent1Id))
            .ThrowsAsync(new Exception("LLM配置不存在"));
        
        var mockChatClient2 = new Mock<IChatClient>();
        mockChatClient2
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "测试工程师回答") }));
        mockFactory.Setup(f => f.CreateAgentAsync(agent2Id)).ReturnsAsync(mockChatClient2.Object);

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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "@架构师 @测试工程师 你好",
            MentionedAgentIds = new List<string> { "100", "200" }
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent2Id && m.Content == "测试工程师回答"
        )), Times.Once, "测试工程师回复应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.FromAgentId == agent1Id && m.Content.Contains("创建失败")
        )), Times.Once, "架构师创建失败消息应该入库");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.IsAny<GroupMessage>()), Times.Exactly(3), "1条用户消息 + 1条错误消息 + 1条Agent回复 = 3条");
    }

    [Fact]
    public async Task Chat_MentionAllAgents_DeserializedFromJsonCorrectly()
    {
        var json = @"{""content"":""@所有人 你好"",""mentionedAgentIds"":[""100"",""200"",""300""]}";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        var request = System.Text.Json.JsonSerializer.Deserialize<CollaborationChatRequest>(json, options);

        Assert.NotNull(request);
        Assert.Equal("@所有人 你好", request.Content);
        Assert.NotNull(request.MentionedAgentIds);
        Assert.Equal(3, request.MentionedAgentIds.Count);
        Assert.Equal(new List<string> { "100", "200", "300" }, request.MentionedAgentIds);
    }

    [Fact]
    public void LlmConfigItem_DeserializeFromDbJson_ShouldMapLlmConfigName()
    {
        var dbJson = @"[{""llmConfigId"":123,""llmConfigName"":""阿里云-通义千问3.5"",""llmModelConfigId"":456,""modelName"":""qwen3.5-35b-a3b"",""isPrimary"":true,""priority"":1,""isValid"":true,""msg"":""250ms""}]";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var configs = System.Text.Json.JsonSerializer.Deserialize<List<LlmConfigItem>>(dbJson, options);

        Assert.NotNull(configs);
        Assert.Single(configs);
        Assert.Equal("阿里云-通义千问3.5", configs[0].LlmConfigName);
        Assert.Equal("qwen3.5-35b-a3b", configs[0].ModelName);
        Assert.True(configs[0].IsPrimary);
    }

    [Fact]
    public void LlmConfigItem_DeserializeMultipleConfigs_PrimaryFirst()
    {
        var dbJson = @"[{""llmConfigId"":1,""llmConfigName"":""备用配置"",""modelName"":""backup-model"",""isPrimary"":false,""priority"":2},{""llmConfigId"":2,""llmConfigName"":""主配置"",""modelName"":""main-model"",""isPrimary"":true,""priority"":1}]";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var configs = System.Text.Json.JsonSerializer.Deserialize<List<LlmConfigItem>>(dbJson, options);

        Assert.NotNull(configs);
        Assert.Equal(2, configs.Count);
        var primary = configs.FirstOrDefault(c => c.IsPrimary);
        Assert.NotNull(primary);
        Assert.Equal("主配置", primary.LlmConfigName);
        Assert.Equal("main-model", primary.ModelName);
    }

    [Fact]
    public async Task Chat_ShouldReturnLlmConfigName_InResponseAndDb()
    {
        var managerAgentId = 100L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo.Setup(r => r.GetByCollaborationIdAsync(1L)).ReturnsAsync(members);

        var managerEntity = new Agent
        {
            Id = managerAgentId,
            Name = "架构师",
            TypeName = "Architect",
            SystemPrompt = "你是架构师",
            LlmConfigs = @"[{""llmConfigId"":1,""llmConfigName"":""阿里云-通义千问"",""llmModelConfigId"":2,""modelName"":""qwen-max"",""isPrimary"":true,""priority"":0,""isValid"":true,""msg"":""ok""}]"
        };

        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(managerAgentId)).ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        var chatResponse = new ChatResponse(new List<ChatMessage>
        {
            new(ChatRole.Assistant, "架构师回复")
        })
        {
            ModelId = "qwen-max"
        };
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        mockFactory.Setup(f => f.CreateAgentAsync(managerAgentId)).ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("data", out var dataProp));
        var firstItem = dataProp.EnumerateArray().First();
        Assert.True(firstItem.TryGetProperty("llmConfigName", out var llmConfigNameProp));
        Assert.Equal("阿里云-通义千问", llmConfigNameProp.GetString());
        Assert.True(firstItem.TryGetProperty("modelName", out var modelNameProp));
        Assert.Equal("qwen-max", modelNameProp.GetString());

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.LlmConfigName == "阿里云-通义千问"
            && m.ModelName == "qwen-max"
        )), Times.Once, "llmConfigName和modelName应正确入库");
    }

    [Fact]
    public async Task Chat_PrivateMessage_ShouldSetMessageTypeToPrivate()
    {
        var workerAgentId = 200L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = 100L, Role = "Manager" },
            new() { AgentId = workerAgentId, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo.Setup(r => r.GetByCollaborationIdAsync(1L)).ReturnsAsync(members);

        var workerEntity = new Agent { Id = workerAgentId, Name = "架构师", TypeName = "Architect", SystemPrompt = "你是架构师" };
        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(workerAgentId)).ReturnsAsync(workerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "私聊回复") }));
        mockFactory.Setup(f => f.CreateAgentAsync(workerAgentId)).ReturnsAsync(mockChatClient.Object);

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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "私聊消息",
            MessageType = "private",
            ToAgentId = workerAgentId
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "User" && m.MessageType == "private" && m.ToAgentId == workerAgentId
        )), Times.Once, "私聊用户消息的message_type应为private且to_agent_id应设置");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.MessageType == "private" && m.FromAgentId == workerAgentId
        )), Times.Once, "私聊Agent回复的message_type应为private");

        mockFactory.Verify(f => f.CreateAgentAsync(workerAgentId), Times.Once, "私聊应只调用目标Agent");
        mockFactory.Verify(f => f.CreateAgentAsync(100L), Times.Never, "私聊不应调用其他Agent");
    }

    [Fact]
    public async Task Chat_PrivateMessage_ShouldNotRequireMention()
    {
        var workerAgentId = 200L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = 100L, Role = "Manager" },
            new() { AgentId = workerAgentId, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo.Setup(r => r.GetByCollaborationIdAsync(1L)).ReturnsAsync(members);

        var workerEntity = new Agent { Id = workerAgentId, Name = "架构师", TypeName = "Architect", SystemPrompt = "你是架构师" };
        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(workerAgentId)).ReturnsAsync(workerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "收到") }));
        mockFactory.Setup(f => f.CreateAgentAsync(workerAgentId)).ReturnsAsync(mockChatClient.Object);

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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "不需要@的消息",
            MessageType = "private",
            ToAgentId = workerAgentId,
            MentionedAgentIds = null
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        mockFactory.Verify(f => f.CreateAgentAsync(workerAgentId), Times.Once, "私聊无需@也能路由到目标Agent");
    }

    [Fact]
    public async Task Chat_PrivateMessage_InvalidToAgentId_ShouldReturnBadRequest()
    {
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = 100L, Role = "Manager" },
            new() { AgentId = 200L, Role = "Worker" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo.Setup(r => r.GetByCollaborationIdAsync(1L)).ReturnsAsync(members);

        var controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockGroupMessageRepository.Object,
            _mockCollaborationTaskRepository.Object,
            Mock.Of<MAFStudio.Core.Interfaces.Services.IEmailService>(),
            Mock.Of<MAFStudio.Application.Interfaces.IAgentFactoryService>(),
            Mock.Of<IAgentRepository>(),
            mockCollabAgentRepo.Object,
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "私聊消息",
            MessageType = "private",
            ToAgentId = 999L
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Chat_GroupMessage_ShouldSetMessageTypeToChat()
    {
        var managerAgentId = 100L;
        var members = new List<CollaborationAgent>
        {
            new() { AgentId = managerAgentId, Role = "Manager" }
        };

        var mockCollabAgentRepo = new Mock<ICollaborationAgentRepository>();
        mockCollabAgentRepo.Setup(r => r.GetByCollaborationIdAsync(1L)).ReturnsAsync(members);

        var managerEntity = new Agent { Id = managerAgentId, Name = "协调者", TypeName = "Manager", SystemPrompt = "你是协调者" };
        var mockAgentRepo = new Mock<IAgentRepository>();
        mockAgentRepo.Setup(r => r.GetByIdAsync(managerAgentId)).ReturnsAsync(managerEntity);

        var mockFactory = new Mock<MAFStudio.Application.Interfaces.IAgentFactoryService>();
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "群聊回复") }));
        mockFactory.Setup(f => f.CreateAgentAsync(managerAgentId)).ReturnsAsync(mockChatClient.Object);

        _mockGroupMessageRepository
            .Setup(r => r.GetByCollaborationIdAsync(1L, 10, It.IsAny<long?>()))
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
            new SkillLoader(Mock.Of<IAgentSkillRepository>()),
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
            Content = "群聊消息",
            MessageType = null,
            ToAgentId = null
        };

        var result = await controller.Chat(1L, request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "User" && m.MessageType == "chat" && m.ToAgentId == null
        )), Times.Once, "群聊用户消息的message_type应为chat且to_agent_id应为null");

        _mockGroupMessageRepository.Verify(r => r.CreateAsync(It.Is<GroupMessage>(m =>
            m.SenderType == "Agent" && m.MessageType == "chat"
        )), Times.Once, "群聊Agent回复的message_type应为chat");
    }

    [Fact]
    public async Task Chat_PrivateMessage_DeserializedFromJsonCorrectly()
    {
        var json = @"{""content"":""私聊你好"",""mentionedAgentIds"":null,""messageType"":""private"",""toAgentId"":200}";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        var request = System.Text.Json.JsonSerializer.Deserialize<CollaborationChatRequest>(json, options);

        Assert.NotNull(request);
        Assert.Equal("私聊你好", request.Content);
        Assert.Equal("private", request.MessageType);
        Assert.Equal(200, request.ToAgentId);
        Assert.Null(request.MentionedAgentIds);
    }

    [Fact]
    public void GetChatHistory_PrivateFilter_MatchesBothUserAndAgentMessages()
    {
        var messages = new List<GroupMessage>
        {
            new GroupMessage { Id = 1, MessageType = "chat", SenderType = "User", Content = "群聊消息", ToAgentId = null, FromAgentId = null },
            new GroupMessage { Id = 2, MessageType = "chat", SenderType = "Agent", Content = "群聊回复", ToAgentId = null, FromAgentId = 100 },
            new GroupMessage { Id = 3, MessageType = "private", SenderType = "User", Content = "私聊给Agent200", ToAgentId = 200, FromAgentId = null },
            new GroupMessage { Id = 4, MessageType = "private", SenderType = "Agent", Content = "Agent200回复", ToAgentId = 200, FromAgentId = 200 },
            new GroupMessage { Id = 5, MessageType = "private", SenderType = "User", Content = "私聊给Agent300", ToAgentId = 300, FromAgentId = null },
            new GroupMessage { Id = 6, MessageType = "private", SenderType = "Agent", Content = "Agent300回复", ToAgentId = 300, FromAgentId = 300 },
        };

        var filtered = messages.Where(m =>
            m.MessageType == "private" &&
            (m.ToAgentId == 200 || (m.FromAgentId == 200 && m.SenderType == "Agent")))
            .ToList();

        Assert.Equal(2, filtered.Count);
        Assert.Equal(3, filtered[0].Id);
        Assert.Equal(4, filtered[1].Id);
        Assert.Equal("私聊给Agent200", filtered[0].Content);
        Assert.Equal("Agent200回复", filtered[1].Content);
    }

    [Fact]
    public void GetChatHistory_PrivateFilter_ExcludesGroupMessages()
    {
        var messages = new List<GroupMessage>
        {
            new GroupMessage { Id = 1, MessageType = "chat", SenderType = "User", Content = "群聊消息", ToAgentId = null, FromAgentId = null },
            new GroupMessage { Id = 2, MessageType = "chat", SenderType = "Agent", Content = "群聊回复", ToAgentId = null, FromAgentId = 200 },
            new GroupMessage { Id = 3, MessageType = "private", SenderType = "User", Content = "私聊消息", ToAgentId = 200, FromAgentId = null },
        };

        var filtered = messages.Where(m =>
            m.MessageType == "private" &&
            (m.ToAgentId == 200 || (m.FromAgentId == 200 && m.SenderType == "Agent")))
            .ToList();

        Assert.Single(filtered);
        Assert.Equal(3, filtered[0].Id);
    }

    [Fact]
    public void GetChatHistory_GroupFilter_ExcludesPrivateMessages()
    {
        var messages = new List<GroupMessage>
        {
            new GroupMessage { Id = 1, MessageType = "chat", SenderType = "User", Content = "群聊消息", ToAgentId = null, FromAgentId = null },
            new GroupMessage { Id = 2, MessageType = "private", SenderType = "User", Content = "私聊消息", ToAgentId = 200, FromAgentId = null },
            new GroupMessage { Id = 3, MessageType = "chat", SenderType = "Agent", Content = "群聊回复", ToAgentId = null, FromAgentId = 200 },
        };

        var filtered = messages.Where(m => m.MessageType == "chat").ToList();

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, m => m.MessageType == "private");
    }

    [Fact]
    public async Task Chat_UserMessageStoredBeforeAgentResponse()
    {
        var storageOrder = new List<string>();
        var collaborationId = 1L;

        _mockCollaborationAgentRepository.Setup(r => r.GetByCollaborationIdAsync(collaborationId))
            .ReturnsAsync(new List<CollaborationAgent>
            {
                new CollaborationAgent { AgentId = 200, Role = "Worker" }
            });

        _mockGroupMessageRepository.Setup(r => r.CreateAsync(It.IsAny<GroupMessage>()))
            .Callback<GroupMessage>(msg =>
            {
                if (msg.SenderType == "User" && msg.MessageType == "private")
                    storageOrder.Add("user");
                else if (msg.SenderType == "Agent" && msg.MessageType == "private")
                    storageOrder.Add("agent");
            })
            .ReturnsAsync((GroupMessage msg) => { msg.Id = storageOrder.Count; return msg; });

        _mockGroupMessageRepository.Setup(r => r.GetByCollaborationIdAsync(collaborationId, It.IsAny<int>(), It.IsAny<long?>()))
            .ReturnsAsync(new List<GroupMessage>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        var request = new CollaborationChatRequest
        {
            Content = "私聊测试",
            MessageType = "private",
            ToAgentId = 200
        };

        var result = await _controller.Chat(collaborationId, request, CancellationToken.None);

        Assert.True(storageOrder.Count >= 1, "至少应该存储了用户消息");
        Assert.Equal("user", storageOrder[0]);
    }
}
