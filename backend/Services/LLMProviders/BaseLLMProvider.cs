using MAFStudio.Backend.Data;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MAFStudio.Backend.Services.LLMProviders
{
    /// <summary>
    /// 大模型供应商基类
    /// 提供通用的HTTP请求和流式测试功能
    /// </summary>
    public abstract class BaseLLMProvider : ILLMProvider
    {
        protected readonly ILogger _logger;
        protected readonly HttpClient _httpClient;

        protected BaseLLMProvider(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public abstract string ProviderId { get; }
        public abstract string DisplayName { get; }
        public abstract string DefaultEndpoint { get; }
        public abstract string DefaultModel { get; }

        /// <summary>
        /// 获取测试请求的URL
        /// </summary>
        protected virtual string GetTestUrl(string endpoint)
        {
            return $"{endpoint.TrimEnd('/')}/chat/completions";
        }

        /// <summary>
        /// 构建测试请求体 - 子类可重写自定义请求格式
        /// </summary>
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
        /// 获取请求头 - 子类可重写添加额外请求头
        /// </summary>
        protected virtual Dictionary<string, string> GetHeaders(LLMConfig config)
        {
            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {config.ApiKey}" },
                { "Content-Type", "application/json" }
            };
        }

        /// <summary>
        /// 测试连通性 - 使用流式请求
        /// </summary>
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
        public virtual async Task<(bool success, string message, int latencyMs)> TestModelConnectionAsync(LLMConfig config, LLMModelConfig modelConfig)
        {
            return await TestConnectionAsync(config, modelConfig.ModelName);
        }

        /// <summary>
        /// 解析错误消息
        /// </summary>
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
        /// 获取可用模型列表 - 子类实现
        /// </summary>
        public abstract Task<List<string>> GetAvailableModelsAsync(LLMConfig config);
    }
}
