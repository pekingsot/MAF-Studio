namespace MAFStudio.Application.VOs;

public class LlmConfigVo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? DefaultModel { get; set; }
    public long UserId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<LlmModelConfigVo> Models { get; set; } = new();
}

public class LlmModelConfigVo
{
    public long Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public decimal Temperature { get; set; }
    public int MaxTokens { get; set; }
    public int ContextWindow { get; set; }
    public decimal? TopP { get; set; }
    public decimal? FrequencyPenalty { get; set; }
    public decimal? PresencePenalty { get; set; }
    public string? StopSequences { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; }
    public DateTime? LastTestTime { get; set; }
    public int AvailabilityStatus { get; set; }
    public string? TestResult { get; set; }
    public DateTime CreatedAt { get; set; }
}
