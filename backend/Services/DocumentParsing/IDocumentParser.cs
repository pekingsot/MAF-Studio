namespace MAFStudio.Backend.Services.DocumentParsing
{
    /// <summary>
    /// 文档解析策略接口
    /// 使用策略模式支持不同文件类型的解析
    /// </summary>
    public interface IDocumentParser
    {
        /// <summary>
        /// 支持的文件扩展名列表
        /// </summary>
        IEnumerable<string> SupportedExtensions { get; }

        /// <summary>
        /// 解析文档内容
        /// </summary>
        /// <param name="fileContent">文件二进制内容</param>
        /// <param name="fileName">文件名</param>
        /// <returns>解析后的文本内容</returns>
        Task<string> ParseAsync(byte[] fileContent, string fileName);

        /// <summary>
        /// 是否支持该文件类型
        /// </summary>
        bool Supports(string extension);
    }
}
