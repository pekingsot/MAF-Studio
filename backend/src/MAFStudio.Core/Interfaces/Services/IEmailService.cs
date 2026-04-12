namespace MAFStudio.Core.Interfaces.Services;

public interface IEmailService
{
    Task<EmailTestResult> TestSmtpAsync(SmtpTestConfig config);
    Task<EmailTestResult> TestSmtpFromCollaborationAsync(long collaborationId, long userId);
}

public class SmtpTestConfig
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

public class EmailTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
