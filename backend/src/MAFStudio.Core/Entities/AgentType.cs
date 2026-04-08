using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agent_types")]
public class AgentType
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    [Column("default_configuration")]
    public string? DefaultConfiguration { get; set; }

    [Column("llm_config_id")]
    public long? LlmConfigId { get; set; }

    [Column("is_system")]
    public bool IsSystem { get; set; } = false;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? DefaultSystemPrompt
    {
        get
        {
            if (string.IsNullOrEmpty(DefaultConfiguration)) return null;
            try
            {
                using var doc = JsonDocument.Parse(DefaultConfiguration);
                if (doc.RootElement.TryGetProperty("systemPrompt", out var prop))
                {
                    return prop.GetString();
                }
            }
            catch { }
            return null;
        }
    }

    public double DefaultTemperature
    {
        get
        {
            if (string.IsNullOrEmpty(DefaultConfiguration)) return 0.7;
            try
            {
                using var doc = JsonDocument.Parse(DefaultConfiguration);
                if (doc.RootElement.TryGetProperty("temperature", out var prop))
                {
                    return prop.GetDouble();
                }
            }
            catch { }
            return 0.7;
        }
    }

    public int DefaultMaxTokens
    {
        get
        {
            if (string.IsNullOrEmpty(DefaultConfiguration)) return 4096;
            try
            {
                using var doc = JsonDocument.Parse(DefaultConfiguration);
                if (doc.RootElement.TryGetProperty("maxTokens", out var prop))
                {
                    return prop.GetInt32();
                }
            }
            catch { }
            return 4096;
        }
    }

    public void SetDefaultConfiguration(string? systemPrompt, double temperature, int maxTokens)
    {
        var config = new Dictionary<string, object>
        {
            { "systemPrompt", systemPrompt ?? "" },
            { "temperature", temperature },
            { "maxTokens", maxTokens }
        };
        DefaultConfiguration = JsonSerializer.Serialize(config);
    }
}
