using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("llm_test_records")]
public class LlmTestRecord
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public long LlmConfigId { get; set; }

    public long? LlmModelConfigId { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public string? Response { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int TokensUsed { get; set; }

    public int ResponseTimeMs { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTime TestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
