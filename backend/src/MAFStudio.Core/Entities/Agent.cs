using MAFStudio.Core.Enums;
using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("agents")]
public class Agent : BaseEntityWithUpdate
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = "Assistant";

    public string Configuration { get; set; } = "{}";

    public string? Avatar { get; set; }

    public string UserId { get; set; } = string.Empty;

    public AgentStatus Status { get; set; } = AgentStatus.Inactive;

    public long? LlmConfigId { get; set; }

    public long? LlmModelConfigId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Dapper.Contrib.Extensions.Write(false)]
    public LlmConfig? LlmConfig { get; set; }

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
