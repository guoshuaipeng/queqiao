# -*- coding: utf-8 -*-
from pathlib import Path
p = Path("src-net48/RemoteAssist.Agent/MainForm.cs")
t = p.read_text(encoding="utf-8")

repls = [
(
'Text = "接受协助 · 点「复制连接凭证」发完整安全载荷 · 危险操作需本机确认",',
'Text = "接受协助 · " + FriendConnectGuide.AcceptGuideHint + "危险操作需本机确认",',
),
(
'''            Text = "点「复制连接凭证」把完整安全载荷发给对方（含业务指纹）；上方短码仅供口述",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)''',
'''            Text = FriendConnectGuide.AcceptGuideHint,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)'''
),
(
'''            Text = "口述短码（不可单独加密连接）",
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)''',
'''            Text = FriendConnectGuide.OralShortCodeHint,
            ForeColor = Color.FromArgb(0x5F, 0x63, 0x68)'''
),
(
'var btnCopyCredential = MakeButton("复制连接凭证", 16, 168, 160);',
'var btnCopyCredential = MakeButton(FriendConnectGuide.CopyCredentialButtonText, 16, 168, 160);',
),
(
'var btnCopyCode = MakeButton("复制连接凭证", 252, 48, 100);',
'var btnCopyCode = MakeButton(FriendConnectGuide.CopyCredentialButtonText, 252, 48, 100);',
),
(
'Text = "TLS 指纹"',
'Text = "安全验证信息"'
),
(
'SetStatus("已复制会话载荷（含 IP / 端口 / 会话码 / TLS 指纹）。");',
'SetStatus(FriendConnectGuide.CopyCredentialSucceededHint);',
),
(
'catch (Exception ex) { SetStatus("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message)); }',
'''catch (Exception ex)
            {
                ShowCopyCredentialFailed(_host.QrPayload, ex);
            }''',
),
]

for a,b in repls:
    if a not in t:
        raise SystemExit("Agent missing:\\n"+repr(a[:120]))
    t = t.replace(a,b,1)

# CopySecureCredential method replace whole
old_csc = '''    private void CopySecureCredential()
    {
        try
        {
            if (_host.PreferRelay)
            {
                if (!_host.UseTls || string.IsNullOrWhiteSpace(_host.CertificateFingerprint))
                {
                    SetStatus("当前无法生成加密载荷（明文调试或证书未就绪）。");
                    return;
                }
                if (!TlsCertUtil.IsWellFormedFingerprint(_host.CertificateFingerprint))
                {
                    SetStatus("证书指纹异常，请刷新会话后重试。");
                    return;
                }
                // 与 QrPayload / PairQrPayload.BuildRelay 同一形态（含 fp、pwd、relay）
                Clipboard.SetText(_host.QrPayload);
                SetStatus("已复制完整安全连接载荷（请发给协助方一次粘贴）。短码不能单独加密连接。");
                return;
            }

            Clipboard.SetText(_host.QrPayload);
            SetStatus("已复制完整安全连接载荷。");
        }
        catch (Exception ex)
        {
            SetStatus("复制失败：" + FriendConnectGuide.RedactSecureMaterial(ex.Message));
        }
    }'''

new_csc = '''    private void CopySecureCredential()
    {
        string payload;
        try
        {
            if (_host.PreferRelay)
            {
                if (!_host.UseTls || string.IsNullOrWhiteSpace(_host.CertificateFingerprint))
                {
                    SetStatus("当前无法生成完整连接凭证（调试模式或安全验证信息未就绪）。");
                    return;
                }
                if (!TlsCertUtil.IsWellFormedFingerprint(_host.CertificateFingerprint))
                {
                    SetStatus("安全验证信息异常，请刷新会话后重试。");
                    return;
                }
            }

            payload = _host.QrPayload;
            Clipboard.SetText(payload);
            SetStatus(FriendConnectGuide.CopyCredentialSucceededHint);
        }
        catch (Exception ex)
        {
            try { payload = _host.QrPayload; }
            catch { payload = ""; }
            ShowCopyCredentialFailed(payload, ex);
        }
    }

    /// <summary>剪贴板失败：逐字失败提示，并展示完整连接凭证供手动选取（不写日志）。</summary>
    private void ShowCopyCredentialFailed(string? payload, Exception? ex = null)
    {
        SetStatus(FriendConnectGuide.CopyCredentialFailedHint);
        if (string.IsNullOrWhiteSpace(payload))
            return;
        try
        {
            using var dlg = new Form
            {
                Text = "手动复制连接凭证",
                Width = 640,
                Height = 220,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false
            };
            var tb = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                Text = payload
            };
            dlg.Controls.Add(tb);
            dlg.Shown += (_, _) => { tb.SelectAll(); tb.Focus(); };
            dlg.ShowDialog(this);
        }
        catch
        {
            /* 展示失败不影响主流程；状态栏已是失败逐字句 */
        }
    }'''

if old_csc not in t:
    raise SystemExit("CopySecureCredential block missing")
t = t.replace(old_csc, new_csc)

# header subtitle refresh
old_h = '''            _headerSubtitle.Text = n == 0
            ? "接受协助 · 点「复制连接凭证」发完整安全载荷 · 危险操作需本机确认"'''
# need full ternary
import re
m = re.search(r'_headerSubtitle\.Text = n == 0\s*\n\s*\? "[^"]+"\s*\n\s*: [^;]+;', t)
if not m:
    # try find lines
    idx = t.find('_headerSubtitle.Text = n == 0')
    print("HEADER CONTEXT:", repr(t[idx:idx+200]))
    raise SystemExit("header subtitle refresh missing")
new_h = '''_headerSubtitle.Text = n == 0
            ? "接受协助 · " + FriendConnectGuide.AcceptGuideHint + "危险操作需本机确认"
            : $"被协助端 · 已有 {n} 个协助方连接 · 可随时断开";'''
# careful - replace only the assignment
old_assign = m.group(0)
t = t.replace(old_assign, new_h)

# TLS value display strings on default card
t = t.replace('_tlsValue.Text = "中继 TLS · " + shortFp;', '_tlsValue.Text = "已加密 · " + shortFp;')
t = t.replace(
    'Tip().SetToolTip(_tlsValue, "业务 TLS 指纹（完整载荷含 fp=）：" + fpRelay + " · 中继 " + _host.RelayEndpointAddress);',
    'Tip().SetToolTip(_tlsValue, "安全验证信息已就绪 · 中继 " + _host.RelayEndpointAddress);'
)
# LAN tooltip with full fp - soften
t = t.replace(
    'Tip().SetToolTip(_tlsValue, fp);',
    'Tip().SetToolTip(_tlsValue, "安全验证信息已就绪");'
)

# UpdateListenLabel TLS 开
t = t.replace('var tls = _host.UseTls ? "TLS 开" : "明文";', 'var tls = _host.UseTls ? "已加密" : "明文";')

# btnCopyPayload still uses ShowCopyCredentialFailed now for one catch - the first catch was replaced. Good.

# Ensure FriendConnectGuide using - Agent MainForm should already have Protocol
if "using RemoteAssist.Protocol;" not in t:
    t = "using RemoteAssist.Protocol;\n" + t

p.write_text(t, encoding="utf-8", newline="\n")
print("Agent MainForm OK")
