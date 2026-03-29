using Xunit;
using Moq;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Api.Controllers;
using MAFStudio.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MAFStudio.Tests.Controllers;

public class LlmConfigsControllerTests
{
    private readonly Mock<ILlmConfigService> _mockLlmConfigService;
    private readonly Mock<ILlmModelConfigRepository> _mockModelConfigRepository;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IOperationLogService> _mockLogService;
    private readonly Mock<ILogger<LlmConfigsController>> _mockLogger;
    private readonly LlmConfigsController _controller;
    private readonly long _testUserId = 1000000000000001;

    public LlmConfigsControllerTests()
    {
        _mockLlmConfigService = new Mock<ILlmConfigService>();
        _mockModelConfigRepository = new Mock<ILlmModelConfigRepository>();
        _mockAuthService = new Mock<IAuthService>();
        _mockLogService = new Mock<IOperationLogService>();
        _mockLogger = new Mock<ILogger<LlmConfigsController>>();

        _controller = new LlmConfigsController(
            _mockLlmConfigService.Object,
            _mockModelConfigRepository.Object,
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
    public async Task DuplicateLlmConfig_WithValidId_ShouldReturnOkResult()
    {
        var originalConfig = new LlmConfig
        {
            Id = 1000,
            Name = "测试配置",
            Provider = "qwen",
            ApiKey = "test-api-key",
            Endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            DefaultModel = "qwen-max",
            IsDefault = true,
            IsEnabled = true
        };

        var newConfig = new LlmConfig
        {
            Id = 1001,
            Name = "测试配置（副本）",
            Provider = "qwen",
            ApiKey = "test-api-key",
            Endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            DefaultModel = "qwen-max",
            IsDefault = false,
            IsEnabled = true
        };

        var originalModels = new List<LlmModelConfig>
        {
            new LlmModelConfig
            {
                Id = 1,
                LlmConfigId = 1000,
                ModelName = "qwen-max",
                DisplayName = "通义千问Max",
                Temperature = 0.7m,
                MaxTokens = 4096,
                ContextWindow = 64000,
                IsDefault = true,
                IsEnabled = true
            },
            new LlmModelConfig
            {
                Id = 2,
                LlmConfigId = 1000,
                ModelName = "qwen-plus",
                DisplayName = "通义千问Plus",
                Temperature = 0.8m,
                MaxTokens = 8192,
                ContextWindow = 128000,
                IsDefault = false,
                IsEnabled = true
            }
        };

        _mockLlmConfigService
            .Setup(s => s.GetByIdAsync(1000))
            .ReturnsAsync(originalConfig);

        _mockLlmConfigService
            .Setup(s => s.CreateAsync(
                "测试配置（副本）",
                "qwen",
                "test-api-key",
                "https://dashscope.aliyuncs.com/compatible-mode/v1",
                "qwen-max",
                It.IsAny<long>()))
            .ReturnsAsync(newConfig);

        _mockModelConfigRepository
            .Setup(r => r.GetByLlmConfigIdAsync(1000))
            .ReturnsAsync(originalModels);

        _mockModelConfigRepository
            .Setup(r => r.CreateAsync(It.IsAny<LlmModelConfig>()))
            .ReturnsAsync((LlmModelConfig model) => model);

        var result = await _controller.DuplicateLlmConfig(1000);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedConfig = Assert.IsType<LlmConfig>(okResult.Value);
        
        Assert.Equal(1001, returnedConfig.Id);
        Assert.Equal("测试配置（副本）", returnedConfig.Name);

        _mockLlmConfigService.Verify(s => s.GetByIdAsync(1000), Times.Once);
        _mockLlmConfigService.Verify(s => s.CreateAsync(
            "测试配置（副本）",
            "qwen",
            "test-api-key",
            "https://dashscope.aliyuncs.com/compatible-mode/v1",
            "qwen-max",
            It.IsAny<long>()), Times.Once);
        
        _mockModelConfigRepository.Verify(r => r.GetByLlmConfigIdAsync(1000), Times.Once);
        _mockModelConfigRepository.Verify(r => r.CreateAsync(It.IsAny<LlmModelConfig>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DuplicateLlmConfig_WithInvalidId_ShouldReturnNotFound()
    {
        _mockLlmConfigService
            .Setup(s => s.GetByIdAsync(9999))
            .ReturnsAsync((LlmConfig?)null);

        var result = await _controller.DuplicateLlmConfig(9999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("原配置不存在", notFoundResult.Value);

        _mockLlmConfigService.Verify(s => s.GetByIdAsync(9999), Times.Once);
        _mockLlmConfigService.Verify(s => s.CreateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>()), Times.Never);
        
        _mockModelConfigRepository.Verify(r => r.GetByLlmConfigIdAsync(It.IsAny<long>()), Times.Never);
        _mockModelConfigRepository.Verify(r => r.CreateAsync(It.IsAny<LlmModelConfig>()), Times.Never);
    }

    [Fact]
    public async Task DuplicateLlmConfig_ShouldCopyAllModels()
    {
        var originalConfig = new LlmConfig
        {
            Id = 1000,
            Name = "测试配置",
            Provider = "qwen",
            ApiKey = "test-api-key",
            Endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1",
            DefaultModel = "qwen-max"
        };

        var newConfig = new LlmConfig
        {
            Id = 1001,
            Name = "测试配置（副本）"
        };

        var originalModels = new List<LlmModelConfig>
        {
            new LlmModelConfig
            {
                Id = 1,
                LlmConfigId = 1000,
                ModelName = "qwen-max",
                DisplayName = "通义千问Max",
                Temperature = 0.7m,
                MaxTokens = 4096,
                ContextWindow = 64000,
                TopP = 0.9m,
                FrequencyPenalty = 0.1m,
                PresencePenalty = 0.2m,
                StopSequences = "[\"STOP\"]",
                IsDefault = true,
                IsEnabled = true,
                SortOrder = 1
            }
        };

        _mockLlmConfigService
            .Setup(s => s.GetByIdAsync(1000))
            .ReturnsAsync(originalConfig);

        _mockLlmConfigService
            .Setup(s => s.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>()))
            .ReturnsAsync(newConfig);

        _mockModelConfigRepository
            .Setup(r => r.GetByLlmConfigIdAsync(1000))
            .ReturnsAsync(originalModels);

        LlmModelConfig? capturedModel = null;
        _mockModelConfigRepository
            .Setup(r => r.CreateAsync(It.IsAny<LlmModelConfig>()))
            .Callback<LlmModelConfig>(m => capturedModel = m)
            .ReturnsAsync((LlmModelConfig m) => m);

        await _controller.DuplicateLlmConfig(1000);

        Assert.NotNull(capturedModel);
        Assert.Equal(1001, capturedModel.LlmConfigId);
        Assert.Equal("qwen-max", capturedModel.ModelName);
        Assert.Equal("通义千问Max", capturedModel.DisplayName);
        Assert.Equal(0.7m, capturedModel.Temperature);
        Assert.Equal(4096, capturedModel.MaxTokens);
        Assert.Equal(64000, capturedModel.ContextWindow);
        Assert.Equal(0.9m, capturedModel.TopP);
        Assert.Equal(0.1m, capturedModel.FrequencyPenalty);
        Assert.Equal(0.2m, capturedModel.PresencePenalty);
        Assert.Equal("[\"STOP\"]", capturedModel.StopSequences);
        Assert.False(capturedModel.IsDefault);
        Assert.True(capturedModel.IsEnabled);
        Assert.Equal(1, capturedModel.SortOrder);
    }
}
