using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    private readonly CollaborationsController _controller;
    private readonly long _testUserId = 1000000000000001;

    public CollaborationsControllerTests() : base()
    {
        _mockCollaborationService = new Mock<ICollaborationService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();

        _controller = new CollaborationsController(
            _mockCollaborationService.Object,
            _mockAuthService.Object,
            _mockLogService.Object
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
                request.GitEmail, request.GitAccessToken, _testUserId))
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
            .Setup(s => s.AddAgentAsync(collaboration.Id, request.AgentId, request.Role, _testUserId))
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
            Description = "Task Description"
        };

        var createdTask = CreateTestTask(collaboration.Id, request.Title);

        _mockCollaborationService
            .Setup(s => s.CreateTaskAsync(collaboration.Id, request.Title, request.Description, _testUserId))
            .ReturnsAsync(createdTask);

        var result = await _controller.CreateTask(collaboration.Id, request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedTask = Assert.IsType<CollaborationTask>(createdResult.Value);
        Assert.Equal(request.Title, returnedTask.Title);
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
    public async Task DeleteTask_ExistingId_ShouldReturnNoContent()
    {
        var collaboration = CreateTestCollaboration("Collaboration1", _testUserId);
        collaboration.Id = 1007;
        
        var task = CreateTestTask(collaboration.Id, "Task1");
        task.Id = 3002;

        _mockCollaborationService
            .Setup(s => s.DeleteTaskAsync(task.Id, _testUserId))
            .ReturnsAsync(true);

        var result = await _controller.DeleteTask(task.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteTask_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = 999999L;

        _mockCollaborationService
            .Setup(s => s.DeleteTaskAsync(nonExistingId, _testUserId))
            .ReturnsAsync(false);

        var result = await _controller.DeleteTask(nonExistingId);

        Assert.IsType<NotFoundResult>(result);
    }
}
