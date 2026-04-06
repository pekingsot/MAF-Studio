using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MAFStudio.Application.Capabilities;

public class GitCapability : ICapability
{
    public string Name => "Git操作";
    public string Description => "提供Git版本控制操作能力";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(GitCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("克隆Git仓库")]
    public string CloneRepository(string repositoryUrl, string localPath)
    {
        try
        {
            var result = ExecuteGitCommand($"clone {repositoryUrl} \"{localPath}\"");
            return result.Contains("fatal") 
                ? $"克隆失败：{result}" 
                : $"成功克隆仓库：{repositoryUrl}";
        }
        catch (Exception ex)
        {
            return $"克隆仓库失败：{ex.Message}";
        }
    }

    [Tool("获取Git状态")]
    public string GetStatus(string repositoryPath)
    {
        try
        {
            return ExecuteGitCommand("status", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"获取状态失败：{ex.Message}";
        }
    }

    [Tool("添加文件到暂存区")]
    public string AddFiles(string repositoryPath, string filePattern = ".")
    {
        try
        {
            return ExecuteGitCommand($"add {filePattern}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"添加文件失败：{ex.Message}";
        }
    }

    [Tool("提交更改")]
    public string Commit(string repositoryPath, string message, string? authorName = null, string? authorEmail = null)
    {
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

    [Tool("推送到远程仓库")]
    public string Push(string repositoryPath, string branch = "", bool setUpstream = false)
    {
        try
        {
            var command = setUpstream ? $"push -u origin {branch}" : "push";
            return ExecuteGitCommand(command, repositoryPath);
        }
        catch (Exception ex)
        {
            return $"推送失败：{ex.Message}";
        }
    }

    [Tool("拉取远程更改")]
    public string Pull(string repositoryPath)
    {
        try
        {
            return ExecuteGitCommand("pull", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"拉取失败：{ex.Message}";
        }
    }

    [Tool("创建分支")]
    public string CreateBranch(string repositoryPath, string branchName)
    {
        try
        {
            return ExecuteGitCommand($"checkout -b {branchName}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"创建分支失败：{ex.Message}";
        }
    }

    [Tool("切换分支")]
    public string CheckoutBranch(string repositoryPath, string branchName)
    {
        try
        {
            return ExecuteGitCommand($"checkout {branchName}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"切换分支失败：{ex.Message}";
        }
    }

    [Tool("查看提交历史")]
    public string GetLog(string repositoryPath, int count = 10)
    {
        try
        {
            return ExecuteGitCommand($"log --oneline -{count}", repositoryPath);
        }
        catch (Exception ex)
        {
            return $"查看历史失败：{ex.Message}";
        }
    }

    [Tool("查看文件差异")]
    public string GetDiff(string repositoryPath, string file = "")
    {
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
}
