using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteAssist.Protocol
{
    public static class Net48Guard
    {
        public static void ThrowIfNull(object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
        }
    }

    public static class Net48Hex
    {
        public static string ToHexString(byte[] bytes)
        {
            var c = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                c[i * 2] = GetHex(b >> 4);
                c[i * 2 + 1] = GetHex(b & 0xF);
            }
            return new string(c);
        }

        private static char GetHex(int v) => (char)(v < 10 ? ('0' + v) : ('A' + (v - 10)));
    }

    public static class Net48Math
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public static class Net48Linq
    {
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return Array.Empty<T>();
            var q = new Queue<T>(count);
            foreach (var item in source)
            {
                if (q.Count == count) q.Dequeue();
                q.Enqueue(item);
            }
            return q.ToArray();
        }
    }

    public static class Net48Crypto
    {
        public static byte[] Sha256Hash(byte[] data)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(data);
        }

        public static void FillRandom(byte[] buffer)
        {
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(buffer);
        }
    }

    public static class Net48Sockets
    {
        public static async Task ConnectAsync(TcpClient tcp, string host, int port, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using (ct.Register(() => { try { tcp.Close(); } catch { } }))
            {
                await tcp.ConnectAsync(host, port).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
        }

        public static async Task<TcpClient> AcceptTcpClientAsync(TcpListener listener, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var accept = listener.AcceptTcpClientAsync();
            var cancelTcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => cancelTcs.TrySetResult(true)))
            {
                var winner = await Task.WhenAny(accept, cancelTcs.Task).ConfigureAwait(false);
                if (winner != accept)
                    throw new OperationCanceledException(ct);
                return await accept.ConfigureAwait(false);
            }
        }
    }

    public static class Net48Tls
    {
        public static readonly SslProtocols Protocols = SslProtocols.Tls12;

        public static async Task<SslStream> AuthenticateAsServerAsync(
            Stream inner, X509Certificate2 serverCert, CancellationToken ct)
        {
            Net48Guard.ThrowIfNull(inner);
            Net48Guard.ThrowIfNull(serverCert);
            var ssl = new SslStream(inner, leaveInnerStreamOpen: false);
            ct.ThrowIfCancellationRequested();
            await Task.Factory.FromAsync(
                (cb, state) => ssl.BeginAuthenticateAsServer(serverCert, false, Protocols, false, cb, state),
                ssl.EndAuthenticateAsServer,
                null).ConfigureAwait(false);
            return ssl;
        }

        public static async Task<SslStream> AuthenticateAsClientAsync(
            Stream inner, string expectedFingerprint, CancellationToken ct)
        {
            Net48Guard.ThrowIfNull(inner);
            var expected = TlsCertUtil.NormalizeFingerprint(expectedFingerprint);
            if (string.IsNullOrEmpty(expected))
                throw new ArgumentException("TLS needs fingerprint", nameof(expectedFingerprint));

            string? seen = null;
            var ssl = new SslStream(
                inner,
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (_, cert, __, ___) =>
                {
                    if (cert is null) return false;
                    using (var wrapped = new X509Certificate2(cert))
                    {
                        seen = TlsCertUtil.Sha256Fingerprint(wrapped);
                        return TlsCertUtil.FingerprintsEqual(seen, expected);
                    }
                });

            try
            {
                ct.ThrowIfCancellationRequested();
                await Task.Factory.FromAsync(
                    (cb, state) => ssl.BeginAuthenticateAsClient(TlsCertUtil.ServerCommonName, null, Protocols, false, cb, state),
                    ssl.EndAuthenticateAsClient,
                    null).ConfigureAwait(false);
            }
            catch (AuthenticationException ex)
            {
                var hint = seen is null
                    ? "peer cert missing"
                    : ("fingerprint mismatch expect " + expected.Substring(0, Math.Min(16, expected.Length)));
                ssl.Dispose();
                throw new AuthenticationException("TLS handshake failed: " + hint, ex);
            }

            return ssl;
        }
    }

    public static class Net48String
    {
        public static bool Contains(string haystack, string needle, StringComparison comparison) =>
            haystack.IndexOf(needle, comparison) >= 0;

        public static string Replace(string input, string oldValue, string newValue, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue))
                return input;
            if (comparison == StringComparison.Ordinal)
                return input.Replace(oldValue, newValue);
            // ordinal ignore case / culture: manual
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < input.Length)
            {
                var idx = input.IndexOf(oldValue, i, comparison);
                if (idx < 0)
                {
                    sb.Append(input.Substring(i));
                    break;
                }
                sb.Append(input, i, idx - i);
                sb.Append(newValue);
                i = idx + oldValue.Length;
            }
            return sb.ToString();
        }

        public static string[] Split(string input, char separator, StringSplitOptions options)
        {
            var parts = input.Split(new[] { separator }, options);
            return parts;
        }

        public static void ThrowIfNullOrWhiteSpace(string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(argument))
                throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }
    }
}

