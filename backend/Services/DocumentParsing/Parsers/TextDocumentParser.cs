using System.Text;

namespace MAFStudio.Backend.Services.DocumentParsing
{
    /// <summary>
    /// 文本文件解析器
    /// 支持纯文本文件：.txt, .md, .log 等
    /// </summary>
    public class TextDocumentParser : BaseDocumentParser
    {
        /// <summary>
        /// 支持的扩展名
        /// </summary>
        public override IEnumerable<string> SupportedExtensions => new[] 
        { 
            "txt", "md", "markdown", "log", "rst", "org" 
        };

        /// <summary>
        /// 解析文本文件
        /// </summary>
        public override Task<string> ParseAsync(byte[] fileContent, string fileName)
        {
            // 尝试检测编码，默认UTF-8
            var encoding = DetectEncoding(fileContent) ?? Encoding.UTF8;
            
            // 跳过BOM标记
            var offset = GetBomLength(fileContent);
            var text = encoding.GetString(fileContent, offset, fileContent.Length - offset);
            
            return Task.FromResult(text);
        }

        /// <summary>
        /// 检测文件编码
        /// </summary>
        private Encoding? DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;
            if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                return Encoding.UTF32;
            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)
                return Encoding.UTF32;
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
            if (bytes.Length >= 4 && ((bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF) ||
                                       (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)))
                return 4;
            if (bytes.Length >= 2 && ((bytes[0] == 0xFE && bytes[1] == 0xFF) || (bytes[0] == 0xFF && bytes[1] == 0xFE)))
                return 2;
            
            return 0;
        }
    }
}
