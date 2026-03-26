namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("llm_model_configs")]
public class LlmModelConfig
{
    [Dapper.Contrib.Extensions.Key]
    public Guid Id { get; set; }

    public Guid LlmConfigId { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public string? ExtraConfig { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
