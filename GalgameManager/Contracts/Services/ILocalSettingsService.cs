using Newtonsoft.Json;

namespace GalgameManager.Contracts.Services;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key, bool isLarge = false, List<JsonConverter>? converters = null,
        bool typeNameHandling = false);

    /// <summary>
    /// 用于直接读取旧设置（直接加载json文件而非从内存读），如果文件不存在则返回默认值
    /// </summary>
    /// <param name="key">key</param>
    /// <param name="template">模板</param>
    /// <param name="settings"></param>
    Task<T?> ReadOldSettingAsync<T>(string key, T template, JsonSerializerSettings? settings = null);

    Task SaveSettingAsync<T>(string key, T value, bool isLarge = false, bool triggerEventWhenNull = false,
        List<JsonConverter>? converters = null, bool typeNameHandling = false);

    Task RemoveSettingAsync(string key, bool isLarge = false);
    
    public delegate void Delegate(string key, object? value);
    
    /// <summary>
    /// 当设置值改变时触发，<b>从UI线程调用</b>
    /// </summary>
    public event Delegate? OnSettingChanged;
}
