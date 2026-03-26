using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Controllers;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Models.Requests;
using Xunit;

namespace MAFStudio.Backend.Tests.Controllers;

public class AgentsControllerTests : TestBase
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IOperationLogService> _mockLogService;
    private readonly AgentsController _controller;
    private readonly string _testUserId = "test-user-id";

    public AgentsControllerTests() : base()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();

        _controller = new AgentsController(
            _mockAgentService.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
            DbContext
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId)
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAllAgents_ShouldReturnOkWithVos()
    {
        var agent1 = await CreateTestAgentAsync("Agent1", _testUserId);
        var agent2 = await CreateTestAgentAsync("Agent2", _testUserId);

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentsByUserIdAsync(_testUserId, false))
            .ReturnsAsync(new List<Agent> { agent1, agent2 });

        var result = await _controller.GetAllAgents();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var vos = Assert.IsType<List<Models.VOs.AgentListItemVo>>(okResult.Value);
        Assert.Equal(2, vos.Count);
    }

    [Fact]
    public async Task GetAgent_ExistingId_ShouldReturnOkWithVo()
    {
        var agent = await CreateTestAgentAsync("Agent1", _testUserId);

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        var result = await _controller.GetAgent(agent.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var vo = Assert.IsType<Models.VOs.AgentVo>(okResult.Value);
        Assert.Equal("Agent1", vo.Name);
    }

    [Fact]
    public async Task GetAgent_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(nonExistingId))
            .ReturnsAsync((Agent?)null);

        var result = await _controller.GetAgent(nonExistingId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAgent_ValidRequest_ShouldReturnCreatedAtAction()
    {
        var request = new CreateAgentRequest
        {
            Name = "New Agent",
            Description = "Test Description",
            Type = "Assistant",
            Configuration = "{}"
        };

        var createdAgent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Configuration = request.Configuration,
            UserId = _testUserId,
            Status = AgentStatus.Inactive,
            CreatedAt = DateTime.UtcNow
        };

        _mockAgentService
            .Setup(s => s.CreateAgentAsync(
                request.Name, request.Description, request.Type,
                request.Configuration, request.Avatar, _testUserId, request.LLMConfigId))
            .ReturnsAsync(createdAgent);

        var result = await _controller.CreateAgent(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedAgent = Assert.IsType<Agent>(createdResult.Value);
        Assert.Equal(request.Name, returnedAgent.Name);
    }

    [Fact]
    public async Task UpdateAgent_ValidRequest_ShouldReturnOk()
    {
        var agent = await CreateTestAgentAsync("Agent1", _testUserId);

        var request = new UpdateAgentRequest
        {
            Name = "Updated Agent",
            Description = "Updated Description",
            Configuration = "{}"
        };

        var updatedAgent = new Agent
        {
            Id = agent.Id,
            Name = request.Name,
            Description = request.Description,
            Configuration = request.Configuration,
            UserId = agent.UserId,
            Status = agent.Status,
            CreatedAt = agent.CreatedAt
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.UpdateAgentAsync(
                agent.Id, request.Name, request.Description,
                request.Configuration, request.Avatar, request.LLMConfigId))
            .ReturnsAsync(updatedAgent);

        var result = await _controller.UpdateAgent(agent.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAgent = Assert.IsType<Agent>(okResult.Value);
        Assert.Equal(request.Name, returnedAgent.Name);
    }

    [Fact]
    public async Task UpdateAgent_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        var request = new UpdateAgentRequest
        {
            Name = "Updated Agent"
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(nonExistingId))
            .ReturnsAsync((Agent?)null);

        var result = await _controller.UpdateAgent(nonExistingId, request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAgent_ExistingId_ShouldReturnNoContent()
    {
        var agent = await CreateTestAgentAsync("Agent1", _testUserId);

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.DeleteAgentAsync(agent.Id))
            .ReturnsAsync(true);

        var result = await _controller.DeleteAgent(agent.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAgent_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = Guid.NewGuid();

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(nonExistingId))
            .ReturnsAsync((Agent?)null);

        var result = await _controller.DeleteAgent(nonExistingId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateAgentStatus_ValidRequest_ShouldReturnOk()
    {
        var agent = await CreateTestAgentAsync("Agent1", _testUserId);

        var request = new UpdateAgentStatusRequest
        {
            Status = AgentStatus.Active
        };

        var updatedAgent = new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            UserId = agent.UserId,
            Status = AgentStatus.Active,
            CreatedAt = agent.CreatedAt
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetAgentByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.UpdateAgentStatusAsync(agent.Id, request.Status))
            .ReturnsAsync(updatedAgent);

        var result = await _controller.UpdateAgentStatus(agent.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAgent = Assert.IsType<Agent>(okResult.Value);
        Assert.Equal(AgentStatus.Active, returnedAgent.Status);
    }
}
