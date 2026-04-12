using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MAFStudio.Application.Capabilities;

/// <summary>
/// 网络请求能力，提供 HTTP 请求和网页抓取功能
/// </summary>
public class WebCapability : ICapability
{
    private static readonly Lazy<HttpClient> _sharedHttpClient = new(() =>
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Add("User-Agent", "MAF-Studio/1.0");
        return client;
    });

    public string Name => "WebCapability";
    public string Description => "网络请求能力，支持 HTTP 请求、网页抓取和文件下载";

    protected HttpClient HttpClient => _sharedHttpClient.Value;

    public IEnumerable<MethodInfo> GetTools()
    {
        return GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Send an HTTP GET request and return the response.")]
    public async Task<string> HttpGetAsync(
        [Description("The URL to request, e.g. 'https://api.example.com/data'")] string url,
        [Description("Custom HTTP headers as 'Key1:Value1;Key2:Value2' format, e.g. 'Authorization:Bearer token123'. Optional")] string? headers = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            if (!string.IsNullOrEmpty(headers))
            {
                foreach (var header in ParseHeaderString(headers))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Serialize(new
            {
                StatusCode = (int)response.StatusCode,
                StatusMessage = response.StatusCode.ToString(),
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Content = content.Length > 10000 ? content.Substring(0, 10000) + "...(已截断)" : content
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"请求失败: {ex.Message}";
        }
    }

    [Tool("Send an HTTP POST request with an optional request body.")]
    public async Task<string> HttpPostAsync(
        [Description("The URL to post to")] string url,
        [Description("Request body content. Optional")] string? body = null,
        [Description("Content type, e.g. 'application/json'. Default 'application/json'")] string? contentType = null,
        [Description("Custom HTTP headers as 'Key1:Value1;Key2:Value2' format. Optional")] string? headers = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
            }

            if (!string.IsNullOrEmpty(headers))
            {
                foreach (var header in ParseHeaderString(headers))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Serialize(new
            {
                StatusCode = (int)response.StatusCode,
                StatusMessage = response.StatusCode.ToString(),
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Content = content.Length > 10000 ? content.Substring(0, 10000) + "...(已截断)" : content
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"请求失败: {ex.Message}";
        }
    }

    [Tool("Fetch a web page and extract its text content.")]
    public async Task<string> FetchUrlAsync(
        [Description("The URL of the web page to fetch")] string url,
        [Description("Request timeout in seconds. Default 30")] int timeoutSeconds = 30)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var response = await HttpClient.GetAsync(url, cts.Token);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cts.Token);
            
            var title = ExtractTitle(content);
            var textContent = ExtractTextContent(content);

            return JsonSerializer.Serialize(new
            {
                Url = url,
                Title = title,
                TextContent = textContent.Length > 5000 ? textContent.Substring(0, 5000) + "...(已截断)" : textContent,
                ContentLength = content.Length
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"获取网页失败: {ex.Message}";
        }
    }

    [Tool("Download a file from a URL and save it to a local path.")]
    public async Task<string> DownloadFileAsync(
        [Description("The URL of the file to download")] string url,
        [Description("Absolute local path where the file will be saved, e.g. '/home/user/downloads/file.zip'")] string savePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileBytes = await HttpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(savePath, fileBytes);

            return $"成功下载文件到: {savePath}，大小: {fileBytes.Length} 字节";
        }
        catch (Exception ex)
        {
            return $"下载文件失败: {ex.Message}";
        }
    }

    [Tool("Send an HTTP PUT request with an optional request body.")]
    public async Task<string> HttpPutAsync(
        [Description("The URL to put to")] string url,
        [Description("Request body content. Optional")] string? body = null,
        [Description("Content type, e.g. 'application/json'. Default 'application/json'")] string? contentType = null,
        [Description("Custom HTTP headers as 'Key1:Value1;Key2:Value2' format. Optional")] string? headers = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "application/json");
            }

            if (!string.IsNullOrEmpty(headers))
            {
                foreach (var header in ParseHeaderString(headers))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Serialize(new
            {
                StatusCode = (int)response.StatusCode,
                StatusMessage = response.StatusCode.ToString(),
                Content = content.Length > 10000 ? content.Substring(0, 10000) + "...(已截断)" : content
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"请求失败: {ex.Message}";
        }
    }

    [Tool("Send an HTTP DELETE request.")]
    public async Task<string> HttpDeleteAsync(
        [Description("The URL to delete")] string url,
        [Description("Custom HTTP headers as 'Key1:Value1;Key2:Value2' format. Optional")] string? headers = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);

            if (!string.IsNullOrEmpty(headers))
            {
                foreach (var header in ParseHeaderString(headers))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Serialize(new
            {
                StatusCode = (int)response.StatusCode,
                StatusMessage = response.StatusCode.ToString(),
                Content = content.Length > 10000 ? content.Substring(0, 10000) + "...(已截断)" : content
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"请求失败: {ex.Message}";
        }
    }

    [Tool("Check if a URL is accessible and return the HTTP status code.")]
    public async Task<string> CheckUrlAsync(
        [Description("The URL to check accessibility")] string url)
    {
        try
        {
            var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return JsonSerializer.Serialize(new
            {
                Url = url,
                IsAccessible = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                StatusMessage = response.StatusCode.ToString(),
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Url = url,
                IsAccessible = false,
                Error = ex.Message
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    private static string ExtractTitle(string html)
    {
        var titleStart = html.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
        if (titleStart < 0) return "";
        
        var titleEnd = html.IndexOf("</title>", titleStart, StringComparison.OrdinalIgnoreCase);
        if (titleEnd < 0) return "";

        var titleContentStart = html.IndexOf('>', titleStart) + 1;
        return titleContentStart < titleEnd 
            ? html.Substring(titleContentStart, titleEnd - titleContentStart).Trim() 
            : "";
    }

    private static string ExtractTextContent(string html)
    {
        var text = new StringBuilder();
        var inTag = false;
        var inScript = false;
        var inStyle = false;

        for (int i = 0; i < html.Length; i++)
        {
            var c = html[i];

            if (c == '<')
            {
                inTag = true;
                var tagStart = i;
                var tagEnd = html.IndexOf('>', tagStart);
                if (tagEnd > tagStart)
                {
                    var tag = html.Substring(tagStart + 1, tagEnd - tagStart - 1).ToLower();
                    if (tag.StartsWith("script")) inScript = true;
                    if (tag.StartsWith("style")) inStyle = true;
                    if (tag.StartsWith("/script")) inScript = false;
                    if (tag.StartsWith("/style")) inStyle = false;
                }
                continue;
            }

            if (c == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag && !inScript && !inStyle)
            {
                if (c == '\n' || c == '\r' || c == '\t')
                {
                    if (text.Length > 0 && text[^1] != ' ')
                        text.Append(' ');
                }
                else
                {
                    text.Append(c);
                }
            }
        }

        var result = text.ToString();
        while (result.Contains("  "))
            result = result.Replace("  ", " ");

        return result.Trim();
    }

    private static Dictionary<string, string> ParseHeaderString(string headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pairs = headers.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var colonIndex = pair.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = pair.Substring(0, colonIndex).Trim();
                var value = pair.Substring(colonIndex + 1).Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }
        return result;
    }
}
