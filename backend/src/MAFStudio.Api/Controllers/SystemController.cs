using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    [HttpGet("environment")]
    [AllowAnonymous]
    public async Task<ActionResult<EnvironmentInfo>> GetEnvironmentInfo()
    {
        var info = new EnvironmentInfo
        {
            DotNetVersion = GetDotNetVersion(),
            GitVersion = await GetCommandVersionAsync("git", "--version"),
            PythonVersion = await GetCommandVersionAsync("python3", "--version"),
            NodeVersion = await GetCommandVersionAsync("node", "--version"),
            OsInfo = Environment.OSVersion.ToString(),
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            OsArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            Containerized = IsRunningInContainer()
        };

        return Ok(info);
    }

    private string GetDotNetVersion()
    {
        return Environment.Version.ToString();
    }

    private async Task<string> GetCommandVersionAsync(string command, string args)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrWhiteSpace(output))
            {
                output = await process.StandardError.ReadToEndAsync();
            }

            return output.Trim();
        }
        catch (Exception)
        {
            return "Not Installed";
        }
    }

    private bool IsRunningInContainer()
    {
        return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
               System.IO.File.Exists("/.dockerenv");
    }
}

public class EnvironmentInfo
{
    public string DotNetVersion { get; set; } = string.Empty;
    public string GitVersion { get; set; } = string.Empty;
    public string PythonVersion { get; set; } = string.Empty;
    public string NodeVersion { get; set; } = string.Empty;
    public string OsInfo { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public string Runtime { get; set; } = string.Empty;
    public string OsArchitecture { get; set; } = string.Empty;
    public string ProcessArchitecture { get; set; } = string.Empty;
    public bool Containerized { get; set; }
}
