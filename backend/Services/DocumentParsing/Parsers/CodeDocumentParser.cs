using System.Text;

namespace MAFStudio.Backend.Services.DocumentParsing.Parsers
{
    /// <summary>
    /// 代码文件解析器
    /// 支持各种编程语言源代码文件
    /// </summary>
    public class CodeDocumentParser : BaseDocumentParser
    {
        /// <summary>
        /// 支持的扩展名
        /// </summary>
        public override IEnumerable<string> SupportedExtensions => new[] 
        { 
            "py", "js", "ts", "jsx", "tsx", "java", "c", "cpp", "cc", "cxx", "h", "hpp", 
            "cs", "go", "rs", "rb", "php", "swift", "kt", "scala", "lua", "perl", "pl",
            "sh", "bash", "zsh", "bat", "cmd", "ps1", "psm1", "psd1",
            "sql", "html", "htm", "css", "scss", "sass", "less", "xml", "xaml", "config",
            "json", "yaml", "yml", "toml", "ini", "env", "gitignore", "dockerignore",
            "dockerfile", "makefile", "cmake", "gradle", "maven", "pom", "r", "m", "vb"
        };

        /// <summary>
        /// 解析代码文件
        /// </summary>
        public override Task<string> ParseAsync(byte[] fileContent, string fileName)
        {
            var encoding = DetectEncoding(fileContent) ?? Encoding.UTF8;
            var offset = GetBomLength(fileContent);
            var text = encoding.GetString(fileContent, offset, fileContent.Length - offset);
            
            // 添加文件名作为标识
            var ext = GetExtension(fileName);
            var language = GetLanguageIdentifier(ext);
            
            var result = new StringBuilder();
            result.AppendLine($"// File: {fileName}");
            result.AppendLine($"// Language: {language}");
            result.AppendLine();
            result.Append(text);
            
            return Task.FromResult(result.ToString());
        }

        /// <summary>
        /// 获取语言标识符
        /// </summary>
        private string GetLanguageIdentifier(string ext)
        {
            return ext.ToLower() switch
            {
                "py" => "Python",
                "js" => "JavaScript",
                "ts" => "TypeScript",
                "jsx" or "tsx" => "React",
                "java" => "Java",
                "c" => "C",
                "cpp" or "cc" or "cxx" => "C++",
                "h" or "hpp" => "C/C++ Header",
                "cs" => "C#",
                "go" => "Go",
                "rs" => "Rust",
                "rb" => "Ruby",
                "php" => "PHP",
                "swift" => "Swift",
                "kt" => "Kotlin",
                "scala" => "Scala",
                "lua" => "Lua",
                "perl" or "pl" => "Perl",
                "sh" or "bash" or "zsh" => "Shell",
                "bat" or "cmd" => "Batch",
                "ps1" => "PowerShell",
                "sql" => "SQL",
                "html" or "htm" => "HTML",
                "css" => "CSS",
                "scss" or "sass" => "Sass",
                "less" => "Less",
                "xml" or "xaml" => "XML",
                "json" => "JSON",
                "yaml" or "yml" => "YAML",
                "toml" => "TOML",
                "ini" => "INI",
                "dockerfile" => "Dockerfile",
                "makefile" => "Makefile",
                "r" => "R",
                "m" => "MATLAB/Objective-C",
                _ => ext.ToUpper()
            };
        }

        /// <summary>
        /// 检测文件编码
        /// </summary>
        private Encoding? DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;
            return null;
        }

        /// <summary>
        /// 获取BOM长度
        /// </summary>
        private int GetBomLength(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return 3;
            if (bytes.Length >= 2 && ((bytes[0] == 0xFE && bytes[1] == 0xFF) || (bytes[0] == 0xFF && bytes[1] == 0xFE)))
                return 2;
            return 0;
        }
    }
}
