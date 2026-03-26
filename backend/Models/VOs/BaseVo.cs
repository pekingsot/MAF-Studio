namespace MAFStudio.Backend.Models.VOs
{
    /// <summary>
    /// 视图对象基类
    /// VO (View Object) - 用于返回给前端的数据结构
    /// </summary>
    public abstract class BaseVo
    {
        /// <summary>
        /// 时间戳转换为 ISO 8601 字符串
        /// </summary>
        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("O");
        }

        /// <summary>
        /// 可空时间戳转换
        /// </summary>
        public static string? FormatDateTime(DateTime? dateTime)
        {
            return dateTime?.ToString("O");
        }
    }
}
