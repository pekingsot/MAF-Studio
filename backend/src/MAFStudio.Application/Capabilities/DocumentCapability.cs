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

    [Tool("读取JSON文件")]
    public string ReadJson(string filePath)
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

    [Tool("写入JSON文件")]
    public string WriteJson(string filePath, string jsonContent)
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

    [Tool("读取CSV文件")]
    public string ReadCsv(string filePath)
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

    [Tool("写入CSV文件")]
    public string WriteCsv(string filePath, string content)
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

    [Tool("读取Markdown文件")]
    public string ReadMarkdown(string filePath)
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

    [Tool("写入Markdown文件")]
    public string WriteMarkdown(string filePath, string content)
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

    [Tool("读取文本文件（指定编码）")]
    public string ReadTextFile(string filePath, string encoding = "UTF-8")
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

    [Tool("写入文本文件（指定编码）")]
    public string WriteTextFile(string filePath, string content, string encoding = "UTF-8")
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

    [Tool("创建目录结构")]
    public string CreateDirectoryStructure(string basePath, string structure)
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

    [Tool("获取文件信息")]
    public string GetFileInfo(string filePath)
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
