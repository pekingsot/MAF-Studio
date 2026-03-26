namespace MAFStudio.Core.Utils;

/// <summary>
/// 雪花算法ID生成器
/// 生成64位长整型唯一ID，结构如下：
/// - 1位符号位（始终为0）
/// - 41位时间戳（毫秒级，可使用约69年）
/// - 10位工作机器ID（0-1023）
/// - 12位序列号（毫秒内序列，0-4095）
/// </summary>
public class SnowflakeIdGenerator
{
    private const long Twepoch = 1288834974657L;
    private const int WorkerIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const int SequenceBits = 12;

    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
    private const int WorkerIdShift = SequenceBits;
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
    private const long SequenceMask = -1L ^ (-1L << SequenceBits);

    private long _workerId;
    private long _datacenterId;
    private long _sequence = 0L;
    private long _lastTimestamp = -1L;
    private readonly object _lock = new();

    private static readonly Lazy<SnowflakeIdGenerator> _instance = new(() => new SnowflakeIdGenerator(1, 1));

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static SnowflakeIdGenerator Instance => _instance.Value;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="workerId">工作机器ID（0-31）</param>
    /// <param name="datacenterId">数据中心ID（0-31）</param>
    public SnowflakeIdGenerator(long workerId, long datacenterId)
    {
        if (workerId > MaxWorkerId || workerId < 0)
        {
            throw new ArgumentException($"工作机器ID必须在0-{MaxWorkerId}之间");
        }

        if (datacenterId > MaxDatacenterId || datacenterId < 0)
        {
            throw new ArgumentException($"数据中心ID必须在0-{MaxDatacenterId}之间");
        }

        _workerId = workerId;
        _datacenterId = datacenterId;
    }

    /// <summary>
    /// 生成下一个ID
    /// </summary>
    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = GetCurrentTimestamp();

            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException(
                    $"时钟回拨，拒绝生成ID {_lastTimestamp - timestamp}毫秒");
            }

            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0L;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Twepoch) << TimestampLeftShift) |
                   (_datacenterId << DatacenterIdShift) |
                   (_workerId << WorkerIdShift) |
                   _sequence;
        }
    }

    /// <summary>
    /// 获取当前时间戳（毫秒）
    /// </summary>
    private static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 等待下一毫秒
    /// </summary>
    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }

    /// <summary>
    /// 解析ID信息（用于调试）
    /// </summary>
    public static (long Timestamp, long DatacenterId, long WorkerId, long Sequence) ParseId(long id)
    {
        var timestamp = (id >> TimestampLeftShift) + Twepoch;
        var datacenterId = (id >> DatacenterIdShift) & MaxDatacenterId;
        var workerId = (id >> WorkerIdShift) & MaxWorkerId;
        var sequence = id & SequenceMask;

        return (timestamp, datacenterId, workerId, sequence);
    }
}
