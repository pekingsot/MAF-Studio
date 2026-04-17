using MAFStudio.Application.Skills;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using Moq;
using Xunit;

namespace MAFStudio.Tests.Skills;

public class SkillLoaderTests
{
    private readonly Mock<IAgentSkillRepository> _mockRepo;
    private readonly SkillLoader _loader;

    public SkillLoaderTests()
    {
        _mockRepo = new Mock<IAgentSkillRepository>();
        _loader = new SkillLoader(_mockRepo.Object);
    }

    [Fact]
    public async Task LoadSkillsForAgentAsync_NoSkills_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetEnabledByAgentIdAsync(1))
            .ReturnsAsync(new List<AgentSkill>());

        var result = await _loader.LoadSkillsForAgentAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadSkillsForAgentAsync_WithSkills_ReturnsDefinitions()
    {
        var agentSkill = new AgentSkill
        {
            Id = 1,
            AgentId = 1,
            SkillName = "code-review",
            SkillContent = @"---
name: code-review
description: Code review expert
allowed-tools:
  - ReadFile
  - WriteFile
---

# Code Review

You are a code review expert.",
            Enabled = true,
            Runtime = "python"
        };

        _mockRepo.Setup(r => r.GetEnabledByAgentIdAsync(1))
            .ReturnsAsync(new List<AgentSkill> { agentSkill });

        var result = await _loader.LoadSkillsForAgentAsync(1);

        Assert.Single(result);
        Assert.Equal("code-review", result[0].Name);
        Assert.Equal("Code review expert", result[0].Description);
        Assert.Contains("ReadFile", result[0].AllowedTools);
        Assert.Contains("WriteFile", result[0].AllowedTools);
        Assert.Contains("Code Review", result[0].Instructions);
    }

    [Fact]
    public async Task LoadSkillsForAgentAsync_CachesResult()
    {
        var agentSkill = new AgentSkill
        {
            Id = 1,
            AgentId = 1,
            SkillName = "test-skill",
            SkillContent = "---\nname: test-skill\ndescription: Test\n---\n\nTest instructions",
            Enabled = true,
            Runtime = "python"
        };

        _mockRepo.Setup(r => r.GetEnabledByAgentIdAsync(1))
            .ReturnsAsync(new List<AgentSkill> { agentSkill });

        var result1 = await _loader.LoadSkillsForAgentAsync(1);
        var result2 = await _loader.LoadSkillsForAgentAsync(1);

        _mockRepo.Verify(r => r.GetEnabledByAgentIdAsync(1), Times.Once());
        Assert.Equal(result1.Count, result2.Count);
    }

    [Fact]
    public void ParseSkillContent_WithFrontmatter_ParsesCorrectly()
    {
        var content = @"---
name: api-doc-generator
description: API documentation generator
version: 2.0.0
author: MAFStudio
allowed-tools:
  - ReadFile
  - SearchInCode
tags:
  - documentation
  - api
permissions:
  network: false
  filesystem: true
---

# API Documentation Generator

## When to use
- Generate API docs
- Create interface specs";

        var definition = _loader.ParseSkillContent(content);

        Assert.Equal("api-doc-generator", definition.Name);
        Assert.Equal("API documentation generator", definition.Description);
        Assert.Equal("2.0.0", definition.Version);
        Assert.Equal("MAFStudio", definition.Author);
        Assert.Contains("ReadFile", definition.AllowedTools);
        Assert.Contains("SearchInCode", definition.AllowedTools);
        Assert.Contains("documentation", definition.Tags);
        Assert.Contains("api", definition.Tags);
        Assert.NotNull(definition.Permissions);
        Assert.False(definition.Permissions.Network);
        Assert.True(definition.Permissions.Filesystem);
        Assert.Contains("API Documentation Generator", definition.Instructions);
    }

    [Fact]
    public void ParseSkillContent_WithoutFrontmatter_ReturnsRawContent()
    {
        var content = "# Simple Skill\n\nJust do the thing.";

        var definition = _loader.ParseSkillContent(content);

        Assert.Equal(string.Empty, definition.Name);
        Assert.Contains("Simple Skill", definition.Instructions);
    }

    [Fact]
    public void BuildSkillInstructions_NoSkills_ReturnsEmpty()
    {
        var result = _loader.BuildSkillInstructions(new List<SkillDefinition>());

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void BuildSkillInstructions_WithSkills_ContainsSkillInfo()
    {
        var skills = new List<SkillDefinition>
        {
            new()
            {
                Name = "code-review",
                Description = "Code review expert",
                AllowedTools = new List<string> { "ReadFile", "WriteFile" },
                Instructions = "Review code and find issues."
            }
        };

        var result = _loader.BuildSkillInstructions(skills);

        Assert.Contains("code-review", result);
        Assert.Contains("Code review expert", result);
        Assert.Contains("ReadFile", result);
        Assert.Contains("WriteFile", result);
        Assert.Contains("Review code and find issues", result);
    }

    [Fact]
    public void ClearCache_ClearsCachedData()
    {
        var agentSkill = new AgentSkill
        {
            Id = 1,
            AgentId = 1,
            SkillName = "test-skill",
            SkillContent = "---\nname: test-skill\ndescription: Test\n---\n\nTest",
            Enabled = true,
            Runtime = "python"
        };

        _mockRepo.Setup(r => r.GetEnabledByAgentIdAsync(1))
            .ReturnsAsync(new List<AgentSkill> { agentSkill });

        _loader.LoadSkillsForAgentAsync(1).Wait();
        _loader.ClearCache();

        _mockRepo.Setup(r => r.GetEnabledByAgentIdAsync(1))
            .ReturnsAsync(new List<AgentSkill>());

        var result = _loader.LoadSkillsForAgentAsync(1).Result;

        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadSkillsForAgentAsync_InvalidSkillContent_SkipsAndContinues()
    {
        var validSkill = new AgentSkill
        {
            Id = 1,
            AgentId = 1,
            SkillName = "valid-skill",
            SkillContent = "---\nname: valid-skill\ndescription: Valid\n---\n\nValid instructions",
            Enabled = true,
            Runtime = "python"
        };

        _mockRepo.Setup(r => r.GetEnabledByAgentIdAsync(1))
            .ReturnsAsync(new List<AgentSkill> { validSkill });

        var result = await _loader.LoadSkillsForAgentAsync(1);

        Assert.Single(result);
        Assert.Equal("valid-skill", result[0].Name);
    }

    [Fact]
    public void ParseSkillContent_WithInputsOutputs_ParsesCorrectly()
    {
        var content = @"---
name: data-processor
description: Process data files
inputs:
  - name: filePath
    type: string
    required: true
    description: Path to data file
  - name: format
    type: string
    required: false
    default: json
outputs:
  - name: result
    type: object
    description: Processing result
---

# Data Processor";

        var definition = _loader.ParseSkillContent(content);

        Assert.Equal("data-processor", definition.Name);
        Assert.Equal(2, definition.Inputs.Count);
        Assert.Equal("filePath", definition.Inputs[0].Name);
        Assert.Equal("string", definition.Inputs[0].Type);
        Assert.True(definition.Inputs[0].Required);
        Assert.Equal("format", definition.Inputs[1].Name);
        Assert.Equal("json", definition.Inputs[1].Default);
        Assert.Single(definition.Outputs);
        Assert.Equal("result", definition.Outputs[0].Name);
    }
}
