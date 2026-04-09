using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    private readonly Mock<IAgentMessageRepository> _mockAgentMessageRepository;
    private readonly Mock<ITaskAgentRepository> _mockTaskAgentRepository;
    private readonly Mock<ILogger<CollaborationsController>> _mockLogger;
    private readonly CollaborationsController _controller;
    private readonly long _testUserId = 1000000000000001;

    public CollaborationsControllerTests() : base()
    {
        _mockCollaborationService = new Mock<ICollaborationService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();
        _mockAgentMessageRepository = new Mock<IAgentMessageRepository>();
        _mockTaskAgentRepository = new Mock<ITaskAgentRepository>();
        _mockLogger = new Mock<ILogger<CollaborationsController>>();

        _controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            _mockAgentMessageRepository.Object,
            _mockTaskAgentRepository.Object,
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
    public async Task GetAllCollaborations_ShouldReturnTasksWithPrompt()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1009;

        var task1 = CreateTestTask(collaboration.Id, "Task1", "【任务要求】请积极参与讨论。");
        var task2 = CreateTestTask(collaboration.Id, "Task2", "【任务要求】提交文档到Git。");

        _mockCollaborationService
            .Setup(s => s.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Collaboration> { collaboration });

        _mockCollaborationService
            .Setup(s => s.GetAgentsAsync(collaboration.Id))
            .ReturnsAsync(new List<CollaborationAgent>());

        _mockCollaborationService
            .Setup(s => s.GetTasksAsync(collaboration.Id))
            .ReturnsAsync(new List<CollaborationTask> { task1, task2 });

        var result = await _controller.GetAllCollaborations();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var collaborations = Assert.IsType<List<Application.VOs.CollaborationVo>>(okResult.Value);
        Assert.Single(collaborations);
        Assert.Equal(2, collaborations[0].Tasks.Count);
        Assert.Equal("【任务要求】请积极参与讨论。", collaborations[0].Tasks[0].Prompt);
        Assert.Equal("【任务要求】提交文档到Git。", collaborations[0].Tasks[1].Prompt);
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
            .Setup(s => s.CreateTaskAsync(collaboration.Id, request.Title, request.Description, _testUserId, request.Prompt, null, null, null, null))
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
            .Setup(s => s.CreateTaskAsync(collaboration.Id, request.Title, request.Description, _testUserId, request.Prompt, null, null, null, null))
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
            .Setup(s => s.UpdateTaskAsync(task.Id, request.Title, request.Description, request.Prompt, null, null, null, null))
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
            .Setup(s => s.UpdateTaskAsync(task.Id, request.Title, request.Description, request.Prompt, null, null, null, null))
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
                config))
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
                config))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateTask(task.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(okResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
        Assert.Equal(config, returnedTask.Config);
    }
}
