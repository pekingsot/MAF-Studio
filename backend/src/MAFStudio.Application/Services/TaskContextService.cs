using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class TaskContextService : ITaskContextService
{
    private static readonly AsyncLocal<Core.Entities.CollaborationTask?> _currentTask = new();

    public void SetCurrentTask(Core.Entities.CollaborationTask task)
    {
        _currentTask.Value = task;
    }

    public Core.Entities.CollaborationTask? GetCurrentTask()
    {
        return _currentTask.Value;
    }

    public GitConfig? GetGitConfig()
    {
        var task = _currentTask.Value;
        if (task == null)
        {
            return null;
        }

        return new GitConfig
        {
            Url = task.GitUrl,
            Branch = task.GitBranch,
            Token = task.GitCredentials
        };
    }

    public void Clear()
    {
        _currentTask.Value = null;
    }
}
