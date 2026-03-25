using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Services.DocumentParsing.Parsers;

namespace MAFStudio.Backend.Services.DocumentParsing
{
    /// <summary>
    /// 文档解析器工厂
    /// 根据文件类型选择合适的解析器
    /// </summary>
    public class DocumentParserFactory
    {
        private readonly List<IDocumentParser> _parsers;
        private readonly TextDocumentParser _defaultParser;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DocumentParserFactory()
        {
            _defaultParser = new TextDocumentParser();
            
            _parsers = new List<IDocumentParser>
            {
                _defaultParser,
                new CodeDocumentParser(),
                new CsvDocumentParser(),
                new HtmlDocumentParser()
            };
        }

        /// <summary>
        /// 获取适合的解析器
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文档解析器</returns>
        public IDocumentParser GetParser(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return _defaultParser;

            var parser = _parsers.FirstOrDefault(p => p.Supports(extension));
            return parser ?? _defaultParser;
        }

        /// <summary>
        /// 获取所有支持的扩展名
        /// </summary>
        public IEnumerable<string> GetAllSupportedExtensions()
        {
            return _parsers.SelectMany(p => p.SupportedExtensions).Distinct().OrderBy(e => e);
        }

        /// <summary>
        /// 检查是否支持该文件类型
        /// </summary>
        public bool IsSupported(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            return _parsers.Any(p => p.Supports(extension));
        }
    }
}
