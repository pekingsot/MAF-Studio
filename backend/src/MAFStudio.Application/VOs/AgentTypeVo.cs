namespace MAFStudio.Application.VOs;

/// <summary>
/// 智能体类型VO
/// </summary>
public class AgentTypeVo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? DefaultSystemPrompt { get; set; }
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; }
}
