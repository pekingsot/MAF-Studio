using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services.DocumentParsing
{
    /// <summary>
    /// 文档解析器抽象基类
    /// 提供通用的辅助方法
    /// </summary>
    public abstract class BaseDocumentParser : IDocumentParser
    {
        /// <summary>
        /// 支持的文件扩展名列表
        /// </summary>
        public abstract IEnumerable<string> SupportedExtensions { get; }

        /// <summary>
        /// 解析文档内容
        /// </summary>
        public abstract Task<string> ParseAsync(byte[] fileContent, string fileName);

        /// <summary>
        /// 是否支持该文件类型
        /// </summary>
        public bool Supports(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            
            var ext = extension.ToLower().TrimStart('.');
            return SupportedExtensions.Any(s => s.ToLower().TrimStart('.') == ext);
        }

        /// <summary>
        /// 获取文件扩展名（不带点）
        /// </summary>
        protected string GetExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;
            
            var dotIndex = fileName.LastIndexOf('.');
            return dotIndex >= 0 ? fileName.Substring(dotIndex + 1).ToLower() : string.Empty;
        }
    }
}
