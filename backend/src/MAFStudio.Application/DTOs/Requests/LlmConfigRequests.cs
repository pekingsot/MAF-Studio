namespace MAFStudio.Application.DTOs.Requests;

public class CreateLlmConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? DefaultModel { get; set; }
}

public class UpdateLlmConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? DefaultModel { get; set; }
}

public class TestLlmRequest
{
    public string Prompt { get; set; } = string.Empty;
    public long? ModelConfigId { get; set; }
}
