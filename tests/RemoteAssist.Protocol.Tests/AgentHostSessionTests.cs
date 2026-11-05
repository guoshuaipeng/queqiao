using System.Net;
using System.Net.Sockets;
using RemoteAssist.Agent;
using RemoteAssist.Protocol;
using RemoteAssist.Protocol.Messages;

namespace RemoteAssist.Protocol.Tests;

public class AgentHostSessionTests
{
    [Fact]
    public async Task DisconnectSession_RevokesToken_AndDropsClient()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-test-audit-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath);
        host.Start();
        try
        {
            Assert.Empty(host.ListPairedClients());

            using var client = await AssistClient.PairAsync(
                "127.0.0.1", port, host.SessionCode, PermissionLevel.Assist, "test-client",
                host.CertificateFingerprint, useTls: true);
            // 等配对登记
            for (var i = 0; i < 20 && host.ListPairedClients().Count == 0; i++)
                await Task.Delay(50);
            Assert.Single(host.ListPairedClients());
            Assert.Equal("test-client", host.ListPairedClients()[0].ClientName);

            var token = host.ListPairedClients()[0].Token;
            Assert.True(host.DisconnectSession(token));
            Assert.Empty(host.ListPairedClients());

            // 踢掉后：要么工具返回未授权，要么 TCP 已被关闭抛 IO 异常
            try
            {
                var resp = await client.InvokeToolAsync(ToolKind.ListProcesses);
                Assert.False(resp.Ok);
            }
            catch (IOException)
            {
                // 连接被 ConnectionCts 取消，属预期
            }
            catch (SocketException)
            {
                // 同上
            }

            var log = await File.ReadAllTextAsync(auditPath);
            Assert.Contains("session_kick", log);
        }
        finally
        {
            host.Stop();
            try { File.Delete(auditPath); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void SetSelectedHost_UpdatesQrPayload()
    {
        var port = GetFreePort();
        var auditPath = Path.Combine(Path.GetTempPath(), "ra-test-audit-" + Guid.NewGuid().ToString("N") + ".log");
        var audit = new AuditLog(auditPath);
        var host = new AgentHost(port, autoYes: true, headless: true, confirmTimeoutSeconds: 30, audit, auditPath);
        try
        {
            host.SetSelectedHost("127.0.0.1");
            Assert.Equal("127.0.0.1", host.SelectedHost);
            var payload = host.QrPayload;
            Assert.True(PairQrPayload.TryParse(payload, out var h, out var p, out var c, out var fp, out var tls));
            Assert.Equal("127.0.0.1", h);
            Assert.Equal(port, p);
            Assert.Equal(host.SessionCode, c);
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
