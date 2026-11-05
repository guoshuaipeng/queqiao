using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace RemoteAssist.Protocol;

/// <summary>用 SslStream 包裹已连接的 TCP 流；客户端按指纹钉扎，不信任系统 CA。</summary>
public static class TlsTransport
{
    public static async Task<SslStream> AuthenticateAsServerAsync(
        Stream inner,
        X509Certificate2 serverCert,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(serverCert);

        var ssl = new SslStream(inner, leaveInnerStreamOpen: false);
        var opts = new SslServerAuthenticationOptions
        {
            ServerCertificate = serverCert,
            ClientCertificateRequired = false,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck
        };
        await ssl.AuthenticateAsServerAsync(opts, ct).ConfigureAwait(false);
        return ssl;
    }

    /// <summary>
    /// 客户端 TLS。必须提供 expectedFingerprint；握手时校验对端证书指纹，不匹配则失败。
    /// </summary>
    public static async Task<SslStream> AuthenticateAsClientAsync(
        Stream inner,
        string expectedFingerprint,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inner);
        var expected = TlsCertUtil.NormalizeFingerprint(expectedFingerprint);
        if (string.IsNullOrEmpty(expected))
            throw new ArgumentException("TLS 需要证书指纹（载荷 fp= 或 --fp / REMOTE_ASSIST_FP）", nameof(expectedFingerprint));

        string? seen = null;
        var ssl = new SslStream(
            inner,
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (_, cert, _, _) =>
            {
                if (cert is null)
                    return false;
                // SslStream 回调里的 cert 可能是 X509Certificate；统一算指纹
                using var wrapped = new X509Certificate2(cert);
                seen = TlsCertUtil.Sha256Fingerprint(wrapped);
                return TlsCertUtil.FingerprintsEqual(seen, expected);
            });

        try
        {
            var opts = new SslClientAuthenticationOptions
            {
                TargetHost = TlsCertUtil.ServerCommonName,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            };
            await ssl.AuthenticateAsClientAsync(opts, ct).ConfigureAwait(false);
        }
        catch (AuthenticationException ex)
        {
            var hint = seen is null
                ? "未拿到对端证书（Agent 是否开启 TLS？双方 --insecure 是否一致？）"
                : $"证书指纹不匹配：期望 {expected[..Math.Min(16, expected.Length)]}… 实际 {seen[..Math.Min(16, seen.Length)]}…";
            ssl.Dispose();
            throw new AuthenticationException("TLS 握手失败: " + hint, ex);
        }

        return ssl;
    }
}
