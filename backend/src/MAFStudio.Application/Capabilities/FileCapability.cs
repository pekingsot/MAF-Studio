using System.Reflection;
using System.Text;

namespace MAFStudio.Application.Capabilities;

public class FileCapability : ICapability
{
    public string Name => "文件操作";
    public string Description => "提供文件读写、创建、删除等操作能力";

    public IEnumerable<MethodInfo> GetTools()
    {
        return typeof(FileCapability).GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("读取文件内容")]
    public string ReadFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            return File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return $"读取文件失败：{ex.Message}";
        }
    }

    [Tool("写入文件内容")]
    public string WriteFile(string filePath, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
            return $"成功写入文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"写入文件失败：{ex.Message}";
        }
    }

    [Tool("追加文件内容")]
    public string AppendFile(string filePath, string content)
    {
        try
        {
            File.AppendAllText(filePath, content, Encoding.UTF8);
            return $"成功追加内容到文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"追加文件失败：{ex.Message}";
        }
    }

    [Tool("删除文件")]
    public string DeleteFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"错误：文件 {filePath} 不存在";

            File.Delete(filePath);
            return $"成功删除文件：{filePath}";
        }
        catch (Exception ex)
        {
            return $"删除文件失败：{ex.Message}";
        }
    }

    [Tool("列出目录下的文件")]
    public string ListFiles(string directoryPath, string searchPattern = "*")
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return $"错误：目录 {directoryPath} 不存在";

            var files = Directory.GetFiles(directoryPath, searchPattern);
            var result = new StringBuilder();
            result.AppendLine($"目录 {directoryPath} 下的文件：");
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                result.AppendLine($"  {info.Name} ({info.Length} bytes)");
            }
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"列出文件失败：{ex.Message}";
        }
    }

    [Tool("创建目录")]
    public string CreateDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
            return $"成功创建目录：{directoryPath}";
        }
        catch (Exception ex)
        {
            return $"创建目录失败：{ex.Message}";
        }
    }

    [Tool("删除目录")]
    public string DeleteDirectory(string directoryPath, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return $"错误：目录 {directoryPath} 不存在";

            Directory.Delete(directoryPath, recursive);
            return $"成功删除目录：{directoryPath}";
        }
        catch (Exception ex)
        {
            return $"删除目录失败：{ex.Message}";
        }
    }

    [Tool("复制文件")]
    public string CopyFile(string sourceFile, string destinationFile, bool overwrite = false)
    {
        try
        {
            if (!File.Exists(sourceFile))
                return $"错误：源文件 {sourceFile} 不存在";

            File.Copy(sourceFile, destinationFile, overwrite);
            return $"成功复制文件：{sourceFile} -> {destinationFile}";
        }
        catch (Exception ex)
        {
            return $"复制文件失败：{ex.Message}";
        }
    }

    [Tool("移动文件")]
    public string MoveFile(string sourceFile, string destinationFile)
    {
        try
        {
            if (!File.Exists(sourceFile))
                return $"错误：源文件 {sourceFile} 不存在";

            File.Move(sourceFile, destinationFile);
            return $"成功移动文件：{sourceFile} -> {destinationFile}";
        }
        catch (Exception ex)
        {
            return $"移动文件失败：{ex.Message}";
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ToolAttribute : Attribute
{
    public string Description { get; }

    public ToolAttribute(string description)
    {
        Description = description;
    }
}
