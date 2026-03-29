namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("llm_model_configs")]
public class LlmModelConfig
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long LlmConfigId { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public decimal Temperature { get; set; } = 0.7m;

    public int MaxTokens { get; set; } = 4096;

    public int ContextWindow { get; set; } = 8192;

    public decimal? TopP { get; set; }

    public decimal? FrequencyPenalty { get; set; }

    public decimal? PresencePenalty { get; set; }

    public string? StopSequences { get; set; }

    public DateTime? LastTestTime { get; set; }

    public int AvailabilityStatus { get; set; } = 0;

    public string? TestResult { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
