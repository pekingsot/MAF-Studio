using System.Reflection;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace MAFStudio.Application.Capabilities;

public class EmailCapability : ICapability
{
    public string Name => "邮件操作";
    public string Description => "提供邮件发送、附件发送、HTML邮件发送等操作能力";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(EmailCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("发送简单邮件")]
    public string SendSimpleEmail(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string subject,
        string body,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.UseDefaultCredentials = false;
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);
            client.Timeout = 30000;

            using var message = new MailMessage(fromEmail, toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            client.Send(message);
            return $"成功发送邮件到 {toEmail}";
        }
        catch (Exception ex)
        {
            return $"发送邮件失败：{ex.Message}\n详细信息：{ex.InnerException?.Message}";
        }
    }

    [Tool("发送HTML邮件")]
    public string SendHtmlEmail(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string subject,
        string htmlBody,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage(fromEmail, toEmail);
            message.Subject = subject;
            message.Body = htmlBody;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            client.Send(message);
            return $"成功发送HTML邮件到 {toEmail}";
        }
        catch (Exception ex)
        {
            return $"发送HTML邮件失败：{ex.Message}";
        }
    }

    [Tool("发送带附件的邮件")]
    public string SendEmailWithAttachment(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string subject,
        string body,
        string attachmentPath,
        bool enableSsl = true)
    {
        try
        {
            if (!File.Exists(attachmentPath))
            {
                return $"错误：附件文件 {attachmentPath} 不存在";
            }

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage(fromEmail, toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            var attachment = new Attachment(attachmentPath);
            message.Attachments.Add(attachment);

            client.Send(message);
            return $"成功发送带附件的邮件到 {toEmail}，附件：{Path.GetFileName(attachmentPath)}";
        }
        catch (Exception ex)
        {
            return $"发送带附件的邮件失败：{ex.Message}";
        }
    }

    [Tool("发送带多个附件的邮件")]
    public string SendEmailWithMultipleAttachments(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string subject,
        string body,
        string attachmentPaths,
        bool enableSsl = true)
    {
        try
        {
            var paths = attachmentPaths.Split('|', ',', ';');
            var missingFiles = paths.Where(p => !File.Exists(p.Trim())).ToList();
            
            if (missingFiles.Count > 0)
            {
                return $"错误：以下附件文件不存在：{string.Join(", ", missingFiles)}";
            }

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage(fromEmail, toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            foreach (var path in paths)
            {
                var trimmedPath = path.Trim();
                if (File.Exists(trimmedPath))
                {
                    var attachment = new Attachment(trimmedPath);
                    message.Attachments.Add(attachment);
                }
            }

            client.Send(message);
            return $"成功发送带 {message.Attachments.Count} 个附件的邮件到 {toEmail}";
        }
        catch (Exception ex)
        {
            return $"发送带多个附件的邮件失败：{ex.Message}";
        }
    }

    [Tool("发送群发邮件")]
    public string SendBulkEmail(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmails,
        string subject,
        string body,
        bool enableSsl = true)
    {
        try
        {
            var emailList = toEmails.Split('|', ',', ';')
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            if (emailList.Count == 0)
            {
                return "错误：没有有效的收件人地址";
            }

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            foreach (var email in emailList)
            {
                message.To.Add(email);
            }

            client.Send(message);
            return $"成功群发邮件到 {emailList.Count} 个收件人";
        }
        catch (Exception ex)
        {
            return $"群发邮件失败：{ex.Message}";
        }
    }

    [Tool("发送抄送邮件")]
    public string SendEmailWithCc(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string ccEmails,
        string subject,
        string body,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            var ccList = ccEmails.Split('|', ',', ';')
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e));

            foreach (var cc in ccList)
            {
                message.CC.Add(cc);
            }

            client.Send(message);
            return $"成功发送邮件到 {toEmail}，抄送给 {message.CC.Count} 人";
        }
        catch (Exception ex)
        {
            return $"发送抄送邮件失败：{ex.Message}";
        }
    }

    [Tool("发送密送邮件")]
    public string SendEmailWithBcc(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string bccEmails,
        string subject,
        string body,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            var bccList = bccEmails.Split('|', ',', ';')
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e));

            foreach (var bcc in bccList)
            {
                message.Bcc.Add(bcc);
            }

            client.Send(message);
            return $"成功发送邮件到 {toEmail}，密送给 {message.Bcc.Count} 人";
        }
        catch (Exception ex)
        {
            return $"发送密送邮件失败：{ex.Message}";
        }
    }

    [Tool("发送回复邮件")]
    public string SendReplyEmail(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string replyToEmail,
        string subject,
        string body,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage(fromEmail, toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;
            message.ReplyToList.Add(replyToEmail);

            client.Send(message);
            return $"成功发送回复邮件到 {toEmail}，回复地址：{replyToEmail}";
        }
        catch (Exception ex)
        {
            return $"发送回复邮件失败：{ex.Message}";
        }
    }

    [Tool("发送带优先级的邮件")]
    public string SendEmailWithPriority(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        string fromEmail,
        string toEmail,
        string subject,
        string body,
        string priority,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);

            using var message = new MailMessage(fromEmail, toEmail);
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            message.Priority = priority.ToLower() switch
            {
                "high" => MailPriority.High,
                "low" => MailPriority.Low,
                _ => MailPriority.Normal
            };

            client.Send(message);
            return $"成功发送 {priority} 优先级邮件到 {toEmail}";
        }
        catch (Exception ex)
        {
            return $"发送带优先级的邮件失败：{ex.Message}";
        }
    }

    [Tool("测试SMTP连接")]
    public string TestSmtpConnection(
        string smtpServer,
        int smtpPort,
        string username,
        string password,
        bool enableSsl = true)
    {
        try
        {
            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(username, password);
            client.Timeout = 5000;

            return $"SMTP连接测试成功：\n" +
                   $"  服务器：{smtpServer}\n" +
                   $"  端口：{smtpPort}\n" +
                   $"  SSL：{(enableSsl ? "启用" : "禁用")}\n" +
                   $"  用户名：{username}";
        }
        catch (Exception ex)
        {
            return $"SMTP连接测试失败：{ex.Message}";
        }
    }
}
