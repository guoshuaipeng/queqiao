using System.Text.Json;

namespace RemoteAssist.Protocol;

/// <summary>
/// 协助端 GUI 本地设置。中继字段为产品化占位，v1 不启用连接。
/// </summary>
public sealed class HelperUiSettings
{
    public string? RelayEndpoint { get; set; }

    /// <summary>最近一次成功连接的主机（方便回填）。</summary>
    public string? LastHost { get; set; }

    public int? LastPort { get; set; }

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RemoteAssist");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "helper-ui.json");

    public static HelperUiSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new HelperUiSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<HelperUiSettings>(json) ?? new HelperUiSettings();
        }
        catch
        {
            return new HelperUiSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
