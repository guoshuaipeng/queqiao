namespace RemoteAssist.Protocol;

/// <summary>
/// 解析 Helper 配对目标：host+code、整段 QR 载荷、或显式 payload。
/// </summary>
public static class PairTargetResolver
{
    public sealed record Result(string Host, int Port, string Code);

    /// <summary>
    /// positional：非选项参数；payload：--payload 值；defaultPort：载荷未带 port 时不用（载荷必带）。
    /// 优先级：显式 payload &gt; 首个 positional 若为 remoteassist:pair &gt; host+code。
    /// </summary>
    public static bool TryResolve(
        IReadOnlyList<string> positional,
        string? explicitPayload,
        int defaultPort,
        out Result result,
        out string error)
    {
        result = new Result("", defaultPort, "");
        error = "";

        if (!string.IsNullOrWhiteSpace(explicitPayload))
        {
            if (!PairQrPayload.TryParse(explicitPayload, out var h, out var p, out var c))
            {
                error = "无法解析 --payload / 剪贴板中的 remoteassist:pair 载荷";
                return false;
            }
            result = new Result(h, p, c);
            return true;
        }

        if (positional.Count >= 1 &&
            positional[0].Trim().StartsWith(PairQrPayload.SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!PairQrPayload.TryParse(positional[0], out var h, out var p, out var c))
            {
                error = "无法解析会话载荷（remoteassist:pair?...）";
                return false;
            }
            result = new Result(h, p, c);
            return true;
        }

        if (positional.Count >= 2)
        {
            var host = positional[0].Trim();
            var code = SessionCodeGenerator.Normalize(positional[1]);
            if (string.IsNullOrWhiteSpace(host))
            {
                error = "host 不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                error = "sessionCode 不能为空";
                return false;
            }
            result = new Result(host, defaultPort, code);
            return true;
        }

        error = "用法: pair <host> <sessionCode> | pair <remoteassist:pair?...> | pair --payload <载荷> | pair --clipboard";
        return false;
    }
}
