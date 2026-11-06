    private async Task ConnectAsync()
    {
        if (_busy) return;
        if (_client is not null)
        {
            SetStatus("已连接，请先断开。");
            return;
        }

        if (!TryBuildOptions(out var opts, out var preview, out var err))
        {
            MessageBox.Show(this, err, "无法连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _busy = true;
        _btnConnect.Enabled = false;
        SetStatus("正在连接… " + preview);
        try
        {
            var client = await AssistClient.PairAsync(opts, "helper-ui").ConfigureAwait(true);
            _client = client;
            _settings.LastHost = opts.Host;
            _settings.LastPort = opts.Port;
            _settings.Save();
            SetConnectedUi(true);
            var tokenPreview = client.Token is { Length: >= 8 } t ? t[..8] : (client.Token ?? "");
            SetStatus($"已连接 {opts.Host}:{opts.Port} · 权限={client.Permission} · token={tokenPreview}…");
            AppendOutput($"PAIR_OK perm={client.Permission}\r\n");
        }
        catch (Exception ex)
        {
            SetStatus("连接失败：" + ex.Message);
            MessageBox.Show(this, ex.Message, "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetConnectedUi(false);
        }
        finally
        {
            _busy = false;
            if (_client is null)
                _btnConnect.Enabled = true;
        }
    }

    private void Disconnect()
    {
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;
        SetConnectedUi(false);
        SetStatus("状态：未连接");
    }

    private void SetConnectedUi(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnDisconnect.Enabled = connected;
        foreach (var b in _toolButtons)
            b.Enabled = connected;
        _inputBox.Enabled = !connected;
        _hostBox.Enabled = !connected;
        _portBox.Enabled = !connected;
        _fpBox.Enabled = !connected;
        _permBox.Enabled = !connected;
        _insecureBox.Enabled = !connected;
    }

    private string? RequireArg(string key)
    {
        var v = _argBox.Text?.Trim() ?? "";
        if (v.Length == 0)
        {
            MessageBox.Show(this, $"请填写 {key}（上方「路径 / 命令参数」）", "缺少参数",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return v;
    }

    private async Task RunToolAsync(ToolKind tool, Dictionary<string, string?> args)
    {
        if (_client is null || _busy) return;

        _busy = true;
        SetStatus($"执行 {tool} …");
        try
        {
            var resp = await _client.InvokeToolAsync(tool, args).ConfigureAwait(true);
            if (!resp.Ok)
            {
                var msg = resp.Error ?? "失败";
                if (resp.DeniedByUser)
                    msg = "对方拒绝：" + msg;
                SetStatus(msg);
                AppendOutput($"[{tool}] FAIL: {msg}\r\n");
                return;
            }

            SetStatus($"{tool} 成功");
            AppendOutput($"[{tool}] OK\r\n{resp.Result}\r\n");
        }
        catch (Exception ex)
        {
            SetStatus("调用失败：" + ex.Message);
            AppendOutput($"[{tool}] ERR: {ex.Message}\r\n");
        }
        finally
        {
            _busy = false;
        }
    }

    private void SaveRelaySetting()
    {
        var text = _relayBox.Text?.Trim() ?? "";
        if (text.Length > 0 && !RelayPlaceholder.TryParseEndpoint(text, out _, out _, out var err))
        {
            MessageBox.Show(this,
                err + "\r\n\r\n仍可留空；填写仅为以后公网中继预留。",
                "中继地址格式", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _settings.RelayEndpoint = string.IsNullOrEmpty(text) ? null : text;
        _settings.Save();
        SetStatus(string.IsNullOrEmpty(text)
            ? "已清除中继设置（v1 未启用）。"
            : $"已保存中继占位：{text}（即将支持，当前不连接）");
    }


    private void RequestHome()
    {
        if (!_enableReturnHome) return;
        if (_client is not null)
        {
            var r = MessageBox.Show(this,
                "当前已连接被控端。返回首页将断开连接。是否继续？",
                "返回首页",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;
        }

        _returningHome = true;
        HomeRequested?.Invoke();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_enableReturnHome && !_returningHome && e.CloseReason == CloseReason.UserClosing)
        {
            // 统一 App：关窗 = 回首页，而不是退出进程
            e.Cancel = true;
            RequestHome();
            return;
        }

        Disconnect();
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    private void AppendOutput(string text)
    {
        _outputBox.AppendText(text);
        if (_outputBox.TextLength > 200_000)
            _outputBox.Text = _outputBox.Text[^100_000..];
    }
}
