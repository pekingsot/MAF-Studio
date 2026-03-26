namespace MAFStudio.Application.VOs;

public class LlmConfigVo : BaseVo
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? DefaultModel { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<LlmModelConfigVo> Models { get; set; } = new();
}

public class LlmModelConfigVo
{
    public long Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}
