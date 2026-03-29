using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<ILlmConfigRepository> _mockLlmConfigRepository;
    private readonly Mock<ILlmModelConfigRepository> _mockLlmModelConfigRepository;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IOperationLogService> _mockLogService;
    private readonly Mock<ILogger<AgentsController>> _mockLogger;
    private readonly AgentsController _controller;
    private readonly long _testUserId = 1000000000000001;

    public AgentsControllerTests() : base()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockAgentTypeRepository = new Mock<IAgentTypeRepository>();
        _mockLlmConfigRepository = new Mock<ILlmConfigRepository>();
        _mockLlmModelConfigRepository = new Mock<ILlmModelConfigRepository>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();
        _mockLogger = new Mock<ILogger<AgentsController>>();

        _controller = new AgentsController(
            _mockAgentService.Object,
            _mockAgentTypeRepository.Object,
            _mockLlmConfigRepository.Object,
            _mockLlmModelConfigRepository.Object,
            _mockAuthService.Object,
            _mockLogService.Object,
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
    public async Task GetAllAgents_ShouldReturnOkWithAgents()
    {
        var agent1 = CreateTestAgent("Agent1", _testUserId);
        agent1.LlmConfigName = "测试配置";
        agent1.LlmModelName = "测试模型";
        var agent2 = CreateTestAgent("Agent2", _testUserId);
        agent2.LlmConfigName = "测试配置2";
        agent2.LlmModelName = "测试模型2";

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByUserIdAsync(_testUserId, false))
            .ReturnsAsync(new List<Agent> { agent1, agent2 });

        var result = await _controller.GetAllAgents();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<AgentListItemVo>>(okResult.Value);
        Assert.Equal(2, response.Count);
        Assert.Equal("测试配置", response[0].LlmConfigName);
        Assert.Equal("测试模型", response[0].PrimaryModelName);
    }

    [Fact]
    public async Task GetAgentTypes_ShouldReturnOkWithTypes()
    {
        var types = new List<AgentType>
        {
            new AgentType { Id = 1, Name = "助手", Code = "Assistant" },
            new AgentType { Id = 2, Name = "设计师", Code = "Designer" }
        };

        _mockAgentTypeRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(types);

        var result = await _controller.GetAgentTypes();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<AgentTypeVo>>(okResult.Value);
        Assert.Equal(2, response.Count);
    }

    [Fact]
    public async Task GetAgent_ExistingId_ShouldReturnOkWithAgent()
    {
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.LlmConfigName = "测试配置";
        agent.LlmModelName = "测试模型";

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
        Assert.Equal("测试配置", returnedAgent.LlmConfigName);
        Assert.Equal("测试模型", returnedAgent.PrimaryModelName);
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
        var llmConfigId = 100L;
        var llmModelConfigId = 200L;
        
        var request = new CreateAgentRequest
        {
            Name = "New Agent",
            Description = "Test Description",
            Type = "Assistant",
            SystemPrompt = "Test system prompt",
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId
        };

        var createdAgent = CreateTestAgent(request.Name, _testUserId);
        createdAgent.LlmConfigId = llmConfigId;
        createdAgent.LlmModelConfigId = llmModelConfigId;
        createdAgent.LlmConfigName = "测试配置";
        createdAgent.LlmModelName = "测试模型";
        createdAgent.TypeName = "助手";

        _mockLlmConfigRepository
            .Setup(r => r.GetByIdAsync(llmConfigId))
            .ReturnsAsync(new LlmConfig { Id = llmConfigId, Name = "测试配置" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId, ModelName = "gpt-4", DisplayName = "测试模型" });

        _mockAgentTypeRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(new List<AgentType> { new AgentType { Code = "Assistant", Name = "助手" } });

        _mockAgentService
            .Setup(s => s.CreateAsync(
                request.Name, request.Description, request.Type,
                request.SystemPrompt, request.Avatar, _testUserId, 
                llmConfigId, llmModelConfigId, 
                It.IsAny<string>(), "助手", "测试配置", "测试模型"))
            .ReturnsAsync(createdAgent);

        var result = await _controller.CreateAgent(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedAgent = Assert.IsType<AgentVo>(createdResult.Value);
        Assert.Equal(request.Name, returnedAgent.Name);
        Assert.Equal("测试配置", returnedAgent.LlmConfigName);
        Assert.Equal("测试模型", returnedAgent.PrimaryModelName);
    }

    [Fact]
    public async Task CreateAgent_WithFallbackModels_ShouldSaveFallbackModelsDetail()
    {
        var llmConfigId = 100L;
        var llmModelConfigId1 = 200L;
        var llmModelConfigId2 = 201L;
        
        var request = new CreateAgentRequest
        {
            Name = "New Agent",
            Type = "Assistant",
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId1,
            FallbackModels = new List<FallbackModelRequest>
            {
                new FallbackModelRequest { LlmConfigId = llmConfigId, LlmModelConfigId = llmModelConfigId2, Priority = 1 }
            }
        };

        var createdAgent = CreateTestAgent(request.Name, _testUserId);
        createdAgent.LlmConfigId = llmConfigId;
        createdAgent.LlmModelConfigId = llmModelConfigId1;
        createdAgent.LlmConfigName = "测试配置";
        createdAgent.LlmModelName = "主模型";

        _mockLlmConfigRepository
            .Setup(r => r.GetByIdAsync(llmConfigId))
            .ReturnsAsync(new LlmConfig { Id = llmConfigId, Name = "测试配置" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId1))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId1, DisplayName = "主模型" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId2))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId2, DisplayName = "副模型" });

        _mockAgentTypeRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(new List<AgentType> { new AgentType { Code = "Assistant", Name = "助手" } });

        string? capturedFallbackModelsJson = null;
        _mockAgentService
            .Setup(s => s.CreateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), 
                It.IsAny<long?>(), It.IsAny<long?>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string?, string, string?, string?, long, long?, long?, string?, string?, string?, string?>(
                (_, _, _, _, _, _, _, _, fallbackModels, _, _, _) => capturedFallbackModelsJson = fallbackModels)
            .ReturnsAsync(createdAgent);

        var result = await _controller.CreateAgent(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);
        
        Assert.NotNull(capturedFallbackModelsJson);
        
        var fallbackModelsList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(capturedFallbackModelsJson);
        Assert.NotNull(fallbackModelsList);
        Assert.Single(fallbackModelsList);
        
        var fallbackModel = fallbackModelsList[0];
        Assert.Equal("测试配置", fallbackModel["llmConfigName"].GetString());
        Assert.Equal("副模型", fallbackModel["modelName"].GetString());
        Assert.Equal(100, fallbackModel["llmConfigId"].GetInt64());
        Assert.Equal(201, fallbackModel["llmModelConfigId"].GetInt64());
        Assert.Equal(1, fallbackModel["priority"].GetInt32());
    }

    [Fact]
    public async Task CreateAgent_WithMultipleFallbackModels_ShouldSaveAllWithNames()
    {
        var llmConfigId1 = 100L;
        var llmConfigId2 = 101L;
        var llmModelConfigId1 = 200L;
        var llmModelConfigId2 = 201L;
        var llmModelConfigId3 = 202L;
        
        var request = new CreateAgentRequest
        {
            Name = "New Agent",
            Type = "Assistant",
            LlmConfigId = llmConfigId1,
            LlmModelConfigId = llmModelConfigId1,
            FallbackModels = new List<FallbackModelRequest>
            {
                new FallbackModelRequest { LlmConfigId = llmConfigId1, LlmModelConfigId = llmModelConfigId2, Priority = 1 },
                new FallbackModelRequest { LlmConfigId = llmConfigId2, LlmModelConfigId = llmModelConfigId3, Priority = 2 }
            }
        };

        var createdAgent = CreateTestAgent(request.Name, _testUserId);
        createdAgent.LlmConfigId = llmConfigId1;
        createdAgent.LlmModelConfigId = llmModelConfigId1;
        createdAgent.LlmConfigName = "阿里千问";
        createdAgent.LlmModelName = "通义千问-Max";

        _mockLlmConfigRepository
            .Setup(r => r.GetByIdAsync(llmConfigId1))
            .ReturnsAsync(new LlmConfig { Id = llmConfigId1, Name = "阿里千问" });

        _mockLlmConfigRepository
            .Setup(r => r.GetByIdAsync(llmConfigId2))
            .ReturnsAsync(new LlmConfig { Id = llmConfigId2, Name = "OpenAI" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId1))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId1, DisplayName = "通义千问-Max" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId2))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId2, DisplayName = "通义千问-Plus" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId3))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId3, DisplayName = "GPT-4" });

        _mockAgentTypeRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(new List<AgentType> { new AgentType { Code = "Assistant", Name = "助手" } });

        string? capturedFallbackModelsJson = null;
        _mockAgentService
            .Setup(s => s.CreateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), 
                It.IsAny<long?>(), It.IsAny<long?>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string?, string, string?, string?, long, long?, long?, string?, string?, string?, string?>(
                (_, _, _, _, _, _, _, _, fallbackModels, _, _, _) => capturedFallbackModelsJson = fallbackModels)
            .ReturnsAsync(createdAgent);

        var result = await _controller.CreateAgent(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.Value);
        
        Assert.NotNull(capturedFallbackModelsJson);
        
        var fallbackModelsList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(capturedFallbackModelsJson);
        Assert.NotNull(fallbackModelsList);
        Assert.Equal(2, fallbackModelsList.Count);
        
        var firstFallback = fallbackModelsList[0];
        Assert.Equal("阿里千问", firstFallback["llmConfigName"].GetString());
        Assert.Equal("通义千问-Plus", firstFallback["modelName"].GetString());
        Assert.Equal(1, firstFallback["priority"].GetInt32());
        
        var secondFallback = fallbackModelsList[1];
        Assert.Equal("OpenAI", secondFallback["llmConfigName"].GetString());
        Assert.Equal("GPT-4", secondFallback["modelName"].GetString());
        Assert.Equal(2, secondFallback["priority"].GetInt32());
    }

    [Fact]
    public async Task UpdateAgent_ValidRequest_ShouldReturnOk()
    {
        var agent = CreateTestAgent("Agent1", _testUserId);
        agent.Type = "Assistant";

        var llmConfigId = 100L;
        var llmModelConfigId = 200L;
        
        var request = new UpdateAgentRequest
        {
            Name = "Updated Agent",
            Description = "Updated Description",
            SystemPrompt = "Updated system prompt",
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId
        };

        var updatedAgent = new Agent
        {
            Id = agent.Id,
            Name = request.Name,
            Description = request.Description,
            SystemPrompt = request.SystemPrompt,
            UserId = agent.UserId,
            Status = agent.Status,
            CreatedAt = agent.CreatedAt,
            LlmConfigId = llmConfigId,
            LlmModelConfigId = llmModelConfigId,
            LlmConfigName = "测试配置",
            LlmModelName = "测试模型"
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockLlmConfigRepository
            .Setup(r => r.GetByIdAsync(llmConfigId))
            .ReturnsAsync(new LlmConfig { Id = llmConfigId, Name = "测试配置" });

        _mockLlmModelConfigRepository
            .Setup(r => r.GetByIdAsync(llmModelConfigId))
            .ReturnsAsync(new LlmModelConfig { Id = llmModelConfigId, DisplayName = "测试模型" });

        _mockAgentTypeRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(new List<AgentType> { new AgentType { Code = "Assistant", Name = "助手" } });

        _mockAgentService
            .Setup(s => s.UpdateAsync(agent.Id, request.Name, request.Description, request.SystemPrompt, 
                request.Avatar, llmConfigId, llmModelConfigId, 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(updatedAgent);

        var result = await _controller.UpdateAgent(agent.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAgent = Assert.IsType<AgentVo>(okResult.Value);
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

        var request = new UpdateAgentStatusRequest
        {
            Status = AgentStatus.Active
        };

        _mockAuthService
            .Setup(s => s.IsAdminAsync(_testUserId))
            .ReturnsAsync(false);

        _mockAgentService
            .Setup(s => s.GetByIdAsync(agent.Id))
            .ReturnsAsync(agent);

        _mockAgentService
            .Setup(s => s.UpdateStatusAsync(agent.Id, request.Status))
            .ReturnsAsync(true);

        var result = await _controller.UpdateAgentStatus(agent.Id, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }
}
