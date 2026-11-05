using RemoteAssist.Agent;

namespace RemoteAssist.Protocol.Tests;

public class AuditLogTests
{
    [Fact]
    public void Write_And_ReadRecent_ReturnsNewestTail_InChronologicalOrder()
    {
        var path = Path.Combine(Path.GetTempPath(), "ra-audit-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            var audit = new AuditLog(path);
            audit.Write("pair_ok", "client=a");
            audit.Write("tool_ok", "tool=ListProcesses");
            audit.Write("session_kick", "client=a");

            var recent = audit.ReadRecent(10);
            Assert.Equal(3, recent.Count);
            Assert.Equal("pair_ok", recent[0].EventType);
            Assert.Equal("tool_ok", recent[1].EventType);
            Assert.Equal("session_kick", recent[2].EventType);
            Assert.Contains("配对成功", recent[0].DisplaySummary);
            Assert.Contains("工具成功", recent[1].DisplaySummary);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ReadRecent_RespectsMaxLines()
    {
        var path = Path.Combine(Path.GetTempPath(), "ra-audit-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            var audit = new AuditLog(path);
            for (var i = 0; i < 20; i++)
                audit.Write("evt", "n=" + i);

            var recent = audit.ReadRecent(5);
            Assert.Equal(5, recent.Count);
            Assert.Contains("n=15", recent[0].Detail);
            Assert.Contains("n=19", recent[4].Detail);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void EntryWritten_Fires_OnWrite()
    {
        var path = Path.Combine(Path.GetTempPath(), "ra-audit-" + Guid.NewGuid().ToString("N") + ".log");
        try
        {
            var audit = new AuditLog(path);
            var n = 0;
            audit.EntryWritten += () => n++;
            audit.Write("x", "y");
            Assert.Equal(1, n);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
