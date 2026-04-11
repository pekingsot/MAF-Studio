namespace MAFStudio.Core.Interfaces.Services;

public class GitConfig
{
    public string? Url { get; set; }
    public string? Branch { get; set; }
    public string? Token { get; set; }
}

public interface ITaskContextService
{
    void SetCurrentTask(Core.Entities.CollaborationTask task);
    GitConfig? GetGitConfig();
    void Clear();
}
