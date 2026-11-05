namespace RemoteAssist.Protocol;

/// <summary>
/// 枚举并筛选本机可用于局域网配对的 IPv4；避开环回、APIPA、常见虚拟/代理网段。
/// </summary>
public static class LanAddressSelector
{
    /// <summary>是否应排除（不写入默认 QR / 不作为首选）。</summary>
    public static bool IsExcluded(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return true;
        if (!System.Net.IPAddress.TryParse(ip.Trim(), out var addr))
            return true;
        return IsExcluded(addr);
    }

    public static bool IsExcluded(System.Net.IPAddress addr)
    {
        if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return true;

        var bytes = addr.GetAddressBytes();
        // 127.0.0.0/8
        if (bytes[0] == 127)
            return true;
        // 169.254.0.0/16 APIPA
        if (bytes[0] == 169 && bytes[1] == 254)
            return true;
        // 198.18.0.0/15 — RFC 2544 / 常见 Clash·VPN 虚拟网卡
        if (bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19))
            return true;
        // 0.0.0.0
        if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
            return true;

        return false;
    }

    /// <summary>越小越优先。私网优先：192.168 &gt; 10 &gt; 172.16-31 &gt; 其它公网/未知。</summary>
    public static int Rank(string ip)
    {
        if (!System.Net.IPAddress.TryParse(ip.Trim(), out var addr))
            return 1000;
        return Rank(addr);
    }

    public static int Rank(System.Net.IPAddress addr)
    {
        var b = addr.GetAddressBytes();
        if (b[0] == 192 && b[1] == 168)
            return 0;
        if (b[0] == 10)
            return 1;
        if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            return 2;
        return 50;
    }

    /// <summary>从候选中过滤、去重、排序；空则回退 127.0.0.1。</summary>
    public static IReadOnlyList<string> Prefer(IEnumerable<string> candidates)
    {
        var list = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Where(c => !IsExcluded(c))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(Rank)
            .ThenBy(c => c, StringComparer.Ordinal)
            .ToList();

        if (list.Count == 0)
            list.Add("127.0.0.1");
        return list;
    }

    public static string PickDefault(IEnumerable<string> candidates) => Prefer(candidates)[0];

    /// <summary>枚举本机活动网卡 IPv4（已过滤虚拟常见网段）。</summary>
    public static IReadOnlyList<string> EnumerateLocalIPv4()
    {
        var raw = new List<string>();
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        continue;
                    raw.Add(ua.Address.ToString());
                }
            }
        }
        catch
        {
            // fall through to Dns
        }

        if (raw.Count == 0)
        {
            try
            {
                raw.AddRange(System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                    .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(a => a.ToString()));
            }
            catch
            {
                /* empty */
            }
        }

        return Prefer(raw);
    }
}
