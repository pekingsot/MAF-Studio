using MAFStudio.Core.Enums;
using MAFStudio.Core.Utils;

namespace MAFStudio.Core.Entities;

[Dapper.Contrib.Extensions.Table("collaborations")]
public class Collaboration : BaseEntityWithUpdate
{
    [Dapper.Contrib.Extensions.Key]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Path { get; set; }

    public CollaborationStatus Status { get; set; } = CollaborationStatus.Active;

    public string UserId { get; set; } = string.Empty;

    public string? GitRepositoryUrl { get; set; }

    public string? GitBranch { get; set; }

    public string? GitUsername { get; set; }

    public string? GitEmail { get; set; }

    public string? GitAccessToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 生成新的雪花ID
    /// </summary>
    public void GenerateId()
    {
        Id = SnowflakeIdGenerator.Instance.NextId();
    }
}
