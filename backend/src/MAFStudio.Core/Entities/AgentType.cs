using MAFStudio.Core.Utils;
using System.Text.Json;

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

    public string? DefaultConfiguration { get; set; }

    public long? LlmConfigId { get; set; }

    public bool IsSystem { get; set; } = false;

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 默认系统提示词（从DefaultConfiguration中提取）
    /// </summary>
    public string? DefaultSystemPrompt
    {
        get
        {
            if (string.IsNullOrEmpty(DefaultConfiguration)) return null;
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(DefaultConfiguration);
                if (config != null && config.TryGetValue("systemPrompt", out var prompt))
                {
                    return prompt?.ToString();
                }
            }
            catch { }
            return null;
        }
    }

    /// <summary>
    /// 默认温度（从DefaultConfiguration中提取）
    /// </summary>
    public double DefaultTemperature
    {
        get
        {
            if (string.IsNullOrEmpty(DefaultConfiguration)) return 0.7;
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(DefaultConfiguration);
                if (config != null && config.TryGetValue("temperature", out var temp))
                {
                    return Convert.ToDouble(temp);
                }
            }
            catch { }
            return 0.7;
        }
    }

    /// <summary>
    /// 默认最大Token数（从DefaultConfiguration中提取）
    /// </summary>
    public int DefaultMaxTokens
    {
        get
        {
            if (string.IsNullOrEmpty(DefaultConfiguration)) return 4096;
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(DefaultConfiguration);
                if (config != null && config.TryGetValue("maxTokens", out var tokens))
                {
                    return Convert.ToInt32(tokens);
                }
            }
            catch { }
            return 4096;
        }
    }

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }

    /// <summary>
    /// 设置默认配置
    /// </summary>
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
