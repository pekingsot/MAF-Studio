using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using MAFStudio.Core.Interfaces.Services;

namespace MAFStudio.Application.Capabilities;

public class GitCapability : ICapability
{
    private readonly ITaskContextService _taskContext;

    public GitCapability(ITaskContextService taskContext)
    {
        _taskContext = taskContext;
    }

    public string Name => "Git操作";
    public string Description => "提供Git版本控制操作能力";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(GitCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Clone the Git repository configured in the current task to a local directory. The repository URL, branch and access token are automatically obtained from the task configuration - do NOT provide them.")]
    public string CloneRepository(
        [Description("Absolute local directory path where the repository should be cloned, e.g. '/workspace/my-project'")] string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            return "Error: Missing required parameter 'targetDirectory'. Please provide a local directory path where the repository should be cloned, e.g. /workspace/my-project";
        }

        var gitConfig = _taskContext.GetGitConfig();
        
        if (gitConfig == null || string.IsNullOrWhiteSpace(gitConfig.Url))
        {
            return "Error: No Git repository configured for the current task. Please configure a Git repository URL in the task settings.";
        }

        try
        {
            var authenticatedUrl = BuildAuthenticatedUrl(gitConfig.Url, gitConfig.Token);
            var branch = gitConfig.Branch ?? "main";
            
            var args = $"clone -b {branch} \"{authenticatedUrl}\" \"{targetDirectory}\"";
            var result = ExecuteGitCommand(args);
            
            return result.Contains("fatal") 
                ? $"Clone failed: {result}" 
                : $"Successfully cloned repository: {gitConfig.Url} (branch: {branch}) to {targetDirectory}";
        }
        catch (Exception ex)
        {
            return $"Clone failed: {ex.Message}";
        }
    }

    [Tool("Get the Git status of a local repository. Shows changed, staged and untracked files.")]
    public string GetStatus(
        [Description("Absolute path to the local Git repository directory, e.g. '/workspace/my-project'")] string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        try
        {
            return ExecuteGitCommand("status", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"获取状态失败：{ex.Message}";
        }
    }

    [Tool("Add files to the Git staging area. You must call AddFiles before Commit.")]
    public string AddFiles(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("Glob pattern of files to add, e.g. 'src/*.cs' or '.' for all files. Default '.'")] string filePattern = ".")
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        try
        {
            return ExecuteGitCommand($"add {filePattern}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"添加文件失败：{ex.Message}";
        }
    }

    [Tool("Commit staged changes to the local Git repository. You must call AddFiles before Commit.")]
    public string Commit(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("The commit message describing the changes")] string message,
        [Description("Commit author name. Default 'MAF Studio Agent'")] string? authorName = null,
        [Description("Commit author email. Default 'agent@maf-studio.local'")] string? authorEmail = null)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        if (string.IsNullOrWhiteSpace(message))
        {
            return "错误：缺少必需参数 message（提交信息）。请提供提交信息";
        }
        
        try
        {
            var userName = authorName ?? "MAF Studio Agent";
            var userEmail = authorEmail ?? "agent@maf-studio.local";
            
            ExecuteGitCommand($"config user.name \"{userName}\"", repositoryPath);
            ExecuteGitCommand($"config user.email \"{userEmail}\"", repositoryPath);
            
            return ExecuteGitCommand($"commit -m \"{message}\"", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"提交失败：{ex.Message}";
        }
    }

    [Tool("Push local commits to the remote Git repository. IMPORTANT: You must Commit before Push. If Push fails, call Pull first, then Push again.")]
    public string Push(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("Remote branch name to push to. Empty string uses current branch")] string branch = "",
        [Description("Whether to set the upstream branch. Default false")] bool setUpstream = false)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        var gitConfig = _taskContext.GetGitConfig();
        var token = gitConfig?.Token;
        
        try
        {
            if (!string.IsNullOrWhiteSpace(branch))
            {
                var command = setUpstream ? $"push -u origin {branch}" : $"push origin {branch}";
                return ExecuteGitCommandWithAuth(command, repositoryPath, gitConfig?.Url, token);
            }
            else
            {
                return ExecuteGitCommandWithAuth("push", repositoryPath, gitConfig?.Url, token);
            }
        }
        catch (Exception ex)
        {
            return $"推送失败：{ex.Message}";
        }
    }

    [Tool("Pull the latest changes from the remote Git repository. Use this when Push fails due to remote changes, then retry Push.")]
    public string Pull(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        var gitConfig = _taskContext.GetGitConfig();
        var token = gitConfig?.Token;
        
        try
        {
            return ExecuteGitCommandWithAuth("pull", repositoryPath, gitConfig?.Url, token);
        }
        catch (Exception ex)
        {
            return $"拉取失败：{ex.Message}";
        }
    }

    [Tool("Create and switch to a new Git branch.")]
    public string CreateBranch(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("Name of the new branch to create, e.g. 'feature/new-login'")] string branchName)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return "错误：缺少必需参数 branchName（分支名称）。请提供新分支的名称";
        }
        
        try
        {
            return ExecuteGitCommand($"checkout -b {branchName}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"创建分支失败：{ex.Message}";
        }
    }

    [Tool("Switch to an existing Git branch.")]
    public string CheckoutBranch(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("Name of the branch to switch to, e.g. 'main' or 'develop'")] string branchName)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return "错误：缺少必需参数 branchName（分支名称）。请提供要切换的分支名称";
        }
        
        try
        {
            return ExecuteGitCommand($"checkout {branchName}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"切换分支失败：{ex.Message}";
        }
    }

    [Tool("View the Git commit history.")]
    public string GetLog(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("Number of commits to show. Default 10")] int count = 10)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        try
        {
            return ExecuteGitCommand($"log --oneline -{count}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"查看历史失败：{ex.Message}";
        }
    }

    [Tool("View the Git diff of changes.")]
    public string GetDiff(
        [Description("Absolute path to the local Git repository directory")] string repositoryPath,
        [Description("Specific file path to diff, relative to repo root. Empty string shows all changes")] string file = "")
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            return "错误：缺少必需参数 repositoryPath（仓库本地路径）。请提供Git仓库的本地路径";
        }
        
        try
        {
            var command = string.IsNullOrEmpty(file) ? "diff" : $"diff {file}";
            return ExecuteGitCommand(command, repositoryPath);
        }
        catch (Exception ex)
        {
            return $"查看差异失败：{ex.Message}";
        }
    }

    private string ExecuteGitCommand(string arguments, string? workingDirectory = null)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            processInfo.WorkingDirectory = workingDirectory;
        }

        using var process = new Process { StartInfo = processInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return error.Length > 0 ? error.ToString() : output.ToString();
    }

    private string ExecuteGitCommandWithAuth(string arguments, string workingDirectory, string? repoUrl, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(repoUrl))
        {
            var authenticatedUrl = BuildAuthenticatedUrl(repoUrl, token);
            ExecuteGitCommand($"remote set-url origin \"{authenticatedUrl}\"", workingDirectory);
        }
        
        return ExecuteGitCommand(arguments, workingDirectory);
    }

    private string BuildAuthenticatedUrl(string repoUrl, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return repoUrl;
        }

        if (repoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return repoUrl.Replace("https://", $"https://{token}@");
        }

        return repoUrl;
    }
}
