using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Services;

public class TaskContextService : ITaskContextService
{
    private Core.Entities.CollaborationTask? _currentTask;

    public void SetCurrentTask(Core.Entities.CollaborationTask task)
    {
        _currentTask = task;
    }

    public GitConfig? GetGitConfig()
    {
        if (_currentTask == null)
        {
            return null;
        }

        return new GitConfig
        {
            Url = _currentTask.GitUrl,
            Branch = _currentTask.GitBranch,
            Token = _currentTask.GitCredentials
        };
    }

    public void Clear()
    {
        _currentTask = null;
    }
}
