using System.ComponentModel;
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

    [Tool("Read the entire content of a text file. Returns the file content as a string.")]
    public string ReadFile(
        [Description("Absolute path to the file to read, e.g. '/home/user/project/src/Program.cs'")] string filePath)
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

    [Tool("Write content to a file, creating the file and any parent directories if they do not exist. This will overwrite the existing file content.")]
    public string WriteFile(
        [Description("Absolute path to the file to write, e.g. '/home/user/project/src/newfile.cs'")] string filePath,
        [Description("The text content to write into the file")] string content)
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

    [Tool("Append content to the end of an existing file.")]
    public string AppendFile(
        [Description("Absolute path to the file to append content to")] string filePath,
        [Description("The text content to append to the end of the file")] string content)
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

    [Tool("Delete a file.")]
    public string DeleteFile(
        [Description("Absolute path to the file to delete")] string filePath)
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

    [Tool("List files in a directory.")]
    public string ListFiles(
        [Description("Absolute path to the directory to list files from")] string directoryPath,
        [Description("Glob pattern to filter files, e.g. '*.cs' for C# files. Default '*' shows all files")] string searchPattern = "*")
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

    [Tool("Create a directory and all parent directories if needed.")]
    public string CreateDirectory(
        [Description("Absolute path of the directory to create, e.g. '/home/user/project/newdir'")] string directoryPath)
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

    [Tool("Delete a directory.")]
    public string DeleteDirectory(
        [Description("Absolute path of the directory to delete")] string directoryPath,
        [Description("Whether to delete subdirectories and files recursively. Default true")] bool recursive = true)
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

    [Tool("Copy a file to a new location.")]
    public string CopyFile(
        [Description("Absolute path of the source file to copy")] string sourceFile,
        [Description("Absolute path for the destination copy")] string destinationFile,
        [Description("Whether to overwrite an existing file at the destination. Default false")] bool overwrite = false)
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

    [Tool("Move a file to a new location.")]
    public string MoveFile(
        [Description("Absolute path of the source file to move")] string sourceFile,
        [Description("Absolute path for the destination")] string destinationFile)
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
