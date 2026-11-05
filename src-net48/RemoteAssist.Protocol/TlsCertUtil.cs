using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RemoteAssist.Protocol;

/// <summary>
/// 局域网 TLS：临时自签证书 + SHA-256 指纹（写入 QR 载荷，客户端钉扎校验）。
/// </summary>
public static class TlsCertUtil
{
    public const string ServerCommonName = "RemoteAssist.Agent";

    /// <summary>生成仅用于本进程生命周期的自签服务器证书（含私钥）。</summary>
    public static X509Certificate2 CreateEphemeralServerCert(TimeSpan? lifetime = null)
    {
        var life = lifetime ?? TimeSpan.FromDays(7);
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            $"CN={ServerCommonName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // serverAuth
            critical: false));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.Add(life);
        using var created = req.CreateSelfSigned(notBefore, notAfter);

        // SslStream 需要可导出的私钥副本
#pragma warning disable SYSLIB0057 // X509Certificate2(byte[]) obsolete in newer TFMs; fine on net8
        return new X509Certificate2(
            created.Export(X509ContentType.Pfx),
            (string?)null,
            X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
    }

    /// <summary>证书原始字节的 SHA-256，大写十六进制无分隔符（与 Convert.ToHexString 一致）。</summary>
    public static string Sha256Fingerprint(X509Certificate2 cert)
    {
        Net48Guard.ThrowIfNull(cert);
        var hash = Net48Crypto.Sha256Hash(cert.RawData);
        return Net48Hex.ToHexString(hash);
    }

    /// <summary>规范化用户输入的指纹（去冒号/空格/横线，转大写）。</summary>
    public static string NormalizeFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return "";
        var chars = fingerprint
            .Where(c => c is not (':' or ' ' or '-'))
            .ToArray();
        return new string(chars).ToUpperInvariant();
    }

    public static bool FingerprintsEqual(string? a, string? b) =>
        string.Equals(NormalizeFingerprint(a), NormalizeFingerprint(b), StringComparison.Ordinal);
}
