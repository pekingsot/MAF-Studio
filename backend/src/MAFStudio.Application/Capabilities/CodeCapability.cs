using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MAFStudio.Application.Capabilities;

/// <summary>
/// 代码操作能力，提供代码分析、格式化和搜索功能
/// </summary>
public class CodeCapability : ICapability
{
    public string Name => "CodeCapability";
    public string Description => "代码操作能力，支持代码分析、格式化、搜索和指标统计";

    public IEnumerable<MethodInfo> GetTools()
    {
        return GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Analyze the structure of a source code file. Reports classes, methods, properties and other definitions.")]
    public string AnalyzeCode(
        [Description("Absolute path to the source code file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"文件不存在: {filePath}";

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var language = GetLanguageFromExtension(extension);
            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');

            var result = new StringBuilder();
            result.AppendLine($"文件: {filePath}");
            result.AppendLine($"语言: {language}");
            result.AppendLine($"总行数: {lines.Length}");
            result.AppendLine($"总字符数: {content.Length}");
            result.AppendLine();

            switch (language)
            {
                case "C#":
                    AnalyzeCSharpCode(content, result);
                    break;
                case "TypeScript":
                case "JavaScript":
                    AnalyzeJavaScriptCode(content, result);
                    break;
                case "Python":
                    AnalyzePythonCode(content, result);
                    break;
                default:
                    result.AppendLine("暂不支持该语言的详细分析");
                    break;
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"分析代码失败: {ex.Message}";
        }
    }

    [Tool("Search for a regex pattern in source code files within a directory.")]
    public string SearchInCode(
        [Description("Regex pattern to search for, e.g. 'class\\s+\\w+'")] string pattern,
        [Description("Absolute path to the directory to search in")] string directory,
        [Description("Glob pattern to filter files, e.g. '*.cs'. Optional")] string? filePattern = null,
        [Description("Case-insensitive search. Default true")] bool ignoreCase = true)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var results = new List<SearchResult>();
            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            var regex = new Regex(pattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            foreach (var file in files)
            {
                try
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (IsBinaryFile(extension)) continue;

                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var matches = regex.Matches(lines[i]);
                        foreach (Match match in matches)
                        {
                            results.Add(new SearchResult
                            {
                                FilePath = file,
                                LineNumber = i + 1,
                                LineContent = lines[i].Trim(),
                                MatchedText = match.Value
                            });
                        }
                    }
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
            output.AppendLine(new string('-', 60));

            foreach (var group in results.GroupBy(r => r.FilePath).Take(20))
            {
                output.AppendLine($"\n文件: {group.Key}");
                foreach (var result in group.Take(10))
                {
                    output.AppendLine($"  行 {result.LineNumber}: {result.LineContent}");
                }
                if (group.Count() > 10)
                    output.AppendLine($"  ... 还有 {group.Count() - 10} 处匹配");
            }

            if (results.Count > 200)
                output.AppendLine($"\n... 结果已截断，共 {results.Count} 处匹配");

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"搜索失败: {ex.Message}";
        }
    }

    [Tool("Get code statistics for a directory including line counts by language.")]
    public string GetCodeMetrics(
        [Description("Absolute path to the code directory")] string directory,
        [Description("Glob pattern to filter files, e.g. '*.cs'. Optional")] string? filePattern = null)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var metrics = new Dictionary<string, LanguageMetrics>();
            var searchOption = SearchOption.AllDirectories;
            var files = string.IsNullOrEmpty(filePattern)
                ? Directory.GetFiles(directory, "*", searchOption)
                : Directory.GetFiles(directory, filePattern, searchOption);

            foreach (var file in files)
            {
                try
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (IsBinaryFile(extension)) continue;

                    var language = GetLanguageFromExtension(extension);
                    if (string.IsNullOrEmpty(language)) continue;

                    var content = File.ReadAllText(file);
                    var lines = content.Split('\n');

                    if (!metrics.ContainsKey(language))
                        metrics[language] = new LanguageMetrics { Language = language };

                    metrics[language].FileCount++;
                    metrics[language].TotalLines += lines.Length;
                    metrics[language].TotalChars += content.Length;
                    metrics[language].CodeLines += lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//") && !l.TrimStart().StartsWith("#") && !l.TrimStart().StartsWith("/*"));
                    metrics[language].CommentLines += lines.Count(l => l.TrimStart().StartsWith("//") || l.TrimStart().StartsWith("#") || l.TrimStart().StartsWith("/*") || l.TrimStart().StartsWith("*"));
                    metrics[language].BlankLines += lines.Count(string.IsNullOrWhiteSpace);
                }
                catch
                {
                    // 跳过无法读取的文件
                }
            }

            if (metrics.Count == 0)
                return "未找到代码文件";

            var output = new StringBuilder();
            output.AppendLine("代码统计信息");
            output.AppendLine(new string('=', 80));
            output.AppendLine($"{"语言",-15} {"文件数",10} {"总行数",10} {"代码行",10} {"注释行",10} {"空白行",10}");
            output.AppendLine(new string('-', 80));

            foreach (var m in metrics.Values.OrderByDescending(m => m.TotalLines))
            {
                output.AppendLine($"{m.Language,-15} {m.FileCount,10} {m.TotalLines,10} {m.CodeLines,10} {m.CommentLines,10} {m.BlankLines,10}");
            }

            output.AppendLine(new string('-', 80));
            output.AppendLine($"{"合计",-15} {metrics.Values.Sum(m => m.FileCount),10} {metrics.Values.Sum(m => m.TotalLines),10} {metrics.Values.Sum(m => m.CodeLines),10} {metrics.Values.Sum(m => m.CommentLines),10} {metrics.Values.Sum(m => m.BlankLines),10}");

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"获取统计信息失败: {ex.Message}";
        }
    }

    [Tool("Find class, interface or method definitions in source code.")]
    public string FindDefinitions(
        [Description("Absolute path to the code directory")] string directory,
        [Description("Filter by type/class name. Optional")] string? typeName = null,
        [Description("Filter by method name. Optional")] string? methodName = null)
    {
        try
        {
            if (!Directory.Exists(directory))
                return $"目录不存在: {directory}";

            var results = new List<DefinitionResult>();
            var codeFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Where(f => !IsBinaryFile(Path.GetExtension(f).ToLowerInvariant()));

            foreach (var file in codeFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var lines = content.Split('\n');

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];

                        // 查找类/接口定义
                        if (string.IsNullOrEmpty(methodName))
                        {
                            var classMatch = Regex.Match(line, @"\b(class|interface|struct|record|enum)\s+(\w+)");
                            if (classMatch.Success)
                            {
                                if (string.IsNullOrEmpty(typeName) || classMatch.Groups[2].Value.Contains(typeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    results.Add(new DefinitionResult
                                    {
                                        FilePath = file,
                                        LineNumber = i + 1,
                                        DefinitionType = classMatch.Groups[1].Value,
                                        Name = classMatch.Groups[2].Value,
                                        LineContent = line.Trim()
                                    });
                                }
                            }
                        }

                        // 查找方法定义
                        if (string.IsNullOrEmpty(typeName) || !string.IsNullOrEmpty(methodName))
                        {
                            var methodMatch = Regex.Match(line, @"\b(public|private|protected|internal|static|async|virtual|override)\s+[\w<>\[\],\s]+\s+(\w+)\s*\(");
                            if (methodMatch.Success)
                            {
                                if (string.IsNullOrEmpty(methodName) || methodMatch.Groups[2].Value.Contains(methodName, StringComparison.OrdinalIgnoreCase))
                                {
                                    results.Add(new DefinitionResult
                                    {
                                        FilePath = file,
                                        LineNumber = i + 1,
                                        DefinitionType = "method",
                                        Name = methodMatch.Groups[2].Value,
                                        LineContent = line.Trim()
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // 跳过无法读取的文件
                }
            }

            if (results.Count == 0)
                return "未找到匹配的定义";

            var output = new StringBuilder();
            output.AppendLine($"找到 {results.Count} 个定义:");
            output.AppendLine(new string('-', 60));

            foreach (var group in results.GroupBy(r => r.FilePath))
            {
                output.AppendLine($"\n文件: {group.Key}");
                foreach (var result in group)
                {
                    output.AppendLine($"  行 {result.LineNumber} [{result.DefinitionType}]: {result.Name}");
                    output.AppendLine($"    {result.LineContent}");
                }
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"查找定义失败: {ex.Message}";
        }
    }

    [Tool("Extract comments from a source code file.")]
    public string ExtractComments(
        [Description("Absolute path to the source code file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"文件不存在: {filePath}";

            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');
            var comments = new List<CommentInfo>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart();

                // 单行注释
                if (line.StartsWith("//"))
                {
                    comments.Add(new CommentInfo
                    {
                        LineNumber = i + 1,
                        CommentType = "单行",
                        Content = line.Substring(2).Trim()
                    });
                }
                // Python/Shell 注释
                else if (line.StartsWith("#"))
                {
                    comments.Add(new CommentInfo
                    {
                        LineNumber = i + 1,
                        CommentType = "单行",
                        Content = line.Substring(1).Trim()
                    });
                }
            }

            // 多行注释 (/* */)
            var multiLineRegex = new Regex(@"/\*(.*?)\*/", RegexOptions.Singleline);
            var multiLineMatches = multiLineRegex.Matches(content);
            foreach (Match match in multiLineMatches)
            {
                var startPos = GetPosition(content, match.Index);
                comments.Add(new CommentInfo
                {
                    LineNumber = startPos.Line,
                    CommentType = "多行",
                    Content = match.Value.TrimStart('/', '*').TrimEnd('*', '/').Trim()
                });
            }

            // XML 文档注释 (///)
            var xmlDocRegex = new Regex(@"///.*", RegexOptions.Multiline);
            var xmlDocMatches = xmlDocRegex.Matches(content);
            foreach (Match match in xmlDocMatches)
            {
                var pos = GetPosition(content, match.Index);
                comments.Add(new CommentInfo
                {
                    LineNumber = pos.Line,
                    CommentType = "XML文档",
                    Content = match.Value.TrimStart('/').Trim()
                });
            }

            if (comments.Count == 0)
                return "未找到注释";

            var output = new StringBuilder();
            output.AppendLine($"文件: {filePath}");
            output.AppendLine($"注释总数: {comments.Count}");
            output.AppendLine(new string('-', 60));

            foreach (var comment in comments.OrderBy(c => c.LineNumber))
            {
                output.AppendLine($"行 {comment.LineNumber} [{comment.CommentType}]: {comment.Content}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"提取注释失败: {ex.Message}";
        }
    }

    [Tool("Analyze the import/using dependencies of a source code file.")]
    public string AnalyzeDependencies(
        [Description("Absolute path to the source code file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"文件不存在: {filePath}";

            var content = File.ReadAllText(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var dependencies = new List<string>();

            switch (extension)
            {
                case ".cs":
                    var usingMatches = Regex.Matches(content, @"using\s+([\w\.]+);");
                    foreach (Match match in usingMatches)
                    {
                        dependencies.Add(match.Groups[1].Value);
                    }
                    break;

                case ".ts":
                case ".tsx":
                case ".js":
                case ".jsx":
                    var importMatches = Regex.Matches(content, @"import\s+.*from\s+['""]([^'""]+)['""]");
                    foreach (Match match in importMatches)
                    {
                        dependencies.Add(match.Groups[1].Value);
                    }
                    var requireMatches = Regex.Matches(content, @"require\(['""]([^'""]+)['""]\)");
                    foreach (Match match in requireMatches)
                    {
                        dependencies.Add(match.Groups[1].Value);
                    }
                    break;

                case ".py":
                    var pyMatches = Regex.Matches(content, @"^(?:import|from)\s+(\w+)", RegexOptions.Multiline);
                    foreach (Match match in pyMatches)
                    {
                        dependencies.Add(match.Groups[1].Value);
                    }
                    break;

                default:
                    return $"暂不支持分析 {extension} 文件的依赖";
            }

            if (dependencies.Count == 0)
                return "未找到依赖";

            var output = new StringBuilder();
            output.AppendLine($"文件: {filePath}");
            output.AppendLine($"依赖数量: {dependencies.Count}");
            output.AppendLine(new string('-', 40));

            foreach (var dep in dependencies.Distinct().OrderBy(d => d))
            {
                output.AppendLine($"  - {dep}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"分析依赖失败: {ex.Message}";
        }
    }

    private static string GetLanguageFromExtension(string extension)
    {
        return extension switch
        {
            ".cs" => "C#",
            ".ts" or ".tsx" => "TypeScript",
            ".js" or ".jsx" => "JavaScript",
            ".py" => "Python",
            ".java" => "Java",
            ".go" => "Go",
            ".rs" => "Rust",
            ".cpp" or ".cc" or ".cxx" => "C++",
            ".c" => "C",
            ".h" or ".hpp" => "C/C++ Header",
            ".vue" => "Vue",
            ".rb" => "Ruby",
            ".php" => "PHP",
            ".swift" => "Swift",
            ".kt" => "Kotlin",
            ".scala" => "Scala",
            ".sql" => "SQL",
            ".sh" => "Shell",
            ".ps1" => "PowerShell",
            ".json" => "JSON",
            ".xml" => "XML",
            ".yaml" or ".yml" => "YAML",
            ".md" => "Markdown",
            ".html" or ".htm" => "HTML",
            ".css" => "CSS",
            ".scss" or ".sass" => "SCSS/SASS",
            ".less" => "LESS",
            _ => ""
        };
    }

    private static bool IsBinaryFile(string extension)
    {
        var binaryExtensions = new HashSet<string>
        {
            ".exe", ".dll", ".so", ".dylib", ".bin",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg",
            ".mp3", ".mp4", ".wav", ".avi", ".mov", ".mkv",
            ".zip", ".rar", ".7z", ".tar", ".gz",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".class", ".jar", ".war",
            ".node_modules"
        };
        return binaryExtensions.Contains(extension);
    }

    private static void AnalyzeCSharpCode(string content, StringBuilder result)
    {
        var classCount = Regex.Matches(content, @"\bclass\s+\w+").Count;
        var interfaceCount = Regex.Matches(content, @"\binterface\s+\w+").Count;
        var methodCount = Regex.Matches(content, @"\b(public|private|protected|internal)\s+[\w<>\[\],\s]+\s+\w+\s*\(").Count;
        var propertyCount = Regex.Matches(content, @"\b(public|private|protected|internal)\s+[\w<>\[\],\s]+\s+\w+\s*\{").Count;
        var usingCount = Regex.Matches(content, @"^using\s+", RegexOptions.Multiline).Count;

        result.AppendLine($"using 语句数: {usingCount}");
        result.AppendLine($"类数量: {classCount}");
        result.AppendLine($"接口数量: {interfaceCount}");
        result.AppendLine($"方法数量: {methodCount}");
        result.AppendLine($"属性数量: {propertyCount}");
    }

    private static void AnalyzeJavaScriptCode(string content, StringBuilder result)
    {
        var functionCount = Regex.Matches(content, @"\bfunction\s+\w+").Count;
        var arrowFunctionCount = Regex.Matches(content, @"=>").Count;
        var classCount = Regex.Matches(content, @"\bclass\s+\w+").Count;
        var importCount = Regex.Matches(content, @"^import\s+", RegexOptions.Multiline).Count;
        var exportCount = Regex.Matches(content, @"^export\s+", RegexOptions.Multiline).Count;

        result.AppendLine($"import 语句数: {importCount}");
        result.AppendLine($"export 语句数: {exportCount}");
        result.AppendLine($"类数量: {classCount}");
        result.AppendLine($"函数数量: {functionCount}");
        result.AppendLine($"箭头函数数: {arrowFunctionCount}");
    }

    private static void AnalyzePythonCode(string content, StringBuilder result)
    {
        var classCount = Regex.Matches(content, @"^class\s+\w+", RegexOptions.Multiline).Count;
        var functionCount = Regex.Matches(content, @"^def\s+\w+", RegexOptions.Multiline).Count;
        var importCount = Regex.Matches(content, @"^(import|from)\s+", RegexOptions.Multiline).Count;

        result.AppendLine($"import 语句数: {importCount}");
        result.AppendLine($"类数量: {classCount}");
        result.AppendLine($"函数数量: {functionCount}");
    }

    private static (int Line, int Column) GetPosition(string content, int index)
    {
        var line = 1;
        var column = 1;
        for (int i = 0; i < index && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }
        return (line, column);
    }

    private class SearchResult
    {
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public string LineContent { get; set; } = "";
        public string MatchedText { get; set; } = "";
    }

    private class LanguageMetrics
    {
        public string Language { get; set; } = "";
        public int FileCount { get; set; }
        public int TotalLines { get; set; }
        public int TotalChars { get; set; }
        public int CodeLines { get; set; }
        public int CommentLines { get; set; }
        public int BlankLines { get; set; }
    }

    private class DefinitionResult
    {
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public string DefinitionType { get; set; } = "";
        public string Name { get; set; } = "";
        public string LineContent { get; set; } = "";
    }

    private class CommentInfo
    {
        public int LineNumber { get; set; }
        public string CommentType { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
