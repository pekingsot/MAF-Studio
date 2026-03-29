using MAFStudio.Application.Services;
using MAFStudio.Core.Interfaces.Repositories;
using Moq;
using Xunit;
using MAFStudio.Core.Entities;

namespace MAFStudio.Tests.Services;

public class AgentFactoryServiceTests
{
    private readonly Mock<IAgentRepository> _agentRepoMock;
    private readonly Mock<ILlmConfigRepository> _llmConfigRepoMock;
    private readonly Mock<ILlmModelConfigRepository> _llmModelConfigRepoMock;
    private readonly AgentFactoryService _service;

    public AgentFactoryServiceTests()
    {
        _agentRepoMock = new Mock<IAgentRepository>();
        _llmConfigRepoMock = new Mock<ILlmConfigRepository>();
        _llmModelConfigRepoMock = new Mock<ILlmModelConfigRepository>();
        
        _service = new AgentFactoryService(
            _agentRepoMock.Object,
            _llmConfigRepoMock.Object,
            _llmModelConfigRepoMock.Object);
    }

    [Fact]
    public async Task CreateAgentAsync_AgentNotFound_ThrowsNotFoundException()
    {
        _agentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Agent?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.CreateAgentAsync(1));
    }

    [Fact]
    public async Task CreateAgentAsync_AgentWithoutLlmConfig_ThrowsBusinessException()
    {
        var agent = new Agent { Id = 1, Name = "Test" };
        _agentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(agent);

        await Assert.ThrowsAsync<BusinessException>(() => 
            _service.CreateAgentAsync(1));
    }

    [Fact]
    public async Task CreateChatClientAsync_LlmConfigNotFound_ThrowsNotFoundException()
    {
        _llmConfigRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((LlmConfig?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.CreateChatClientAsync(1, 1));
    }

    [Fact]
    public async Task CreateChatClientAsync_ModelConfigNotFound_ThrowsNotFoundException()
    {
        var llmConfig = new LlmConfig { Id = 1, Provider = "openai" };
        _llmConfigRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(llmConfig);
        
        _llmModelConfigRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((LlmModelConfig?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.CreateChatClientAsync(1, 1));
    }

    [Fact]
    public async Task CreateChatClientAsync_UnsupportedProvider_ThrowsNotSupportedException()
    {
        var llmConfig = new LlmConfig { Id = 1, Provider = "unknown" };
        var modelConfig = new LlmModelConfig { Id = 1, ModelName = "test" };
        
        _llmConfigRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(llmConfig);
        _llmModelConfigRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(modelConfig);

        await Assert.ThrowsAsync<NotSupportedException>(() => 
            _service.CreateChatClientAsync(1, 1));
    }
}
