using System.Text;

namespace MAFStudio.Backend.Services.DocumentParsing.Parsers
{
    /// <summary>
    /// CSV文件解析器
    /// 支持CSV和TSV文件
    /// </summary>
    public class CsvDocumentParser : BaseDocumentParser
    {
        /// <summary>
        /// 支持的扩展名
        /// </summary>
        public override IEnumerable<string> SupportedExtensions => new[] { "csv", "tsv" };

        /// <summary>
        /// 解析CSV文件
        /// </summary>
        public override Task<string> ParseAsync(byte[] fileContent, string fileName)
        {
            var encoding = DetectEncoding(fileContent) ?? Encoding.UTF8;
            var offset = GetBomLength(fileContent);
            var text = encoding.GetString(fileContent, offset, fileContent.Length - offset);
            
            var ext = GetExtension(fileName);
            var delimiter = ext == "tsv" ? '\t' : ',';
            
            var result = new StringBuilder();
            result.AppendLine($"// File: {fileName}");
            result.AppendLine($"// Format: {(ext == "tsv" ? "TSV" : "CSV")}");
            result.AppendLine();
            
            // 解析CSV并格式化输出
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            var maxColumnWidths = new List<int>();
            
            // 计算每列最大宽度
            foreach (var line in lines)
            {
                var columns = ParseCsvLine(line, delimiter);
                for (int i = 0; i < columns.Count; i++)
                {
                    if (i >= maxColumnWidths.Count)
                        maxColumnWidths.Add(columns[i].Length);
                    else if (columns[i].Length > maxColumnWidths[i])
                        maxColumnWidths[i] = columns[i].Length;
                }
            }
            
            // 格式化输出
            foreach (var line in lines)
            {
                var columns = ParseCsvLine(line, delimiter);
                var formattedLine = new StringBuilder();
                
                for (int i = 0; i < columns.Count; i++)
                {
                    var padded = columns[i].PadRight(maxColumnWidths.Count > i ? maxColumnWidths[i] : 0);
                    formattedLine.Append(padded);
                    if (i < columns.Count - 1)
                        formattedLine.Append(" | ");
                }
                
                result.AppendLine(formattedLine.ToString());
            }
            
            return Task.FromResult(result.ToString());
        }

        /// <summary>
        /// 解析CSV行
        /// </summary>
        private List<string> ParseCsvLine(string line, char delimiter)
        {
            var columns = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == delimiter)
                    {
                        columns.Add(current.ToString().Trim());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }
            
            columns.Add(current.ToString().Trim());
            return columns;
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
