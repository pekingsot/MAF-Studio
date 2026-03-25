using System.Text;
using System.Text.RegularExpressions;

namespace MAFStudio.Backend.Services.DocumentParsing.Parsers
{
    /// <summary>
    /// HTML文件解析器
    /// 提取HTML文件中的文本内容
    /// </summary>
    public class HtmlDocumentParser : BaseDocumentParser
    {
        /// <summary>
        /// 支持的扩展名
        /// </summary>
        public override IEnumerable<string> SupportedExtensions => new[] { "html", "htm", "xhtml" };

        /// <summary>
        /// 解析HTML文件
        /// </summary>
        public override Task<string> ParseAsync(byte[] fileContent, string fileName)
        {
            var encoding = DetectEncoding(fileContent) ?? Encoding.UTF8;
            var offset = GetBomLength(fileContent);
            var html = encoding.GetString(fileContent, offset, fileContent.Length - offset);
            
            var result = new StringBuilder();
            result.AppendLine($"// File: {fileName}");
            result.AppendLine();
            
            // 提取标题
            var titleMatch = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (titleMatch.Success)
            {
                result.AppendLine($"标题: {StripTags(titleMatch.Groups[1].Value).Trim()}");
                result.AppendLine();
            }
            
            // 提取meta描述
            var descMatch = Regex.Match(html, @"<meta[^>]*name=['""]description['""][^>]*content=['""]([^'""]+)['""]", RegexOptions.IgnoreCase);
            if (descMatch.Success)
            {
                result.AppendLine($"描述: {descMatch.Groups[1].Value.Trim()}");
                result.AppendLine();
            }
            
            // 移除脚本和样式
            var text = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // 将块级元素替换为换行
            text = Regex.Replace(text, @"</(p|div|br|li|tr|td|th|h[1-6]|article|section|header|footer|nav|aside)[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            
            // 移除所有标签
            text = StripTags(text);
            
            // 解码HTML实体
            text = DecodeHtmlEntities(text);
            
            // 清理多余空白
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n\s*\n", "\n\n");
            text = text.Trim();
            
            result.Append(text);
            
            return Task.FromResult(result.ToString());
        }

        /// <summary>
        /// 移除HTML标签
        /// </summary>
        private string StripTags(string html)
        {
            return Regex.Replace(html, @"<[^>]+>", "");
        }

        /// <summary>
        /// 解码HTML实体
        /// </summary>
        private string DecodeHtmlEntities(string text)
        {
            return text
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&#39;", "'")
                .Replace("&ldquo;", "\"")
                .Replace("&rdquo;", "\"")
                .Replace("&lsquo;", "'")
                .Replace("&rsquo;", "'");
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
