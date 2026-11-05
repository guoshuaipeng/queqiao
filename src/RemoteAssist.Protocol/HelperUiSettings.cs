using System.Text.Json;

namespace RemoteAssist.Protocol;

/// <summary>协助端 / 统一 App 本地设置（中继、账号令牌等）。</summary>
public sealed class HelperUiSettings
{
    public string? RelayEndpoint { get; set; } = Protocol.RelayEndpoint.DefaultEndpoint;

    public string? LastHost { get; set; }
    public int? LastPort { get; set; }

    public string? AccountUser { get; set; }
    public string? AccountToken { get; set; }

    /// <summary>本机稳定设备 ID（被控端绑定用）。</summary>
    public string? DeviceId { get; set; }

    public string? DeviceDisplayName { get; set; }

    /// <summary>false 时优先局域网直连（高级/调试）。默认 true = 公网中继。</summary>
    public bool PreferRelay { get; set; } = true;

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
            var s = JsonSerializer.Deserialize<HelperUiSettings>(json) ?? new HelperUiSettings();
            if (string.IsNullOrWhiteSpace(s.RelayEndpoint))
                s.RelayEndpoint = Protocol.RelayEndpoint.DefaultEndpoint;
            if (string.IsNullOrWhiteSpace(s.DeviceId))
                s.DeviceId = EnsureDeviceId();
            return s;
        }
        catch
        {
            return new HelperUiSettings { DeviceId = EnsureDeviceId() };
        }
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
            DeviceId = EnsureDeviceId();
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static string EnsureDeviceId()
    {
        try
        {
            var path = Path.Combine(SettingsDirectory, "device-id.txt");
            if (File.Exists(path))
            {
                var id = File.ReadAllText(path).Trim();
                if (id.Length > 0) return id;
            }
            Directory.CreateDirectory(SettingsDirectory);
            var fresh = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, fresh);
            return fresh;
        }
        catch
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
