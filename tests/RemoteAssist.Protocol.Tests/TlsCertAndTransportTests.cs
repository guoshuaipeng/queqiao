using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Protocol.Tests;

public class TlsCertUtilTests
{
    [Fact]
    public void CreateEphemeral_HasStableSha256Fingerprint()
    {
        using var cert = TlsCertUtil.CreateEphemeralServerCert();
        var fp1 = TlsCertUtil.Sha256Fingerprint(cert);
        var fp2 = TlsCertUtil.Sha256Fingerprint(cert);
        Assert.Equal(64, fp1.Length);
        Assert.Equal(fp1, fp2);
        Assert.Equal(fp1, TlsCertUtil.NormalizeFingerprint(string.Join(':', Enumerable.Range(0, 32).Select(i => fp1.Substring(i * 2, 2)))));
    }

    [Fact]
    public void FingerprintsEqual_IgnoresSeparatorsAndCase()
    {
        Assert.True(TlsCertUtil.FingerprintsEqual("aabb", "AA:BB"));
        Assert.False(TlsCertUtil.FingerprintsEqual("aabb", "aacc"));
    }
}

public class TlsTransportSmokeTests
{
    [Fact]
    public async Task Tls_Oneshot_SmokePass()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-tls-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath, insecure: false);
        Assert.False(string.IsNullOrEmpty(host.CertificateFingerprint));
        host.Start();
        try
        {
            using var client = await AssistClient.PairAsync(
                "127.0.0.1", port, host.SessionCode, PermissionLevel.Assist, "tls-smoke",
                host.CertificateFingerprint, useTls: true);

            var procs = await client.InvokeToolAsync(ToolKind.ListProcesses);
            Assert.True(procs.Ok, procs.Error);

            var run = await client.InvokeToolAsync(ToolKind.RunCommand, new Dictionary<string, string?>
            {
                ["command"] = "echo remote-assist-tls-ok"
            });
            Assert.True(run.Ok, run.Error);
            Assert.Contains("remote-assist-tls-ok", run.Result ?? "", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            host.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task Tls_WrongFingerprint_Fails()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-tls-badfp-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath, insecure: false);
        host.Start();
        try
        {
            var wrong = new string('F', 64);
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                using var client = await AssistClient.PairAsync(
                    "127.0.0.1", port, host.SessionCode, PermissionLevel.Assist, "bad-fp",
                    wrong, useTls: true);
            });
        }
        finally
        {
            host.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task Insecure_Plaintext_StillWorks()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-plain-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath, insecure: true);
        Assert.Null(host.CertificateFingerprint);
        Assert.Contains("tls=0", host.QrPayload);
        host.Start();
        try
        {
            using var client = await AssistClient.PairAsync(
                "127.0.0.1", port, host.SessionCode, PermissionLevel.Assist, "plain",
                expectedFingerprint: null, useTls: false);
            var procs = await client.InvokeToolAsync(ToolKind.ListProcesses);
            Assert.True(procs.Ok, procs.Error);
        }
        finally
        {
            host.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void QrPayload_IncludesFingerprint_WhenTls()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-qr-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath, insecure: false);
        try
        {
            Assert.True(PairQrPayload.TryParse(host.QrPayload, out _, out var p, out _, out var fp, out var tls));
            Assert.Equal(port, p);
            Assert.True(tls);
            Assert.Equal(host.CertificateFingerprint, fp);
        }
        finally
        {
            host.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var p = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }
}
