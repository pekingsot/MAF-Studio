namespace MAFStudio.Application.VOs;

public abstract class BaseVo
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
