using MAFStudio.Core.Entities;
using Xunit;
using System.Text.Json;

namespace MAFStudio.Tests.Repositories;

public class AgentTypeRepositoryTests
{
    [Fact]
    public void SetDefaultConfiguration_ShouldSetCorrectValues()
    {
        var agentType = new AgentType
        {
            Name = "测试类型",
            Code = "test_type"
        };

        agentType.SetDefaultConfiguration("你是一个助手", 0.8, 8192);

        Console.WriteLine($"[DEBUG] DefaultConfiguration raw: {agentType.DefaultConfiguration}");
        
        var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(agentType.DefaultConfiguration!);
        Console.WriteLine($"[DEBUG] Parsed config: {string.Join(", ", parsed!.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        
        Assert.NotNull(agentType.DefaultConfiguration);
        Assert.Contains("systemPrompt", agentType.DefaultConfiguration);
        Assert.Contains("temperature", agentType.DefaultConfiguration);
        Assert.Contains("maxTokens", agentType.DefaultConfiguration);
        
        var temp = agentType.DefaultTemperature;
        var tokens = agentType.DefaultMaxTokens;
        
        Console.WriteLine($"[DEBUG] DefaultTemperature: {temp}");
        Console.WriteLine($"[DEBUG] DefaultMaxTokens: {tokens}");
        
        Assert.Equal(0.8, temp, 2);
        Assert.Equal(8192, tokens);
    }
}
