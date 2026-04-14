using System.Reflection;
using MAFStudio.Application.Capabilities;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

public class EmailCapabilityTests
{
    private readonly EmailCapability _emailCapability;

    public EmailCapabilityTests()
    {
        _emailCapability = new EmailCapability();
    }

    [Fact]
    public void TestCapabilityInfo()
    {
        Console.WriteLine($"Capability名称: {_emailCapability.Name}");
        Console.WriteLine($"Capability描述: {_emailCapability.Description}");

        var tools = _emailCapability.GetTools().ToList();
        Console.WriteLine($"\n工具数量: {tools.Count}");

        foreach (var tool in tools)
        {
            var attr = tool.GetCustomAttribute<ToolAttribute>();
            Console.WriteLine($"  - {tool.Name}: {attr?.Description}");
        }

        Assert.Equal("Email", _emailCapability.Name);
        Assert.True(tools.Count >= 1, $"工具数量应该至少为1，实际为{tools.Count}");
    }
}
