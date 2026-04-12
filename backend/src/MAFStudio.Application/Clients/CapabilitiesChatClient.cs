using System.Reflection;
using System.Runtime.CompilerServices;
using MAFStudio.Application.Capabilities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Clients;

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
        
        try
        {
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
        catch (ArgumentException ex) when (ex.Message.Contains("required parameter") || ex.Message.Contains("arguments dictionary"))
        {
            _logger?.LogWarning(ex, "[CapabilitiesChatClient] 工具调用缺少必需参数: {Message}", ex.Message);
            return new ChatResponse([new ChatMessage(ChatRole.Assistant,
                $"I encountered an error while trying to use a tool: a required parameter was missing ({ex.Message}). " +
                "I will try a different approach without using that tool.")]);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is ArgumentException argEx 
            && (argEx.Message.Contains("required parameter") || argEx.Message.Contains("arguments dictionary")))
        {
            _logger?.LogWarning(ex, "[CapabilitiesChatClient] 工具调用缺少必需参数: {Message}", ex.InnerException.Message);
            return new ChatResponse([new ChatMessage(ChatRole.Assistant,
                $"I encountered an error while trying to use a tool: a required parameter was missing ({ex.InnerException.Message}). " +
                "I will try a different approach without using that tool.")]);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var effectiveOptions = EnsureToolsInOptions(options);
        _logger?.LogInformation("[CapabilitiesChatClient] GetStreamingResponseAsync - 工具数量: {Count}", effectiveOptions.Tools?.Count ?? 0);
        
        var enumerator = base.GetStreamingResponseAsync(chatMessages, effectiveOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatResponseUpdate? update = null;
            ChatResponseUpdate? errorUpdate = null;
            
            try
            {
                if (!await enumerator.MoveNextAsync())
                    break;
                update = enumerator.Current;
            }
            catch (ArgumentException ex) when (ex.Message.Contains("required parameter") || ex.Message.Contains("arguments dictionary"))
            {
                _logger?.LogWarning(ex, "[CapabilitiesChatClient] 流式响应中工具调用缺少必需参数: {Message}", ex.Message);
                errorUpdate = new ChatResponseUpdate(ChatRole.Assistant,
                    $"I encountered an error while trying to use a tool: a required parameter was missing ({ex.Message}). " +
                    "I will try a different approach without using that tool.");
            }
            catch (InvalidOperationException ex) when (ex.InnerException is ArgumentException argEx 
                && (argEx.Message.Contains("required parameter") || argEx.Message.Contains("arguments dictionary")))
            {
                _logger?.LogWarning(ex, "[CapabilitiesChatClient] 流式响应中工具调用缺少必需参数: {Message}", ex.InnerException.Message);
                errorUpdate = new ChatResponseUpdate(ChatRole.Assistant,
                    $"I encountered an error while trying to use a tool: a required parameter was missing ({ex.InnerException.Message}). " +
                    "I will try a different approach without using that tool.");
            }

            if (errorUpdate != null)
            {
                yield return errorUpdate;
                yield break;
            }

            if (update != null)
            {
                yield return update;
            }
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

        var aiFunction = AIFunctionFactory.Create(method, capability, new AIFunctionFactoryOptions
        {
            Name = method.Name,
            Description = description
        });
        
        _logger?.LogInformation("工具 {Name} JSON Schema: {Schema}", method.Name, aiFunction.JsonSchema);
        
        return aiFunction;
    }

    public IReadOnlyList<AITool> GetTools() => _tools.AsReadOnly();
}
