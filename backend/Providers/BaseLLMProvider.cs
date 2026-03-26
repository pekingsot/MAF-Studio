using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace MAFStudio.Backend.Providers
{
    /// <summary>
    /// 大模型供应商抽象基类
    /// 提供通用的HTTP请求和流式测试功能
    /// 使用模板方法模式，子类只需实现特定的配置和请求格式
    /// </summary>
    public abstract class BaseLLMProvider : ILLMProvider
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// HTTP客户端
        /// </summary>
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        protected BaseLLMProvider(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120)
            };
        }

        /// <summary>
        /// 供应商标识（子类实现）
        /// </summary>
        public abstract string ProviderId { get; }

        /// <summary>
        /// 供应商显示名称（子类实现）
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 默认端点地址（子类实现）
        /// </summary>
        public abstract string DefaultEndpoint { get; }

        /// <summary>
        /// 默认模型（子类实现）
        /// </summary>
        public abstract string DefaultModel { get; }

        /// <summary>
        /// 获取测试请求的URL
        /// 子类可重写以支持不同的API路径
        /// </summary>
        /// <param name="endpoint">端点地址</param>
        /// <returns>完整URL</returns>
        protected virtual string GetTestUrl(string endpoint)
        {
            return $"{endpoint.TrimEnd('/')}/chat/completions";
        }

        /// <summary>
        /// 构建测试请求体
        /// 子类可重写以支持不同的请求格式
        /// </summary>
        /// <param name="modelId">模型ID</param>
        /// <returns>请求对象</returns>
        protected virtual object BuildTestRequest(string modelId)
        {
            return new
            {
                model = modelId,
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                },
                max_tokens = 1,
                stream = true
            };
        }

        /// <summary>
        /// 获取请求头
        /// 子类可重写以添加额外的请求头
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <returns>请求头字典</returns>
        protected virtual Dictionary<string, string> GetHeaders(LLMConfig config)
        {
            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {config.ApiKey}" },
                { "Content-Type", "application/json" }
            };
        }

        /// <summary>
        /// 测试连通性
        /// 使用流式请求以支持需要流式输出的模型
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <param name="modelName">模型名称，为空使用默认模型</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        public virtual async Task<(bool success, string message, int latencyMs)> TestConnectionAsync(LLMConfig config, string? modelName = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var endpoint = config.Endpoint ?? DefaultEndpoint;
                var modelId = modelName ?? DefaultModel;
                var url = GetTestUrl(endpoint);

                _logger.LogInformation("测试{Provider}连接 - URL: {Url}, Model: {Model}", DisplayName, url, modelId);

                var testRequest = BuildTestRequest(modelId);
                var jsonContent = JsonSerializer.Serialize(testRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                foreach (var header in GetHeaders(config))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                stopwatch.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("测试{Provider}连接 - Status: {Status}, Response: {Response}",
                    DisplayName, response.StatusCode, responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "连接成功", (int)stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    var errorMsg = ParseErrorMessage(responseContent);
                    return (false, $"连接失败: {errorMsg}", (int)stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "测试{Provider}连接失败", DisplayName);
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                return (false, $"连接失败: {errorMsg}", (int)stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// 测试指定模型的连通性
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <param name="modelConfig">模型配置</param>
        /// <returns>测试结果（是否成功、消息、延迟毫秒数）</returns>
        public virtual async Task<(bool success, string message, int latencyMs)> TestModelConnectionAsync(LLMConfig config, LLMModelConfig modelConfig)
        {
            return await TestConnectionAsync(config, modelConfig.ModelName);
        }

        /// <summary>
        /// 解析错误消息
        /// 从API响应中提取有意义的错误信息
        /// </summary>
        /// <param name="errorContent">错误响应内容</param>
        /// <returns>解析后的错误消息</returns>
        protected virtual string ParseErrorMessage(string errorContent)
        {
            try
            {
                var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                if (errorObj.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg))
                {
                    return msg.GetString() ?? errorContent;
                }
                if (errorObj.TryGetProperty("message", out var msg2))
                {
                    return msg2.GetString() ?? errorContent;
                }
            }
            catch { }
            return errorContent;
        }

        /// <summary>
        /// 获取可用模型列表
        /// 子类实现以返回支持的模型列表
        /// </summary>
        /// <param name="config">大模型配置</param>
        /// <returns>模型名称列表</returns>
        public abstract Task<List<string>> GetAvailableModelsAsync(LLMConfig config);

        /// <summary>
        /// 构建聊天请求体
        /// 子类可重写以支持不同的请求格式
        /// </summary>
        protected virtual object BuildChatRequest(string modelId, List<ChatMessage> messages)
        {
            return new
            {
                model = modelId,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                stream = true
            };
        }

        /// <summary>
        /// 流式聊天完成
        /// </summary>
        public virtual async IAsyncEnumerable<string> ChatStreamAsync(
            LLMConfig config, 
            LLMModelConfig modelConfig, 
            List<ChatMessage> messages, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var endpoint = config.Endpoint ?? DefaultEndpoint;
            var url = GetTestUrl(endpoint);
            var modelId = modelConfig.ModelName;

            _logger.LogInformation("流式聊天 - Provider: {Provider}, Model: {Model}, URL: {Url}", 
                DisplayName, modelId, url);

            var chatRequest = BuildChatRequest(modelId, messages);
            var jsonContent = JsonSerializer.Serialize(chatRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            foreach (var header in GetHeaders(config))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    
                    if (data == "[DONE]")
                        break;

                    var content2 = ParseStreamContent(data);
                    if (!string.IsNullOrEmpty(content2))
                    {
                        yield return content2;
                    }
                }
            }
        }

        /// <summary>
        /// 解析流式响应内容
        /// </summary>
        protected virtual string? ParseStreamContent(string data)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(data);
                if (json.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                {
                    var firstChoice = choices.EnumerateArray().FirstOrDefault();
                    if (firstChoice.TryGetProperty("delta", out var delta) && 
                        delta.TryGetProperty("content", out var content))
                    {
                        return content.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析流式响应失败: {Data}", data);
            }
            return null;
        }
    }
}
