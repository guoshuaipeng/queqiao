from pathlib import Path
p = Path("src-net48/RemoteAssist.Protocol/HelperUiSettings.cs")
p.write_text("""using System;
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
""", encoding="utf-8", newline="\n")
print("HelperUiSettings OK")

# RelayPlaceholder - stop saying 即将支持
Path("src-net48/RemoteAssist.Relay.Stub/RelayPlaceholder.cs").write_text("""namespace RemoteAssist.Relay.Stub;

/// <summary>
/// 中继地址格式校验（R043：真连接由 Protocol.RelayEndpoint / RelayClient 完成）。
/// </summary>
public static class RelayPlaceholder
{
    public static bool TryParseEndpoint(string? text, out string host, out int port, out string error)
    {
        if (RemoteAssist.Protocol.RelayEndpoint.TryParse(text, out host, out port, out var err))
        {
            error = "";
            return true;
        }
        host = "";
        port = 0;
        error = string.IsNullOrWhiteSpace(err) ? "中继格式应为 IP:端口" : err!;
        // 人话：不要露出协议英文
        if (error.Contains("empty") || error.Contains("expect"))
            error = "中继格式应为 IP:端口（例如 121.196.45.81:17891）";
        return false;
    }
}
""", encoding="utf-8", newline="\n")
print("RelayPlaceholder OK")
