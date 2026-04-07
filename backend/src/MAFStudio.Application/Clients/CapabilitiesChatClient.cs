using System.Reflection;
using MAFStudio.Application.Capabilities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Clients;

/// <summary>
/// 基于MAF的能力工具ChatClient
/// 使用CapabilityManager动态注入工具到ChatOptions
/// 配合UseFunctionInvocation()中间件使用
/// </summary>
public class CapabilitiesChatClient : DelegatingChatClient
{
    private readonly CapabilityManager _capabilityManager;
    private readonly ILogger? _logger;
    private readonly List<AITool> _tools;

    public CapabilitiesChatClient(
        IChatClient innerClient,
        CapabilityManager capabilityManager,
        ILogger? logger = null)
        : base(innerClient)
    {
        _capabilityManager = capabilityManager;
        _logger = logger;
        _tools = BuildTools();
        
        _logger?.LogInformation("CapabilitiesChatClient 初始化，注册了 {Count} 个工具", _tools.Count);
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveOptions = EnsureToolsInOptions(options);
        _logger?.LogInformation("[CapabilitiesChatClient] GetResponseAsync - 工具数量: {Count}", effectiveOptions.Tools?.Count ?? 0);
        
        var response = await base.GetResponseAsync(chatMessages, effectiveOptions, cancellationToken);
        
        var functionCalls = response.Messages
            .SelectMany(m => m.Contents ?? [])
            .OfType<FunctionCallContent>()
            .ToList();
        
        if (functionCalls.Count > 0)
        {
            _logger?.LogInformation("[CapabilitiesChatClient] 响应包含 {Count} 个工具调用请求", functionCalls.Count);
            foreach (var call in functionCalls)
            {
                _logger?.LogInformation("[CapabilitiesChatClient] 工具调用: {Name}, 参数: {Args}", call.Name, call.Arguments);
            }
        }
        
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveOptions = EnsureToolsInOptions(options);
        _logger?.LogInformation("[CapabilitiesChatClient] GetStreamingResponseAsync - 工具数量: {Count}", effectiveOptions.Tools?.Count ?? 0);
        
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, effectiveOptions, cancellationToken))
        {
            yield return update;
        }
    }

    private ChatOptions EnsureToolsInOptions(ChatOptions? options)
    {
        var effectiveOptions = options ?? new ChatOptions();
        
        if (effectiveOptions.Tools == null || effectiveOptions.Tools.Count == 0)
        {
            effectiveOptions.Tools = _tools;
        }
        
        return effectiveOptions;
    }

    private List<AITool> BuildTools()
    {
        var tools = new List<AITool>();
        
        foreach (var capability in _capabilityManager.GetAllCapabilities())
        {
            foreach (var method in capability.GetTools())
            {
                try
                {
                    var tool = CreateAIFunction(method, capability);
                    tools.Add(tool);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "创建工具 {MethodName} 失败", method.Name);
                }
            }
        }

        return tools;
    }

    private AIFunction CreateAIFunction(MethodInfo method, ICapability capability)
    {
        var toolAttr = method.GetCustomAttribute<ToolAttribute>();
        var description = toolAttr?.Description ?? method.Name;

        var delegateMethod = Delegate.CreateDelegate(
            GetFuncType(method),
            capability,
            method);

        return AIFunctionFactory.Create(delegateMethod, new AIFunctionFactoryOptions
        {
            Name = method.Name,
            Description = description
        });
    }

    private Type GetFuncType(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var returnType = method.ReturnType;

        if (returnType == typeof(void))
        {
            return parameters.Length switch
            {
                0 => typeof(Action),
                1 => typeof(Action<>).MakeGenericType(parameters[0].ParameterType),
                2 => typeof(Action<,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType),
                3 => typeof(Action<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType),
                4 => typeof(Action<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType),
                5 => typeof(Action<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType),
                6 => typeof(Action<,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType),
                7 => typeof(Action<,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType),
                8 => typeof(Action<,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, parameters[7].ParameterType),
                _ => throw new NotSupportedException($"不支持 {parameters.Length} 个参数的 Action")
            };
        }

        if (returnType == typeof(Task))
        {
            return parameters.Length switch
            {
                0 => typeof(Func<Task>),
                1 => typeof(Func<,>).MakeGenericType(parameters[0].ParameterType, typeof(Task)),
                2 => typeof(Func<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, typeof(Task)),
                3 => typeof(Func<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, typeof(Task)),
                4 => typeof(Func<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, typeof(Task)),
                5 => typeof(Func<,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, typeof(Task)),
                6 => typeof(Func<,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, typeof(Task)),
                7 => typeof(Func<,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, typeof(Task)),
                8 => typeof(Func<,,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, parameters[7].ParameterType, typeof(Task)),
                _ => throw new NotSupportedException($"不支持 {parameters.Length} 个参数的 Func<Task>")
            };
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = returnType.GetGenericArguments()[0];
            return parameters.Length switch
            {
                0 => typeof(Func<>).MakeGenericType(returnType),
                1 => typeof(Func<,>).MakeGenericType(parameters[0].ParameterType, returnType),
                2 => typeof(Func<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, returnType),
                3 => typeof(Func<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, returnType),
                4 => typeof(Func<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, returnType),
                5 => typeof(Func<,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, returnType),
                6 => typeof(Func<,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, returnType),
                7 => typeof(Func<,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, returnType),
                8 => typeof(Func<,,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, parameters[7].ParameterType, returnType),
                _ => throw new NotSupportedException($"不支持 {parameters.Length} 个参数的 Func<Task<T>>")
            };
        }

        return parameters.Length switch
        {
            0 => typeof(Func<>).MakeGenericType(returnType),
            1 => typeof(Func<,>).MakeGenericType(parameters[0].ParameterType, returnType),
            2 => typeof(Func<,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, returnType),
            3 => typeof(Func<,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, returnType),
            4 => typeof(Func<,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, returnType),
            5 => typeof(Func<,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, returnType),
            6 => typeof(Func<,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, returnType),
            7 => typeof(Func<,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, returnType),
            8 => typeof(Func<,,,,,,,,>).MakeGenericType(parameters[0].ParameterType, parameters[1].ParameterType, parameters[2].ParameterType, parameters[3].ParameterType, parameters[4].ParameterType, parameters[5].ParameterType, parameters[6].ParameterType, parameters[7].ParameterType, returnType),
            _ => throw new NotSupportedException($"不支持 {parameters.Length} 个参数的 Func")
        };
    }

    public IReadOnlyList<AITool> GetTools() => _tools.AsReadOnly();
}
