using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Application.Services;

namespace MAFStudio.Application.Capabilities;

public class CapabilityManager
{
    private readonly List<ICapability> _capabilities = new();
    private readonly IServiceProvider _serviceProvider;

    public CapabilityManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterBuiltInCapabilities();
    }

    private void RegisterBuiltInCapabilities()
    {
        RegisterCapability(new FileCapability());
        RegisterCapability(ActivatorUtilities.CreateInstance<GitCapability>(_serviceProvider));
        RegisterCapability(new DocumentCapability());
        RegisterCapability(new WebCapability());
        RegisterCapability(new CodeCapability());
        RegisterCapability(new SearchCapability());
        RegisterCapability(new ArchiveCapability());
        RegisterCapability(new TimeCapability());
        RegisterCapability(new EmailCapability());
    }

    public void RegisterCapability(ICapability capability)
    {
        _capabilities.Add(capability);
    }

    public IEnumerable<ICapability> GetAllCapabilities()
    {
        return _capabilities;
    }

    public IEnumerable<MethodInfo> GetAllTools()
    {
        return _capabilities.SelectMany(c => c.GetTools());
    }

    public Dictionary<string, List<MethodInfo>> GetToolsByCapability()
    {
        return _capabilities.ToDictionary(
            c => c.Name,
            c => c.GetTools().ToList()
        );
    }

    public MethodInfo? GetTool(string name)
    {
        return GetAllTools().FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<object?> ExecuteToolAsync(string toolName, params object[] parameters)
    {
        var tool = GetTool(toolName);
        if (tool == null)
        {
            throw new NotFoundException($"工具 {toolName} 不存在");
        }

        var capability = _capabilities.FirstOrDefault(c => c.GetTools().Contains(tool));
        if (capability == null)
        {
            throw new NotFoundException($"找不到工具 {toolName} 所属的能力");
        }

        try
        {
            var result = tool.Invoke(capability, parameters);
            
            if (result is Task task)
            {
                await task;
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    return taskType.GetProperty("Result")?.GetValue(task);
                }
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"执行工具 {toolName} 失败：{ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }
}
