namespace MAFStudio.Application.DTOs;

public class CollaborationResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<ChatMessageDto> Messages { get; set; } = new();
    public string? Error { get; set; }
}

public class ChatMessageDto
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
