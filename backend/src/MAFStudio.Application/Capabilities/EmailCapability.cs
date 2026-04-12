using System.ComponentModel;
using System.Reflection;
using System.Net;
using System.Net.Mail;
using System.Text;
using MAFStudio.Application.Context;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MAFStudio.Application.Capabilities;

public class EmailCapability : ICapability
{
    public string Name => "Email";
    public string Description => "Send emails using SMTP configuration from the collaboration settings";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(EmailCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Send a plain text email. SMTP settings are read from the collaboration configuration automatically. IMPORTANT: SMTP must be configured in the collaboration settings before using this tool.")]
    public string SendEmail(
        [Description("Recipient email address. Use semicolons to separate multiple recipients, e.g. 'user1@example.com;user2@example.com'")] string toEmail,
        [Description("Email subject line")] string subject,
        [Description("Plain text email content")] string body)
    {
        var (client, fromEmail) = CreateSmtpClient();
        
        try
        {
            var recipients = ParseEmailAddresses(toEmail);
            
            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            foreach (var recipient in recipients)
            {
                message.To.Add(recipient);
            }
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            client.Send(message);
            return $"Successfully sent email to {toEmail}";
        }
        catch (Exception ex)
        {
            return $"Failed to send email: {ex.Message}\nDetails: {ex.InnerException?.Message}";
        }
    }

    [Tool("Send an HTML formatted email. SMTP settings are read from the collaboration configuration automatically. IMPORTANT: SMTP must be configured in the collaboration settings before using this tool.")]
    public string SendHtmlEmail(
        [Description("Recipient email address. Use semicolons to separate multiple recipients")] string toEmail,
        [Description("Email subject line")] string subject,
        [Description("HTML content of the email body")] string htmlBody,
        [Description("CC recipients separated by semicolons. Optional")] string? ccEmails = null,
        [Description("BCC recipients separated by semicolons. Optional")] string? bccEmails = null)
    {
        var (client, fromEmail) = CreateSmtpClient();
        
        try
        {
            var recipients = ParseEmailAddresses(toEmail);
            
            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            foreach (var recipient in recipients)
            {
                message.To.Add(recipient);
            }
            message.Subject = subject;
            message.Body = htmlBody;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            if (!string.IsNullOrWhiteSpace(ccEmails))
            {
                foreach (var cc in ParseEmailAddresses(ccEmails))
                {
                    message.CC.Add(cc);
                }
            }

            if (!string.IsNullOrWhiteSpace(bccEmails))
            {
                foreach (var bcc in ParseEmailAddresses(bccEmails))
                {
                    message.Bcc.Add(bcc);
                }
            }

            client.Send(message);
            return $"Successfully sent HTML email to {toEmail}";
        }
        catch (Exception ex)
        {
            return $"Failed to send HTML email: {ex.Message}";
        }
    }

    [Tool("Send an email with file attachments. SMTP settings are read from the collaboration configuration automatically. IMPORTANT: SMTP must be configured in the collaboration settings before using this tool.")]
    public string SendEmailWithAttachments(
        [Description("Recipient email address. Use semicolons to separate multiple recipients")] string toEmail,
        [Description("Email subject line")] string subject,
        [Description("Email content (plain text or HTML)")] string body,
        [Description("Absolute paths of files to attach, separated by semicolons or commas, e.g. '/home/user/report.pdf;/home/user/data.csv'")] string attachmentPaths,
        [Description("Set to true if body contains HTML. Default false")] bool isHtml = false)
    {
        var (client, fromEmail) = CreateSmtpClient();
        
        try
        {
            var paths = attachmentPaths.Split(';', ',', '|')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();
            
            var missingFiles = paths.Where(p => !File.Exists(p)).ToList();
            if (missingFiles.Count > 0)
            {
                return $"Error: The following attachment files do not exist: {string.Join(", ", missingFiles)}";
            }

            var recipients = ParseEmailAddresses(toEmail);
            
            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            foreach (var recipient in recipients)
            {
                message.To.Add(recipient);
            }
            message.Subject = subject;
            message.Body = body;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;
            message.IsBodyHtml = isHtml;

            foreach (var path in paths)
            {
                var attachment = new Attachment(path);
                message.Attachments.Add(attachment);
            }

            client.Send(message);
            return $"Successfully sent email with {paths.Count} attachment(s) to {toEmail}";
        }
        catch (Exception ex)
        {
            return $"Failed to send email with attachments: {ex.Message}";
        }
    }

    private (SmtpClient Client, string FromEmail) CreateSmtpClient()
    {
        var configContext = CollaborationConfigContext.Current;
        if (configContext == null)
        {
            throw new InvalidOperationException("No collaboration configuration context found. Ensure this tool is called within a collaboration workflow.");
        }
        
        var smtpConfig = configContext.GetConfigValue<SmtpConfig>("smtp");
        if (smtpConfig == null)
        {
            throw new InvalidOperationException("SMTP is not configured in the collaboration settings. Please configure SMTP server, port, username, password and sender email first.");
        }
        
        if (string.IsNullOrEmpty(smtpConfig.Server) || 
            string.IsNullOrEmpty(smtpConfig.Username) || 
            string.IsNullOrEmpty(smtpConfig.Password) ||
            string.IsNullOrEmpty(smtpConfig.FromEmail))
        {
            throw new InvalidOperationException("SMTP configuration is incomplete. Please check server, username, password and sender email.");
        }

        var client = new SmtpClient(smtpConfig.Server, smtpConfig.Port);
        client.UseDefaultCredentials = false;
        client.EnableSsl = smtpConfig.EnableSsl;
        client.Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password);
        client.Timeout = 30000;

        return (client, smtpConfig.FromEmail);
    }

    private static List<string> ParseEmailAddresses(string emails)
    {
        return emails.Split(';', ',', '|')
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();
    }
}

public class SmtpConfig
{
    [JsonPropertyName("server")]
    public string Server { get; set; } = string.Empty;
    
    [JsonPropertyName("port")]
    public int Port { get; set; } = 587;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("fromEmail")]
    public string FromEmail { get; set; } = string.Empty;
    
    [JsonPropertyName("enableSsl")]
    public bool EnableSsl { get; set; } = true;
}
