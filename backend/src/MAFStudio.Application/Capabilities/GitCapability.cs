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

    [Tool("克隆当前任务的Git仓库到本地目录。参数: localPath(本地存放路径)。返回: 克隆结果信息。注意：此工具会自动使用任务中配置的Git仓库地址、分支和访问令牌")]
    public string CloneRepository(string localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return "错误：缺少必需参数 localPath（本地存放路径）。请提供本地目录路径，例如：/path/to/repo";
        }

        var gitConfig = _taskContext.GetGitConfig();
        
        if (gitConfig == null || string.IsNullOrWhiteSpace(gitConfig.Url))
        {
            return "错误：当前任务没有配置 Git 仓库。请在任务配置中设置 Git 仓库地址";
        }

        try
        {
            var authenticatedUrl = BuildAuthenticatedUrl(gitConfig.Url, gitConfig.Token);
            var branch = gitConfig.Branch ?? "main";
            
            var args = $"clone -b {branch} \"{authenticatedUrl}\" \"{localPath}\"";
            var result = ExecuteGitCommand(args);
            
            return result.Contains("fatal") 
                ? $"克隆失败：{result}" 
                : $"成功克隆仓库：{gitConfig.Url}（分支：{branch}）到 {localPath}";
        }
        catch (Exception ex)
        {
            return $"克隆仓库失败：{ex.Message}";
        }
    }

    [Tool("获取Git状态。参数: repositoryPath(仓库本地路径)。返回: Git状态信息")]
    public string GetStatus(string repositoryPath)
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

    [Tool("添加文件到Git暂存区。参数: repositoryPath(仓库本地路径), filePattern(文件模式，默认'.'表示所有文件)。返回: 操作结果")]
    public string AddFiles(string repositoryPath, string filePattern = ".")
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

    [Tool("提交Git更改到本地仓库。参数: repositoryPath(仓库本地路径), message(提交信息), authorName(可选，提交者名称), authorEmail(可选，提交者邮箱)。返回: 提交结果")]
    public string Commit(string repositoryPath, string message, string? authorName = null, string? authorEmail = null)
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

    [Tool("推送Git提交到远程仓库。参数: repositoryPath(仓库本地路径), branch(可选，分支名，不提供则使用当前分支), setUpstream(可选，是否设置上游分支)。返回: 推送结果。注意: 推送前必须先Commit")]
    public string Push(string repositoryPath, string branch = "", bool setUpstream = false)
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

    [Tool("拉取远程仓库的最新更改。参数: repositoryPath(仓库本地路径)。返回: 拉取结果。注意: 当Push失败时，先调用此工具拉取最新代码，再Push")]
    public string Pull(string repositoryPath)
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

    [Tool("创建分支。参数: repositoryPath(仓库本地路径), branchName(分支名称)。返回: 操作结果")]
    public string CreateBranch(string repositoryPath, string branchName)
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

    [Tool("切换分支。参数: repositoryPath(仓库本地路径), branchName(分支名称)。返回: 操作结果")]
    public string CheckoutBranch(string repositoryPath, string branchName)
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

    [Tool("查看提交历史。参数: repositoryPath(仓库本地路径), count(可选，显示数量，默认10)。返回: 提交历史")]
    public string GetLog(string repositoryPath, int count = 10)
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

    [Tool("查看文件差异。参数: repositoryPath(仓库本地路径), file(可选，文件路径，不提供则显示所有差异)。返回: 差异信息")]
    public string GetDiff(string repositoryPath, string file = "")
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
