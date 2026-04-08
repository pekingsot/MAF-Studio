using System.Reflection;
using MAFStudio.Application.Capabilities;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

public class EmailCapabilityTests
{
    private readonly EmailCapability _emailCapability;
    private readonly string _smtpServer = "smtp.qq.com";
    private readonly int _smtpPort = 587;
    private readonly string _username = "284184032@qq.com";
    private readonly string _password = "xqjrxtrgjzncbhca";
    private readonly string _testEmail = "284184032@qq.com";

    public EmailCapabilityTests()
    {
        _emailCapability = new EmailCapability();
    }

    [Fact(Skip = "跳过真实邮件测试，避免发送测试邮件。需要测试时移除Skip属性。")]
    public void TestSmtpConnection_ShouldSucceed()
    {
        var result = _emailCapability.TestSmtpConnection(
            _smtpServer,
            _smtpPort,
            _username,
            _password,
            true);

        Console.WriteLine($"SMTP连接测试结果: {result}");
        Assert.Contains("SMTP连接测试成功", result);
    }

    [Fact(Skip = "跳过真实邮件测试，避免发送测试邮件。需要测试时移除Skip属性。")]
    public void SendSimpleEmail_ShouldSucceed()
    {
        var subject = $"MAF Studio 单元测试 - 简单邮件 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        var body = $"这是一封来自MAF Studio的单元测试邮件。\n\n发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n此邮件用于测试邮件发送功能。";

        var result = _emailCapability.SendSimpleEmail(
            _smtpServer,
            _smtpPort,
            _username,
            _password,
            _testEmail,
            _testEmail,
            subject,
            body,
            true);

        Console.WriteLine($"发送简单邮件结果: {result}");
        Assert.Contains("成功发送邮件", result);
    }

    [Fact(Skip = "跳过真实邮件测试，避免发送测试邮件。需要测试时移除Skip属性。")]
    public void SendHtmlEmail_ShouldSucceed()
    {
        var subject = $"MAF Studio 单元测试 - HTML邮件 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        var htmlBody = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .header {{ background-color: #4CAF50; color: white; padding: 10px; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #f1f1f1; padding: 10px; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>MAF Studio 邮件测试</h2>
    </div>
    <div class='content'>
        <p>这是一封HTML格式的测试邮件。</p>
        <p>发送时间: <strong>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</strong></p>
        <ul>
            <li>支持HTML格式</li>
            <li>支持CSS样式</li>
            <li>支持富文本内容</li>
        </ul>
    </div>
    <div class='footer'>
        <p>此邮件由MAF Studio自动发送，请勿回复。</p>
    </div>
</body>
</html>";

        var result = _emailCapability.SendHtmlEmail(
            _smtpServer,
            _smtpPort,
            _username,
            _password,
            _testEmail,
            _testEmail,
            subject,
            htmlBody,
            true);

        Console.WriteLine($"发送HTML邮件结果: {result}");
        Assert.Contains("成功发送HTML邮件", result);
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

        Assert.Equal("邮件操作", _emailCapability.Name);
        Assert.True(tools.Count >= 10, $"工具数量应该至少为10，实际为{tools.Count}");
    }
}
