using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using MAFStudio.Application.Capabilities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public class EmailService : IEmailService
{
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        ICollaborationRepository collaborationRepository,
        ILogger<EmailService> logger)
    {
        _collaborationRepository = collaborationRepository;
        _logger = logger;
    }

    public async Task<EmailTestResult> TestSmtpAsync(SmtpTestConfig config)
    {
        var validationError = ValidateSmtpConfig(config.Server, config.Username, config.Password, config.FromEmail);
        if (validationError != null)
        {
            return new EmailTestResult { Success = false, Message = validationError };
        }

        _logger.LogInformation("测试SMTP配置: Server={Server}, Port={Port}", config.Server, config.Port);

        try
        {
            using var client = new SmtpClient(config.Server, config.Port);
            client.UseDefaultCredentials = false;
            client.EnableSsl = config.EnableSsl;
            client.Credentials = new NetworkCredential(config.Username, config.Password);
            client.Timeout = 30000;

            var subject = $"MAF Studio 测试邮件 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var body = $"这是一封测试邮件。\n\n发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n如果您收到这封邮件，说明SMTP配置正确。";

            using var message = new MailMessage(config.FromEmail, config.FromEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            client.Send(message);
            return new EmailTestResult { Success = true, Message = $"成功发送测试邮件到 {config.FromEmail}" };
        }
        catch (Exception ex)
        {
            return new EmailTestResult { Success = false, Message = $"发送测试邮件失败：{ex.Message}" };
        }
    }

    public async Task<EmailTestResult> TestSmtpFromCollaborationAsync(long collaborationId, long userId)
    {
        var collaboration = await _collaborationRepository.GetByIdAsync(collaborationId);
        if (collaboration == null || collaboration.UserId != userId)
        {
            return new EmailTestResult { Success = false, Message = "协作不存在" };
        }

        if (string.IsNullOrEmpty(collaboration.Config))
        {
            return new EmailTestResult { Success = false, Message = "协作未配置SMTP信息，请先在编辑模式下配置并保存SMTP信息" };
        }

        var smtpConfig = ParseSmtpFromConfig(collaboration.Config);
        if (smtpConfig == null)
        {
            return new EmailTestResult { Success = false, Message = "协作配置中未找到SMTP配置，请先配置SMTP信息" };
        }

        var validationError = ValidateSmtpConfig(smtpConfig.Server, smtpConfig.Username, smtpConfig.Password, smtpConfig.FromEmail);
        if (validationError != null)
        {
            return new EmailTestResult { Success = false, Message = validationError };
        }

        _logger.LogInformation("测试协作SMTP配置: CollaborationId={Id}, Server={Server}", collaborationId, smtpConfig.Server);

        try
        {
            using var client = new SmtpClient(smtpConfig.Server, smtpConfig.Port);
            client.UseDefaultCredentials = false;
            client.EnableSsl = smtpConfig.EnableSsl;
            client.Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password);
            client.Timeout = 30000;

            var subject = $"MAF Studio 测试邮件 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var body = $"这是一封测试邮件。\n\n发送时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n协作名称: {collaboration.Name}\n\n如果您收到这封邮件，说明SMTP配置正确。";

            using var message = new MailMessage(smtpConfig.FromEmail, smtpConfig.FromEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            client.Send(message);
            return new EmailTestResult { Success = true, Message = $"成功发送测试邮件到 {smtpConfig.FromEmail}" };
        }
        catch (Exception ex)
        {
            return new EmailTestResult { Success = false, Message = $"发送测试邮件失败：{ex.Message}" };
        }
    }

    private string? ValidateSmtpConfig(string? server, string? username, string? password, string? fromEmail)
    {
        if (string.IsNullOrEmpty(server))
        {
            return "SMTP配置不完整，请检查服务器地址是否填写";
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return "SMTP配置不完整，请检查用户名和密码是否填写";
        }

        if (string.IsNullOrEmpty(fromEmail))
        {
            return "SMTP配置不完整，请检查发件人邮箱是否填写";
        }

        return null;
    }

    private SmtpConfig? ParseSmtpFromConfig(string configJson)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
            if (config == null || !config.ContainsKey("smtp"))
            {
                _logger.LogWarning("配置中未找到smtp节点");
                return null;
            }

            var smtpElement = (JsonElement)config["smtp"];
            return JsonSerializer.Deserialize<SmtpConfig>(smtpElement.GetRawText());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "解析SMTP配置失败");
            return null;
        }
    }
}
