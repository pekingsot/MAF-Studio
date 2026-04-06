using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MAFStudio.Application.Capabilities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Clients;

/// <summary>
/// 支持工具调用的 ChatClient 包装器
/// 使用 FunctionInvokingChatClient 自动处理工具调用
/// </summary>
public class ToolCallingChatClient : DelegatingChatClient
{
    private readonly CapabilityManager _capabilityManager;
    private readonly ILogger<ToolCallingChatClient>? _logger;
    private readonly List<AITool> _tools;

    public ToolCallingChatClient(
        IChatClient innerClient,
        CapabilityManager capabilityManager,
        ILogger<ToolCallingChatClient>? logger = null)
        : base(innerClient)
    {
        _capabilityManager = capabilityManager;
        _logger = logger;
        _tools = BuildTools();
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveOptions = AddToolsToOptions(options);
        
        var iteration = 0;
        var messages = chatMessages.ToList();
        ChatResponse? response = null;

        while (iteration < 10)
        {
            iteration++;
            
            response = await base.GetResponseAsync(messages, effectiveOptions, cancellationToken);
            
            var lastMessage = response.Messages.LastOrDefault();
            if (lastMessage == null) break;

            var toolCalls = lastMessage.Contents?.OfType<FunctionCallContent>().ToList();
            if (toolCalls == null || toolCalls.Count == 0)
            {
                break;
            }

            _logger?.LogInformation("收到 {Count} 个工具调用请求", toolCalls.Count);
            messages.Add(lastMessage);

            foreach (var toolCall in toolCalls)
            {
                var result = await ExecuteToolCallAsync(toolCall);
                var toolResultMessage = new ChatMessage(ChatRole.Tool, [result]);
                messages.Add(toolResultMessage);
            }
        }

        return response!;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveOptions = AddToolsToOptions(options);
        
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, effectiveOptions, cancellationToken))
        {
            yield return update;
        }
    }

    private ChatOptions AddToolsToOptions(ChatOptions? options)
    {
        var effectiveOptions = options ?? new ChatOptions();
        effectiveOptions.Tools = _tools;
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

        _logger?.LogInformation("注册了 {Count} 个工具", tools.Count);
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

    private async Task<FunctionResultContent> ExecuteToolCallAsync(FunctionCallContent toolCall)
    {
        try
        {
            _logger?.LogInformation("执行工具: {Name}, 参数: {Args}", toolCall.Name, toolCall.Arguments);

            var tool = _capabilityManager.GetTool(toolCall.Name!);
            if (tool == null)
            {
                return new FunctionResultContent(toolCall.CallId, $"错误：工具 {toolCall.Name} 不存在");
            }

            var capability = _capabilityManager.GetAllCapabilities()
                .FirstOrDefault(c => c.GetTools().Contains(tool));
            
            if (capability == null)
            {
                return new FunctionResultContent(toolCall.CallId, $"错误：找不到工具 {toolCall.Name} 所属的能力");
            }

            var args = ParseArguments(toolCall.Arguments);
            var parameters = tool.GetParameters();
            var paramValues = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name ?? $"arg{i}";
                
                if (args.TryGetValue(paramName, out var value))
                {
                    paramValues[i] = ConvertValue(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    paramValues[i] = param.DefaultValue;
                }
                else
                {
                    paramValues[i] = param.ParameterType.IsValueType 
                        ? Activator.CreateInstance(param.ParameterType) 
                        : null;
                }
            }

            var result = tool.Invoke(capability, paramValues);

            string resultString;
            if (result is Task<string> stringTask)
            {
                resultString = await stringTask;
            }
            else if (result is Task task)
            {
                await task;
                resultString = "执行完成";
            }
            else if (result is string str)
            {
                resultString = str;
            }
            else
            {
                resultString = JsonSerializer.Serialize(result);
            }

            _logger?.LogInformation("工具 {Name} 执行结果: {Result}", toolCall.Name, resultString);
            return new FunctionResultContent(toolCall.CallId, resultString);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "执行工具 {Name} 失败", toolCall.Name);
            return new FunctionResultContent(toolCall.CallId, $"执行失败: {ex.Message}");
        }
    }

    private Dictionary<string, object?> ParseArguments(IDictionary<string, object?>? args)
    {
        if (args == null) return new Dictionary<string, object?>();
        return new Dictionary<string, object?>(args, StringComparer.OrdinalIgnoreCase);
    }

    private object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        if (targetType == typeof(string))
        {
            return value?.ToString();
        }

        if (value is JsonElement jsonElement)
        {
            var jsonValue = jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value
            };
            
            if (jsonValue != null && targetType != typeof(object))
            {
                return Convert.ChangeType(jsonValue, targetType);
            }
            return jsonValue;
        }

        var sourceType = value.GetType();
        if (targetType.IsAssignableFrom(sourceType))
        {
            return value;
        }

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return value;
        }
    }
}
