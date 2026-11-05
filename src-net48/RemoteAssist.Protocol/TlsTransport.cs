using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteAssist.Protocol;

public static class TlsTransport
{
    public static Task<SslStream> AuthenticateAsServerAsync(
        Stream inner,
        X509Certificate2 serverCert,
        CancellationToken ct = default) =>
        Net48Tls.AuthenticateAsServerAsync(inner, serverCert, ct);

    public static Task<SslStream> AuthenticateAsClientAsync(
        Stream inner,
        string expectedFingerprint,
        CancellationToken ct = default) =>
        Net48Tls.AuthenticateAsClientAsync(inner, expectedFingerprint, ct);
}
