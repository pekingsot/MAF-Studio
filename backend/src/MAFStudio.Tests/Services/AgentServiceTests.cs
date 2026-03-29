using Moq;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Application.Services;
using Xunit;

namespace MAFStudio.Tests.Services;

/// <summary>
/// 智能体服务测试
/// </summary>
public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _mockAgentRepository;
    private readonly AgentService _agentService;

    public AgentServiceTests()
    {
        _mockAgentRepository = new Mock<IAgentRepository>();
        _agentService = new AgentService(_mockAgentRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAgents()
    {
        // Arrange
        var agents = new List<Agent>
        {
            new Agent { Id = 1, Name = "Agent1" },
            new Agent { Id = 2, Name = "Agent2" }
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(agents);

        // Act
        var result = await _agentService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAgent()
    {
        // Arrange
        var agent = new Agent { Id = 1, Name = "TestAgent" };

        _mockAgentRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(agent);

        // Act
        var result = await _agentService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestAgent", result?.Name);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAgentWithRedundantFields()
    {
        // Arrange
        var name = "New Agent";
        var description = "Test Description";
        var type = "Assistant";
        var systemPrompt = "Test Prompt";
        var avatar = "🤖";
        var userId = 1L;
        var llmConfigId = 100L;
        var llmModelConfigId = 200L;
        var typeName = "助手";
        var llmConfigName = "测试配置";
        var llmModelName = "测试模型";
        var fallbackModelsJson = "[{\"llmConfigId\":100,\"llmConfigName\":\"测试配置\",\"llmModelConfigId\":200,\"modelName\":\"测试模型\",\"priority\":1}]";

        Agent? capturedAgent = null;
        _mockAgentRepository.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .Callback<Agent>(a => capturedAgent = a)
            .ReturnsAsync((Agent a) => a);

        // Act
        var result = await _agentService.CreateAsync(
            name, description, type, systemPrompt, avatar, userId,
            llmConfigId, llmModelConfigId, fallbackModelsJson, typeName, llmConfigName, llmModelName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(typeName, result.TypeName);
        Assert.Equal(llmConfigName, result.LlmConfigName);
        Assert.Equal(llmModelName, result.LlmModelName);
        Assert.Equal(fallbackModelsJson, result.FallbackModels);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAgentWithRedundantFields()
    {
        // Arrange
        var existingAgent = new Agent
        {
            Id = 1,
            Name = "Old Name",
            UserId = 1
        };

        var newName = "Updated Agent";
        var newDescription = "Updated Description";
        var llmConfigId = 100L;
        var llmModelConfigId = 200L;
        var typeName = "助手";
        var llmConfigName = "测试配置";
        var llmModelName = "测试模型";
        var fallbackModelsJson = "[{\"llmConfigId\":100,\"llmConfigName\":\"测试配置\",\"llmModelConfigId\":200,\"modelName\":\"测试模型\",\"priority\":1}]";

        _mockAgentRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingAgent);

        _mockAgentRepository.Setup(r => r.UpdateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        // Act
        var result = await _agentService.UpdateAsync(
            1, newName, newDescription, null, null,
            llmConfigId, llmModelConfigId, fallbackModelsJson, typeName, llmConfigName, llmModelName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newName, result.Name);
        Assert.Equal(newDescription, result.Description);
        Assert.Equal(llmConfigName, result.LlmConfigName);
        Assert.Equal(llmModelName, result.LlmModelName);
        Assert.Equal(fallbackModelsJson, result.FallbackModels);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenAgentExists()
    {
        // Arrange
        _mockAgentRepository.Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _agentService.DeleteAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserAgents_WhenNotAdmin()
    {
        // Arrange
        var userId = 1L;
        var agents = new List<Agent>
        {
            new Agent { Id = 1, Name = "Agent1", UserId = userId },
            new Agent { Id = 2, Name = "Agent2", UserId = userId }
        };

        _mockAgentRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(agents);

        // Act
        var result = await _agentService.GetByUserIdAsync(userId, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAllAgents_WhenAdmin()
    {
        // Arrange
        var agents = new List<Agent>
        {
            new Agent { Id = 1, Name = "Agent1", UserId = 1 },
            new Agent { Id = 2, Name = "Agent2", UserId = 2 }
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(agents);

        // Act
        var result = await _agentService.GetByUserIdAsync(1, true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
