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
using MAFStudio.Application.VOs;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Xunit;

namespace MAFStudio.Tests.Controllers;

public class AgentsControllerTests : TestBase
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IAgentTypeRepository> _mockAgentTypeRepository;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IOperationLogService> _mockLogService;
    private readonly AgentsController _controller;
    private readonly string _testUserId = "test-user-id";

    public AgentsControllerTests() : base()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockAgentTypeRepository = new Mock<IAgentTypeRepository>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();

        _controller = new AgentsController(
            _mockAgentService.Object,
            _mockAgentTypeRepository.Object,
            _mockAuthService.Object,
            _mockLogService.Object
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
    public async Task GetAllAgents_ShouldReturnOkWithAgents()
    {
        var agent1 = CreateTestAgent("Agent1", _testUserId);
        var agent2 = CreateTestAgent("Agent2", _testUserId);

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByUserIdAsync(_testUserId, false))
            .ReturnsAsync(new List<Agent> { agent1, agent2 });

        _mockAgentTypeRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(new List<AgentType>());

        var result = await _controller.GetAllAgents();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AgentListVo>(okResult.Value);
        Assert.Equal(2, response.Agents.Count);
    }

    [Fact]
    public async Task GetAgent_ExistingId_ShouldReturnOkWithAgent()
    {
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.GenerateId();

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        var result = await _controller.GetAgent(agent.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAgent = Assert.IsType<AgentVo>(okResult.Value);
        Assert.Equal("Agent1", returnedAgent.Name);
    }

    [Fact]
    public async Task GetAgent_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = 999999L;

        _mockAgentService
            .Setup(s => s.GetByIdAsync(nonExistingId))
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
            SystemPrompt = "Test system prompt"
        };

        var createdAgent = CreateTestAgent(request.Name, _testUserId);

        _mockAgentService
            .Setup(s => s.CreateAsync(
                request.Name, request.Description, request.Type,
                request.SystemPrompt, request.Avatar, _testUserId, request.LlmConfigId, request.LlmModelConfigId, null))
            .ReturnsAsync(createdAgent);

        var result = await _controller.CreateAgent(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedAgent = Assert.IsType<Agent>(createdResult.Value);
        Assert.Equal(request.Name, returnedAgent.Name);
    }

    [Fact]
    public async Task UpdateAgent_ValidRequest_ShouldReturnOk()
    {
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.GenerateId();

        var request = new UpdateAgentRequest
        {
            Name = "Updated Agent",
            Description = "Updated Description",
            SystemPrompt = "Updated system prompt"
        };

        var updatedAgent = new Agent
        {
            Id = agent.Id,
            Name = request.Name,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            UserId = agent.UserId,
            Status = agent.Status,
            CreatedAt = agent.CreatedAt
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.UpdateAsync(agent.Id, request.Name, request.Description, request.SystemPrompt, request.Avatar, request.LlmConfigId, request.LlmModelConfigId, null))
            .ReturnsAsync(updatedAgent);

        var result = await _controller.UpdateAgent(agent.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAgent = Assert.IsType<Agent>(okResult.Value);
        Assert.Equal(request.Name, returnedAgent.Name);
    }

    [Fact]
    public async Task UpdateAgent_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = 999999L;

        var request = new UpdateAgentRequest
        {
            Name = "Updated Agent"
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(nonExistingId))
            .ReturnsAsync((Agent?)null);

        var result = await _controller.UpdateAgent(nonExistingId, request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAgent_ExistingId_ShouldReturnNoContent()
    {
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.GenerateId();

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.DeleteAsync(agent.Id))
            .ReturnsAsync(true);

        var result = await _controller.DeleteAgent(agent.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAgent_NonExistingId_ShouldReturnNotFound()
    {
        var nonExistingId = 999999L;

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(nonExistingId))
            .ReturnsAsync((Agent?)null);

        var result = await _controller.DeleteAgent(nonExistingId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateAgentStatus_ValidRequest_ShouldReturnOk()
    {
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.GenerateId();

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
            .Setup(s => s.GetByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.UpdateStatusAsync(agent.Id, request.Status))
            .ReturnsAsync(updatedAgent);

        var result = await _controller.UpdateAgentStatus(agent.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAgent = Assert.IsType<Agent>(okResult.Value);
        Assert.Equal(AgentStatus.Active, returnedAgent.Status);
    }
}
