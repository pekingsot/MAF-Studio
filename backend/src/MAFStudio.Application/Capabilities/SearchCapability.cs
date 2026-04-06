using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MAFStudio.Application.Capabilities;

/// <summary>
/// 搜索能力，提供文件内容搜索和正则匹配功能
/// </summary>
public class SearchCapability : ICapability
{
    public string Name => "SearchCapability";
    public string Description => "搜索能力，支持文件内容搜索、正则匹配和文件查找";

    public IEnumerable<MethodInfo> GetTools()
    {
        return GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("在文件中搜索文本")]
    public string SearchInFiles(string pattern, string directory, string? filePattern = null, bool ignoreCase = true, int maxResults = 100)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var results = new List<SearchMatch>();
            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (var file in files)
            {
                try
                {
                    if (IsBinaryFile(Path.GetExtension(file).ToLowerInvariant())) continue;

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(pattern, comparison))
                        {
                            results.Add(new SearchMatch
                            {
                                FilePath = file,
                                LineNumber = i + 1,
                                LineContent = lines[i].Trim(),
                                MatchStart = lines[i].IndexOf(pattern, comparison),
                                MatchLength = pattern.Length
                            });

                            if (results.Count >= maxResults) break;
                        }
                    }
                    if (results.Count >= maxResults) break;
                }
                catch
                {
                    // 跳过无法读取的文件
                }
            }

            if (results.Count == 0)
                return $"未找到匹配 '{pattern}' 的内容";

            var output = new StringBuilder();
            output.AppendLine($"搜索模式: {pattern}");
            output.AppendLine($"搜索目录: {directory}");
            output.AppendLine($"匹配结果: {results.Count} 处");
            output.AppendLine(new string('=', 80));

            foreach (var group in results.GroupBy(r => r.FilePath))
            {
                output.AppendLine($"\n📄 {group.Key}");
                foreach (var match in group)
                {
                    output.AppendLine($"  行 {match.LineNumber}: {match.LineContent}");
                }
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索失败: {ex.Message}";
        }
    }

    [Tool("使用正则表达式搜索")]
    public string Grep(string pattern, string path, string? filePattern = null, int maxResults = 100)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (File.Exists(path))
            {
                return GrepSingleFile(regex, path, maxResults);
            }
            else if (Directory.Exists(path))
            {
                return GrepDirectory(regex, path, filePattern, maxResults);
            }
            else
            {
                return $"路径不存在: {path}";
            }
        }
        catch (Exception ex)
        {
            return $"正则搜索失败: {ex.Message}";
        }
    }

    [Tool("查找文件")]
    public string FindFiles(string pattern, string directory, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directory, pattern, searchOption);

            if (files.Length == 0)
                return $"未找到匹配 '{pattern}' 的文件";

            var output = new StringBuilder();
            output.AppendLine($"搜索模式: {pattern}");
            output.AppendLine($"搜索目录: {directory}");
            output.AppendLine($"找到文件: {files.Length} 个");
            output.AppendLine(new string('-', 60));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                output.AppendLine($"  {file}");
                output.AppendLine($"    大小: {FormatFileSize(fileInfo.Length)}, 修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"查找文件失败: {ex.Message}";
        }
    }

    [Tool("查找目录")]
    public string FindDirectories(string pattern, string directory, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dirs = Directory.GetDirectories(directory, pattern, searchOption);

            if (dirs.Length == 0)
                return $"未找到匹配 '{pattern}' 的目录";

            var output = new StringBuilder();
            output.AppendLine($"搜索模式: {pattern}");
            output.AppendLine($"搜索目录: {directory}");
            output.AppendLine($"找到目录: {dirs.Length} 个");
            output.AppendLine(new string('-', 60));

            foreach (var dir in dirs)
            {
                var dirInfo = new DirectoryInfo(dir);
                var fileCount = dirInfo.GetFiles().Length;
                var subDirCount = dirInfo.GetDirectories().Length;
                output.AppendLine($"  {dir}");
                output.AppendLine($"    文件数: {fileCount}, 子目录数: {subDirCount}, 修改时间: {dirInfo.LastWriteTime:yyyy-MM-dd HH:mm}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"查找目录失败: {ex.Message}";
        }
    }

    [Tool("搜索并替换文本")]
    public string SearchAndReplace(string searchPattern, string replaceWith, string directory, string? filePattern = null, bool ignoreCase = true, int maxReplacements = 1000)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var totalReplacements = 0;
            var modifiedFiles = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    if (IsBinaryFile(Path.GetExtension(file).ToLowerInvariant())) continue;

                    var content = File.ReadAllText(file);
                    var newContent = new StringBuilder();
                    var lastIndex = 0;
                    var fileReplacements = 0;

                    int index;
                    while ((index = content.IndexOf(searchPattern, lastIndex, comparison)) != -1 && totalReplacements < maxReplacements)
                    {
                        newContent.Append(content.Substring(lastIndex, index - lastIndex));
                        newContent.Append(replaceWith);
                        lastIndex = index + searchPattern.Length;
                        fileReplacements++;
                        totalReplacements++;
                    }

                    if (fileReplacements > 0)
                    {
                        newContent.Append(content.Substring(lastIndex));
                        File.WriteAllText(file, newContent.ToString());
                        modifiedFiles.Add($"{file} ({fileReplacements} 处替换)");
                    }
                }
                catch
                {
                    // 跳过无法处理的文件
                }
            }

            if (totalReplacements == 0)
                return $"未找到匹配 '{searchPattern}' 的内容";

            var output = new StringBuilder();
            output.AppendLine($"搜索模式: {searchPattern}");
            output.AppendLine($"替换为: {replaceWith}");
            output.AppendLine($"总替换数: {totalReplacements}");
            output.AppendLine($"修改文件数: {modifiedFiles.Count}");
            output.AppendLine(new string('-', 60));

            foreach (var file in modifiedFiles)
            {
                output.AppendLine($"  ✏️ {file}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索替换失败: {ex.Message}";
        }
    }

    [Tool("搜索最近修改的文件")]
    public string FindRecentlyModified(string directory, int days = 7, string? filePattern = null)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var cutoffDate = DateTime.Now.AddDays(-days);
            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            var recentFiles = files
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTime >= cutoffDate && !IsBinaryFile(f.Extension.ToLowerInvariant()))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(50)
                .ToList();

            if (recentFiles.Count == 0)
                return $"过去 {days} 天内没有修改过的文件";

            var output = new StringBuilder();
            output.AppendLine($"搜索目录: {directory}");
            output.AppendLine($"时间范围: 过去 {days} 天");
            output.AppendLine($"找到文件: {recentFiles.Count} 个");
            output.AppendLine(new string('-', 80));
            output.AppendLine($"{"修改时间",-20} {"大小",-12} {"文件路径"}");
            output.AppendLine(new string('-', 80));

            foreach (var file in recentFiles)
            {
                output.AppendLine($"{file.LastWriteTime:yyyy-MM-dd HH:mm:ss,-20} {FormatFileSize(file.Length),-12} {file.FullName}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索失败: {ex.Message}";
        }
    }

    [Tool("搜索大文件")]
    public string FindLargeFiles(string directory, long minSizeMB = 10, string? filePattern = null)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var minSizeBytes = minSizeMB * 1024 * 1024;
            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            var largeFiles = files
                .Select(f => new FileInfo(f))
                .Where(f => f.Length >= minSizeBytes)
                .OrderByDescending(f => f.Length)
                .Take(50)
                .ToList();

            if (largeFiles.Count == 0)
                return $"没有大于 {minSizeMB}MB 的文件";

            var output = new StringBuilder();
            output.AppendLine($"搜索目录: {directory}");
            output.AppendLine($"最小大小: {minSizeMB}MB");
            output.AppendLine($"找到文件: {largeFiles.Count} 个");
            output.AppendLine(new string('-', 80));
            output.AppendLine($"{"大小",-15} {"修改时间",-20} {"文件路径"}");
            output.AppendLine(new string('-', 80));

            foreach (var file in largeFiles)
            {
                output.AppendLine($"{FormatFileSize(file.Length),-15} {file.LastWriteTime:yyyy-MM-dd HH:mm:ss,-20} {file.FullName}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索失败: {ex.Message}";
        }
    }

    [Tool("搜索重复文件")]
    public string FindDuplicateFiles(string directory, string? filePattern = null)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            var filesBySize = files
                .Select(f => new FileInfo(f))
                .Where(f => !IsBinaryFile(f.Extension.ToLowerInvariant()) || f.Length < 10 * 1024 * 1024)
                .GroupBy(f => f.Length)
                .Where(g => g.Count() > 1)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            if (filesBySize.Count == 0)
                return "没有找到重复文件";

            var output = new StringBuilder();
            output.AppendLine($"搜索目录: {directory}");
            output.AppendLine($"可能重复的文件组: {filesBySize.Count} 组");
            output.AppendLine(new string('-', 80));

            foreach (var group in filesBySize.OrderByDescending(g => g.Key).Take(20))
            {
                output.AppendLine($"\n📦 文件大小: {FormatFileSize(group.Key)} ({group.Value.Count} 个文件)");
                foreach (var file in group.Value)
                {
                    output.AppendLine($"  - {file.FullName}");
                }
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索失败: {ex.Message}";
        }
    }

    private static string GrepSingleFile(Regex regex, string filePath, int maxResults)
    {
        var content = File.ReadAllText(filePath);
        var lines = content.Split('\n');
        var results = new List<GrepMatch>();

        for (int i = 0; i < lines.Length && results.Count < maxResults; i++)
        {
            var matches = regex.Matches(lines[i]);
            foreach (Match match in matches)
            {
                results.Add(new GrepMatch
                {
                    LineNumber = i + 1,
                    LineContent = lines[i].TrimEnd(),
                    MatchStart = match.Index,
                    MatchLength = match.Length
                });
            }
        }

        if (results.Count == 0)
            return $"文件 {filePath} 中未找到匹配";

        var output = new StringBuilder();
        output.AppendLine($"文件: {filePath}");
        output.AppendLine($"匹配数: {results.Count}");
        output.AppendLine(new string('-', 60));

        foreach (var match in results)
        {
            output.AppendLine($"行 {match.LineNumber}: {match.LineContent}");
        }

        return output.ToString();
    }

    private static string GrepDirectory(Regex regex, string directory, string? filePattern, int maxResults)
    {
        var results = new List<(string File, int Line, string Content)>();
        var searchOption = SearchOption.AllDirectories;
        var files = string.IsNullOrEmpty(filePattern)
            ? Directory.GetFiles(directory, "*", searchOption)
            : Directory.GetFiles(directory, filePattern, searchOption);

        foreach (var file in files)
        {
            try
            {
                if (IsBinaryFile(Path.GetExtension(file).ToLowerInvariant())) continue;

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length && results.Count < maxResults; i++)
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        results.Add((file, i + 1, lines[i].TrimEnd()));
                    }
                }
                if (results.Count >= maxResults) break;
            }
            catch
            {
                // 跳过无法读取的文件
            }
        }

        if (results.Count == 0)
            return $"目录 {directory} 中未找到匹配";

        var output = new StringBuilder();
        output.AppendLine($"搜索目录: {directory}");
        output.AppendLine($"匹配数: {results.Count}");
        output.AppendLine(new string('=', 80));

        foreach (var group in results.GroupBy(r => r.File))
        {
            output.AppendLine($"\n📄 {group.Key}");
            foreach (var (_, line, content) in group)
            {
                output.AppendLine($"  行 {line}: {content}");
            }
        }

        return output.ToString();
    }

    private static bool IsBinaryFile(string extension)
    {
        var binaryExtensions = new HashSet<string>
        {
            ".exe", ".dll", ".so", ".dylib", ".bin",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico",
            ".mp3", ".mp4", ".wav", ".avi", ".mov",
            ".zip", ".rar", ".7z", ".tar", ".gz",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx"
        };
        return binaryExtensions.Contains(extension);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private class SearchMatch
    {
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public string LineContent { get; set; } = "";
        public int MatchStart { get; set; }
        public int MatchLength { get; set; }
    }

    private class GrepMatch
    {
        public int LineNumber { get; set; }
        public string LineContent { get; set; } = "";
        public int MatchStart { get; set; }
        public int MatchLength { get; set; }
    }
}
