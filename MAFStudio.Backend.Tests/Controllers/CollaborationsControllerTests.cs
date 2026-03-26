using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Controllers;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Hubs;
using MAFStudio.Backend.Models.Requests;
using Xunit;

namespace MAFStudio.Backend.Tests.Controllers;

public class CollaborationsControllerTests : TestBase
{
    private readonly Mock<ICollaborationService> _mockCollaborationService;
    private readonly Mock<IAgentRuntimeService> _mockAgentRuntimeService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IHubContext<AgentHub>> _mockHubContext;
    private readonly Mock<ILogger<CollaborationsController>> _mockLogger;
    private readonly CollaborationsController _controller;

    public CollaborationsControllerTests() : base()
    {
        _mockCollaborationService = new Mock<ICollaborationService>();
        _mockAgentRuntimeService = new Mock<IAgentRuntimeService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockAgentService = new Mock<IAgentService>();
        _mockHubContext = new Mock<IHubContext<AgentHub>>();
        _mockLogger = new Mock<ILogger<CollaborationsController>>();

        _controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAgentRuntimeService.Object,
            _mockMessageService.Object,
            _mockAgentService.Object,
            _mockHubContext.Object,
            _mockLogger.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAllCollaborations_ShouldReturnOkWithVos()
    {
        var userId = "test-user-id";
        var collaboration = await CreateTestCollaborationAsync("Test1", userId);
        var collaboration2 = await CreateTestCollaborationAsync("Test2", userId);

        _mockCollaborationService
            .Setup(s => s.GetAllCollaborationsAsync(userId))
            .ReturnsAsync(new List<Collaboration> { collaboration, collaboration2 });

        var result = await _controller.GetAllCollaborations();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var vos = Assert.IsType<List<Models.VOs.CollaborationVo>>(okResult.Value);
        Assert.Equal(2, vos.Count);
    }

    [Fact]
    public async Task GetCollaboration_ExistingId_ShouldReturnOkWithVo()
    {
        var userId = "test-user-id";
        var collaboration = await CreateTestCollaborationAsync("Test", userId);

        _mockCollaborationService
            .Setup(s => s.GetCollaborationByIdAsync(collaboration.Id, userId))
            .ReturnsAsync(collaboration);

        _mockCollaborationService
            .Setup(s => s.GetCollaborationAgentsAsync(collaboration.Id))
            .ReturnsAsync(new List<CollaborationAgent>());

        var result = await _controller.GetCollaboration(collaboration.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var vo = Assert.IsType<Models.VOs.CollaborationVo>(okResult.Value);
        Assert.Equal("Test", vo.Name);
    }

    [Fact]
    public async Task GetCollaboration_NonExistingId_ShouldReturnNotFound()
    {
        var userId = "test-user-id";
        var nonExistingId = Guid.NewGuid();

        _mockCollaborationService
            .Setup(s => s.GetCollaborationByIdAsync(nonExistingId, userId))
            .ReturnsAsync((Collaboration?)null);

        var result = await _controller.GetCollaboration(nonExistingId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateCollaboration_ValidRequest_ShouldReturnCreatedAtAction()
    {
        var userId = "test-user-id";
        var request = new CreateCollaborationRequest
        {
            Name = "New Collaboration",
            Description = "Test Description"
        };

        var createdCollaboration = new Collaboration
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            UserId = userId,
            Status = CollaborationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _mockCollaborationService
            .Setup(s => s.CreateCollaborationAsync(
                request.Name, request.Description, request.Path,
                request.GitRepositoryUrl, request.GitBranch, request.GitUsername,
                request.GitEmail, request.GitAccessToken, userId))
            .ReturnsAsync(createdCollaboration);

        var result = await _controller.CreateCollaboration(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedCollaboration = Assert.IsType<Collaboration>(createdResult.Value);
        Assert.Equal(request.Name, returnedCollaboration.Name);
    }

    [Fact]
    public async Task DeleteCollaboration_ExistingId_ShouldReturnNoContent()
    {
        var userId = "test-user-id";
        var collaboration = await CreateTestCollaborationAsync("Test", userId);

        _mockCollaborationService
            .Setup(s => s.DeleteCollaborationAsync(collaboration.Id, userId))
            .ReturnsAsync(true);

        var result = await _controller.DeleteCollaboration(collaboration.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCollaboration_NonExistingId_ShouldReturnNotFound()
    {
        var userId = "test-user-id";
        var nonExistingId = Guid.NewGuid();

        _mockCollaborationService
            .Setup(s => s.DeleteCollaborationAsync(nonExistingId, userId))
            .ReturnsAsync(false);

        var result = await _controller.DeleteCollaboration(nonExistingId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddAgentToCollaboration_ValidRequest_ShouldReturnOk()
    {
        var userId = "test-user-id";
        var user = await CreateTestUserAsync("testuser");
        var agent = await CreateTestAgentAsync("Agent1", user.Id);
        var collaboration = await CreateTestCollaborationAsync("Test", userId);

        var request = new AddAgentRequest
        {
            AgentId = agent.Id,
            Role = "Developer"
        };

        _mockCollaborationService
            .Setup(s => s.AddAgentToCollaborationAsync(collaboration.Id, agent.Id, request.Role, userId))
            .ReturnsAsync(collaboration);

        var result = await _controller.AddAgentToCollaboration(collaboration.Id, request);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task RemoveAgentFromCollaboration_ValidRequest_ShouldReturnNoContent()
    {
        var userId = "test-user-id";
        var collaborationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        _mockCollaborationService
            .Setup(s => s.RemoveAgentFromCollaborationAsync(collaborationId, agentId, userId))
            .ReturnsAsync(true);

        var result = await _controller.RemoveAgentFromCollaboration(collaborationId, agentId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CreateTask_ValidRequest_ShouldReturnCreatedAtAction()
    {
        var userId = "test-user-id";
        var collaboration = await CreateTestCollaborationAsync("Test", userId);

        var request = new CreateTaskRequest
        {
            Title = "New Task",
            Description = "Task Description"
        };

        var createdTask = new CollaborationTask
        {
            Id = Guid.NewGuid(),
            CollaborationId = collaboration.Id,
            Title = request.Title,
            Description = request.Description,
            Status = Data.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _mockCollaborationService
            .Setup(s => s.CreateTaskAsync(collaboration.Id, request.Title, request.Description, userId))
            .ReturnsAsync(createdTask);

        var result = await _controller.CreateTask(collaboration.Id, request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(createdResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
    }

    [Fact]
    public async Task UpdateTaskStatus_ValidRequest_ShouldReturnOk()
    {
        var userId = "test-user-id";
        var collaboration = await CreateTestCollaborationAsync("Test", userId);
        var task = await CreateTestTaskAsync(collaboration.Id, "Task");

        var request = new UpdateTaskStatusRequest
        {
            Status = "InProgress"
        };

        var updatedTask = new CollaborationTask
        {
            Id = task.Id,
            CollaborationId = collaboration.Id,
            Title = task.Title,
            Status = Data.TaskStatus.InProgress,
            CreatedAt = task.CreatedAt
        };

        _mockCollaborationService
            .Setup(s => s.UpdateTaskStatusAsync(task.Id, Data.TaskStatus.InProgress, userId))
            .ReturnsAsync(updatedTask);

        var result = await _controller.UpdateTaskStatus(task.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(okResult.Value);
        Assert.Equal(Data.TaskStatus.InProgress, returnedTask.Status);
    }

    [Fact]
    public async Task DeleteTask_ExistingId_ShouldReturnNoContent()
    {
        var userId = "test-user-id";
        var taskId = Guid.NewGuid();

        _mockCollaborationService
            .Setup(s => s.DeleteTaskAsync(taskId, userId))
            .ReturnsAsync(true);

        var result = await _controller.DeleteTask(taskId);

        Assert.IsType<NoContentResult>(result);
    }
}
