using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MAFStudio.Application.Capabilities;

public class DocumentCapability : ICapability
{
    public string Name => "文档操作";
    public string Description => "提供各种文档格式的读写操作能力";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(DocumentCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Read and pretty-print a JSON file.")]
    public string ReadJson(
        [Description("Absolute path to the JSON file, e.g. '/home/user/project/config.json'")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var jsonDoc = JsonDocument.Parse(content);
            
            return JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"读取JSON失败：{ex.Message}";
        }
    }

    [Tool("Write a JSON file with pretty-printed formatting. Creates parent directories if needed.")]
    public string WriteJson(
        [Description("Absolute path to the JSON file")] string filePath,
        [Description("Valid JSON string to write")] string jsonContent)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var jsonDoc = JsonDocument.Parse(jsonContent);
            var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(filePath, formatted, Encoding.UTF8);
            return $"成功写入JSON文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"写入JSON失败：{ex.Message}";
        }
    }

    [Tool("Read a CSV file and return its content.")]
    public string ReadCsv(
        [Description("Absolute path to the CSV file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            var result = new StringBuilder();
            
            foreach (var line in lines)
            {
                result.AppendLine(line);
            }
            
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"读取CSV失败：{ex.Message}";
        }
    }

    [Tool("Write content to a CSV file. Creates parent directories if needed.")]
    public string WriteCsv(
        [Description("Absolute path to the CSV file")] string filePath,
        [Description("CSV content to write")] string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
            return $"成功写入CSV文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"写入CSV失败：{ex.Message}";
        }
    }

    [Tool("Read a Markdown file and return its content.")]
    public string ReadMarkdown(
        [Description("Absolute path to the Markdown file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            return File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return $"读取Markdown失败：{ex.Message}";
        }
    }

    [Tool("Write content to a Markdown file. Creates parent directories if needed.")]
    public string WriteMarkdown(
        [Description("Absolute path to the Markdown file")] string filePath,
        [Description("Markdown content to write")] string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
            return $"成功写入Markdown文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"写入Markdown失败：{ex.Message}";
        }
    }

    [Tool("Read a text file with a specific character encoding.")]
    public string ReadTextFile(
        [Description("Absolute path to the file")] string filePath,
        [Description("Character encoding name: UTF-8, UTF-16, ASCII, GB2312, GBK. Default 'UTF-8'")] string encoding = "UTF-8")
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            var enc = GetEncoding(encoding);
            return File.ReadAllText(filePath, enc);
        }
        catch (Exception ex)
        {
            return $"读取文本文件失败：{ex.Message}";
        }
    }

    [Tool("Write content to a text file with a specific character encoding. Creates parent directories if needed.")]
    public string WriteTextFile(
        [Description("Absolute path to the file")] string filePath,
        [Description("Text content to write")] string content,
        [Description("Character encoding name: UTF-8, UTF-16, ASCII, GB2312, GBK. Default 'UTF-8'")] string encoding = "UTF-8")
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var enc = GetEncoding(encoding);
            File.WriteAllText(filePath, content, enc);
            return $"成功写入文本文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"写入文本文件失败：{ex.Message}";
        }
    }

    [Tool("Create multiple directories under a base path.")]
    public string CreateDirectoryStructure(
        [Description("The root directory path")] string basePath,
        [Description("Newline-separated list of relative directory paths to create, e.g. 'src\\nsrc/models\\ntests'")] string structure)
    {
        try
        {
            var directories = structure.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var created = new List<string>();

            foreach (var dir in directories)
            {
                var trimmed = dir.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var fullPath = Path.Combine(basePath, trimmed);
                Directory.CreateDirectory(fullPath);
                created.Add(fullPath);
            }

            return $"成功创建目录结构：\n{string.Join("\n", created)}";
        }
        catch (Exception ex)
        {
            return $"创建目录结构失败：{ex.Message}";
        }
    }

    [Tool("Get detailed information about a file including size, timestamps and attributes.")]
    public string GetFileInfo(
        [Description("Absolute path to the file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            var info = new FileInfo(filePath);
            var result = new StringBuilder();
            result.AppendLine($"文件名：{info.Name}");
            result.AppendLine($"路径：{info.FullName}");
            result.AppendLine($"大小：{info.Length} bytes");
            result.AppendLine($"创建时间：{info.CreationTime}");
            result.AppendLine($"修改时间：{info.LastWriteTime}");
            result.AppendLine($"访问时间：{info.LastAccessTime}");
            result.AppendLine($"扩展名：{info.Extension}");
            result.AppendLine($"只读：{info.IsReadOnly}");
            
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"获取文件信息失败：{ex.Message}";
        }
    }

    private Encoding GetEncoding(string encodingName)
    {
        return encodingName.ToUpper() switch
        {
            "UTF-8" => Encoding.UTF8,
            "UTF-16" => Encoding.Unicode,
            "ASCII" => Encoding.ASCII,
            "GB2312" => Encoding.GetEncoding("GB2312"),
            "GBK" => Encoding.GetEncoding("GBK"),
            _ => Encoding.UTF8
        };
    }
}
