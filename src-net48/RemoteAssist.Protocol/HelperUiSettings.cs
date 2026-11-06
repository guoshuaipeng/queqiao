using System;
using System.IO;
using System.Text.Json;

namespace RemoteAssist.Protocol;

/// <summary>
/// 协助端 GUI 本地设置。默认公网中继（R043）。
/// </summary>
public sealed class HelperUiSettings
{
    public string? RelayEndpoint { get; set; } = Protocol.RelayEndpoint.DefaultEndpoint;

    public bool PreferRelay { get; set; } = true;

    /// <summary>最近一次成功连接的主机（局域网高级回填）。</summary>
    public string? LastHost { get; set; }

    public int? LastPort { get; set; }

    public string? LastFingerprint { get; set; }

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RemoteAssist");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "helper-ui-net48.json");

    public static HelperUiSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new HelperUiSettings();
            var json = File.ReadAllText(SettingsPath);
            var s = JsonSerializer.Deserialize<HelperUiSettings>(json) ?? new HelperUiSettings();
            if (string.IsNullOrWhiteSpace(s.RelayEndpoint))
                s.RelayEndpoint = Protocol.RelayEndpoint.DefaultEndpoint;
            return s;
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
