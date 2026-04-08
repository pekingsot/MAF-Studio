using System.Text.Json;

namespace MAFStudio.Application.Context;

/// <summary>
/// 协作配置上下文
/// 用于在协作工作流执行时传递配置信息
/// </summary>
public class CollaborationConfigContext
{
    private static readonly AsyncLocal<CollaborationConfigContext?> _current = new();
    
    public static CollaborationConfigContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
    
    public long CollaborationId { get; set; }
    public string? ConfigJson { get; set; }
    
    private Dictionary<string, object>? _config;
    
    public Dictionary<string, object>? Config
    {
        get
        {
            if (_config == null && !string.IsNullOrEmpty(ConfigJson))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<Dictionary<string, object>>(ConfigJson);
                }
                catch
                {
                    _config = new Dictionary<string, object>();
                }
            }
            return _config;
        }
    }
    
    public T? GetConfigValue<T>(string key)
    {
        if (Config == null || !Config.TryGetValue(key, out var value))
        {
            return default;
        }
        
        try
        {
            var jsonElement = (JsonElement)value;
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
        }
        catch
        {
            return default;
        }
    }
    
    public string? GetConfigString(string key)
    {
        if (Config == null || !Config.TryGetValue(key, out var value))
        {
            return null;
        }
        
        try
        {
            var jsonElement = (JsonElement)value;
            return jsonElement.GetString();
        }
        catch
        {
            return null;
        }
    }
    
    public int? GetConfigInt(string key)
    {
        if (Config == null || !Config.TryGetValue(key, out var value))
        {
            return null;
        }
        
        try
        {
            var jsonElement = (JsonElement)value;
            return jsonElement.GetInt32();
        }
        catch
        {
            return null;
        }
    }
    
    public bool? GetConfigBool(string key)
    {
        if (Config == null || !Config.TryGetValue(key, out var value))
        {
            return null;
        }
        
        try
        {
            var jsonElement = (JsonElement)value;
            return jsonElement.GetBoolean();
        }
        catch
        {
            return null;
        }
    }
}
