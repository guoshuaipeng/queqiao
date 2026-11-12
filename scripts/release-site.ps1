# 鹊桥：打包客户端并发布到官网 downloads/
#
# 用法示例：
#   .\scripts\release-site.ps1                          # 交互选择平台
#   .\scripts\release-site.ps1 -Platforms windows
#   .\scripts\release-site.ps1 -Platforms windows,linux,macos -Version 0.1.4
#   .\scripts\release-site.ps1 -Platforms macos -SkipBuild   # 只用已有 artifacts
#   .\scripts\release-site.ps1 -Platforms windows -SkipUpload # 只打包
#   .\scripts\release-site.ps1 -Platforms windows -SkipSite   # 不换首页二进制
#
# 环境变量（可选）：
#   QUEQIAO_SSH            上传目标，默认 root@121.196.45.81
#   QUEQIAO_SITE           官网根，默认 http://121.196.45.81/
#   QUEQIAO_DEFAULT_RELAY  编译期默认中继，默认 121.196.45.81:17891
#
# 产物目录：artifacts/release-<version>/
# 口令不进脚本；需对本机 ssh/scp 到 QUEQIAO_SSH 免密（或已有 agent）。也可用 -SkipUpload 后改走 ssh-connect MCP。

[CmdletBinding()]
param(
    # 可多次传入，或逗号分隔：-Platforms windows,macos
    [string[]]$Platforms,

    [string]$Version,

    [string]$Notes = "",

    [string]$SshTarget = $(if ($env:QUEQIAO_SSH) { $env:QUEQIAO_SSH } else { "root@121.196.45.81" }),

    [string]$SiteBase = $(if ($env:QUEQIAO_SITE) { $env:QUEQIAO_SITE.TrimEnd("/") } else { "http://121.196.45.81" }),

    [string]$Relay = $(if ($env:QUEQIAO_DEFAULT_RELAY) { $env:QUEQIAO_DEFAULT_RELAY } else { "121.196.45.81:17891" }),

    [string]$ArtifactsRoot,

    [switch]$SkipBuild,
    [switch]$SkipUpload,
    [switch]$SkipSite,
    [switch]$ForceLatestVersion,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Info([string]$msg) { Write-Host "    $msg" }
function Write-Warn2([string]$msg) { Write-Host "    ! $msg" -ForegroundColor Yellow }

function Get-CargoVersion {
    $cargo = Join-Path $root "apps\queqiao-tauri\src-tauri\Cargo.toml"
    $m = Select-String -Path $cargo -Pattern '^\s*version\s*=\s*"([^"]+)"' | Select-Object -First 1
    if (-not $m) { throw "读不到 Cargo.toml version" }
    return $m.Matches[0].Groups[1].Value
}

function Set-ProjectVersion([string]$ver) {
    Write-Step "同步版本号 → $ver"
    $paths = @(
        @{ Path = "apps\queqiao-tauri\src-tauri\Cargo.toml"; Pattern = '(?m)^version\s*=\s*"[^"]+"'; Replace = "version = `"$ver`"" },
        @{ Path = "apps\queqiao-tauri\package.json"; Pattern = '"version"\s*:\s*"[^"]+"'; Replace = "`"version`": `"$ver`"" },
        @{ Path = "apps\queqiao-tauri\src-tauri\tauri.conf.json"; Pattern = '"version"\s*:\s*"[^"]+"'; Replace = "`"version`": `"$ver`"" }
    )
    foreach ($item in $paths) {
        $full = Join-Path $root $item.Path
        $text = Get-Content -Raw -Encoding UTF8 $full
        $new = [regex]::Replace($text, $item.Pattern, $item.Replace, 1)
        if ($text -eq $new) { Write-Warn2 "未改到: $($item.Path)" }
        elseif ($DryRun) { Write-Info "[dry-run] 将改 $($item.Path)" }
        else { Set-Content -Path $full -Value $new -Encoding UTF8 -NoNewline }
    }
}

function Update-WorkflowFilenames([string]$ver, [string[]]$plats) {
    $wf = Join-Path $root ".github\workflows\release-packages.yml"
    if (-not (Test-Path $wf)) { return }
    $text = Get-Content -Raw -Encoding UTF8 $wf
    $orig = $text
    if ($plats -contains "windows") {
        $text = [regex]::Replace($text, "Queqiao-Setup-\d+\.\d+\.\d+\.exe", "Queqiao-Setup-$ver.exe")
    }
    if ($plats -contains "macos") {
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-macos-arm64\.dmg", "Queqiao-$ver-macos-arm64.dmg")
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-macos-x64\.dmg", "Queqiao-$ver-macos-x64.dmg")
    }
    if ($plats -contains "linux") {
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-linux-x64\.AppImage", "Queqiao-$ver-linux-x64.AppImage")
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-linux-x64\.deb", "Queqiao-$ver-linux-x64.deb")
        $text = [regex]::Replace($text, "Queqiao-MCP-\d+\.\d+\.\d+-linux-x64", "Queqiao-MCP-$ver-linux-x64")
    }
    if ($text -eq $orig) { return }
    if ($DryRun) { Write-Info "[dry-run] 将更新 release-packages.yml 产物名" }
    else {
        Set-Content -Path $wf -Value $text -Encoding UTF8 -NoNewline
        Write-Info "已更新 .github/workflows/release-packages.yml 产物文件名"
    }
}

function Update-SiteIndex([string]$ver, [string[]]$plats) {
    $index = Join-Path $root "site\index.html"
    $text = Get-Content -Raw -Encoding UTF8 $index
    $orig = $text
    if ($plats -contains "windows") {
        $text = [regex]::Replace($text, "Queqiao-Setup-\d+\.\d+\.\d+\.exe", "Queqiao-Setup-$ver.exe")
        $text = [regex]::Replace(
            $text,
            '(?s)(<div><h3>Windows</h3>.*?<ul class="dl-meta">\s*<li>)版本\s*\d+\.\d+\.\d+',
            "`${1}版本 $ver"
        )
    }
    if ($plats -contains "macos") {
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-macos-arm64\.dmg", "Queqiao-$ver-macos-arm64.dmg")
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-macos-x64\.dmg", "Queqiao-$ver-macos-x64.dmg")
        $text = [regex]::Replace(
            $text,
            '(?s)(<div><h3>macOS</h3>.*?<ul class="dl-meta">\s*<li>)版本\s*\d+\.\d+\.\d+',
            "`${1}版本 $ver"
        )
    }
    if ($plats -contains "linux") {
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-linux-x64\.AppImage", "Queqiao-$ver-linux-x64.AppImage")
        $text = [regex]::Replace($text, "Queqiao-\d+\.\d+\.\d+-linux-x64\.deb", "Queqiao-$ver-linux-x64.deb")
        $text = [regex]::Replace($text, "Queqiao-MCP-\d+\.\d+\.\d+-linux-x64\.tar\.gz", "Queqiao-MCP-$ver-linux-x64.tar.gz")
        $text = [regex]::Replace(
            $text,
            '(?s)(<div><h3>Linux</h3>.*?<ul class="dl-meta">\s*<li>)版本\s*\d+\.\d+\.\d+',
            "`${1}版本 $ver"
        )
    }

    $all = (@("windows", "macos", "linux") | Where-Object { $plats -contains $_ }).Count -eq 3
    if ($all) {
        $text = [regex]::Replace(
            $text,
            '<p class="hero-meta">.*?</p>',
            "<p class=`"hero-meta`">版本 $ver · Windows / macOS / Linux · 不用另外安装运行库</p>"
        )
    }

    if ($text -eq $orig) {
        Write-Warn2 "site/index.html 无链接变更（可能已是目标版本）"
        return $false
    }
    if ($DryRun) {
        Write-Info "[dry-run] 将更新 site/index.html"
        return $true
    }
    Set-Content -Path $index -Value $text -Encoding UTF8 -NoNewline
    Write-Info "已更新 site/index.html 下载链接"
    return $true
}

function Select-PlatformsInteractive {
    Write-Host ""
    Write-Host "选择要打包/发布的平台（可多选，逗号分隔）："
    Write-Host "  1) windows"
    Write-Host "  2) macos"
    Write-Host "  3) linux"
    Write-Host "  4) all"
    $ans = Read-Host "输入编号或名字"
    $ans = $ans.Trim().ToLowerInvariant()
    if ($ans -eq "4" -or $ans -eq "all") { return @("windows", "macos", "linux") }
    $map = @{ "1" = "windows"; "2" = "macos"; "3" = "linux" }
    $parts = $ans -split "[,\s]+" | Where-Object { $_ }
    $out = @()
    foreach ($p in $parts) {
        if ($map.ContainsKey($p)) { $out += $map[$p] }
        elseif ($p -in @("windows", "macos", "linux", "win", "mac")) {
            if ($p -eq "win") { $out += "windows" }
            elseif ($p -eq "mac") { $out += "macos" }
            else { $out += $p }
        }
        else { throw "无法识别平台: $p" }
    }
    $out = $out | Select-Object -Unique
    if (-not $out) { throw "未选择任何平台" }
    return ,$out
}

function Ensure-Dir([string]$path) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

function Invoke-Ssh([string]$command) {
    if ($DryRun) {
        Write-Info "[dry-run] ssh $SshTarget $command"
        return
    }
    # Windows here-string 带 CRLF，远端 bash 会把 \r 当成单元名/选项的一部分而失败。
    $command = $command -replace "`r`n", "`n"
    & ssh -o BatchMode=yes -o ConnectTimeout=15 $SshTarget $command
    if ($LASTEXITCODE -ne 0) {
        throw "ssh 失败 ($LASTEXITCODE)。请配置对 $SshTarget 的免密登录，或改用 -SkipUpload 后走 ssh-connect MCP。"
    }
}

function Send-RemoteFile([string]$local, [string]$remote) {
    if (-not (Test-Path $local)) { throw "缺少本地文件: $local" }
    if ($DryRun) {
        Write-Info "[dry-run] scp $local → ${SshTarget}:$remote"
        return
    }
    & scp -o BatchMode=yes -o ConnectTimeout=15 $local "${SshTarget}:$remote"
    if ($LASTEXITCODE -ne 0) {
        throw "scp 失败 ($LASTEXITCODE): $local"
    }
}

function Build-Windows([string]$ver, [string]$outDir) {
    if ($env:OS -ne "Windows_NT") {
        throw "当前不是 Windows，无法本机打 NSIS。请把 Queqiao-Setup-$ver.exe 放到 $outDir，或加 -SkipBuild 并用 Actions 产物。"
    }
    Write-Step "构建 Windows $ver"
    $env:QUEQIAO_DEFAULT_RELAY = $Relay
    $env:QUEQIAO_SITE = "$SiteBase/"

    Write-Info "cargo build -p queqiao-mcp --release"
    & cargo build -p queqiao-mcp --release
    if ($LASTEXITCODE -ne 0) { throw "MCP 构建失败" }

    $mcpSrc = Join-Path $root "target\release\remote_assist_mcp.exe"
    $mcpResDir = Join-Path $root "apps\queqiao-tauri\src-tauri\resources\mcp"
    Ensure-Dir $mcpResDir
    Copy-Item -Force $mcpSrc (Join-Path $mcpResDir "RemoteAssist.Mcp.exe")

    Push-Location (Join-Path $root "apps\queqiao-tauri")
    try {
        if (-not (Test-Path "node_modules")) {
            Write-Info "npm ci"
            & npm ci
            if ($LASTEXITCODE -ne 0) { throw "npm ci 失败" }
        }
        Write-Info "npm run build -- --bundles nsis"
        & npm run build -- --bundles nsis
        if ($LASTEXITCODE -ne 0) { throw "Tauri NSIS 构建失败" }
    }
    finally { Pop-Location }

    $nsisDir = Join-Path $root "apps\queqiao-tauri\src-tauri\target\release\bundle\nsis"
    $setup = Get-ChildItem -Path $nsisDir -Filter "*-setup.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $setup) { throw "未找到 NSIS 安装包: $nsisDir" }

    Ensure-Dir $outDir
    $destSetup = Join-Path $outDir "Queqiao-Setup-$ver.exe"
    Copy-Item -Force $setup.FullName $destSetup
    Copy-Item -Force (Join-Path $mcpResDir "RemoteAssist.Mcp.exe") (Join-Path $outDir "RemoteAssist.Mcp-windows-x64.exe")
    Write-Info "OK $destSetup"
    Write-Info ("SHA256 " + (Get-FileHash -Algorithm SHA256 $destSetup).Hash)
}

function Require-Artifact([string]$path, [string]$hint) {
    if (-not (Test-Path $path)) {
        throw "缺少产物: $path`n$hint"
    }
}

function Assert-PlatformArtifacts([string]$ver, [string]$outDir, [string[]]$plats) {
    $hintGh = "请先 GitHub Actions 出包并下载到此目录，或去掉 -SkipBuild（Windows 可本机打）。"
    $check = {
        param($path)
        if (Test-Path $path) { return }
        if ($DryRun) { Write-Warn2 "[dry-run] 将需要: $path"; return }
        Require-Artifact $path $hintGh
    }
    if ($plats -contains "windows") { & $check (Join-Path $outDir "Queqiao-Setup-$ver.exe") }
    if ($plats -contains "macos") {
        & $check (Join-Path $outDir "Queqiao-$ver-macos-arm64.dmg")
        & $check (Join-Path $outDir "Queqiao-$ver-macos-x64.dmg")
    }
    if ($plats -contains "linux") {
        & $check (Join-Path $outDir "Queqiao-$ver-linux-x64.AppImage")
        & $check (Join-Path $outDir "Queqiao-MCP-$ver-linux-x64.tar.gz")
    }
}

function Publish-Downloads([string]$ver, [string]$outDir, [string[]]$plats) {
    Write-Step "上传 downloads/ ($($plats -join ', '))"
    $remoteDl = "/var/lib/remote-assist-site/downloads"
    $files = @()
    if ($plats -contains "windows") {
        $files += @(
            @{ Local = "Queqiao-Setup-$ver.exe"; RemoteName = "Queqiao-Setup-$ver.exe"; Mode = "644" },
            @{ Local = "RemoteAssist.Mcp-windows-x64.exe"; RemoteName = "RemoteAssist.Mcp-windows-x64.exe"; Mode = "644"; Optional = $true }
        )
    }
    if ($plats -contains "macos") {
        $files += @(
            @{ Local = "Queqiao-$ver-macos-arm64.dmg"; RemoteName = "Queqiao-$ver-macos-arm64.dmg"; Mode = "644" },
            @{ Local = "Queqiao-$ver-macos-x64.dmg"; RemoteName = "Queqiao-$ver-macos-x64.dmg"; Mode = "644" }
        )
    }
    if ($plats -contains "linux") {
        $files += @(
            @{ Local = "Queqiao-$ver-linux-x64.AppImage"; RemoteName = "Queqiao-$ver-linux-x64.AppImage"; Mode = "755" },
            @{ Local = "Queqiao-MCP-$ver-linux-x64.tar.gz"; RemoteName = "Queqiao-MCP-$ver-linux-x64.tar.gz"; Mode = "644" },
            @{ Local = "Queqiao-$ver-linux-x64.deb"; RemoteName = "Queqiao-$ver-linux-x64.deb"; Mode = "644"; Optional = $true }
        )
    }

    $uploaded = @()
    foreach ($f in $files) {
        $local = Join-Path $outDir $f.Local
        if (-not (Test-Path $local)) {
            if ($f.Optional) {
                Write-Warn2 "跳过可选文件: $($f.Local)"
                continue
            }
            throw "缺少: $local"
        }
        $tmp = "/tmp/$($f.RemoteName)"
        Send-RemoteFile $local $tmp
        $uploaded += $f
    }

    $moveCmds = foreach ($f in $uploaded) {
        $tmp = "/tmp/$($f.RemoteName)"
        $dest = "$remoteDl/$($f.RemoteName)"
        "install -o rasite -g rasite -m $($f.Mode) '$tmp' '$dest'; rm -f '$tmp'"
    }
    if ($moveCmds) {
        Invoke-Ssh (($moveCmds -join "; "))
    }
}

function Get-RemoteLatestJson {
    $tmp = Join-Path $env:TEMP "queqiao-latest-remote.json"
    if ($DryRun) { return $null }
    try {
        & scp -o BatchMode=yes -o ConnectTimeout=15 "${SshTarget}:/var/lib/remote-assist-site/downloads/latest.json" $tmp 2>$null
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $tmp)) { return $null }
        return Get-Content -Raw -Encoding UTF8 $tmp | ConvertFrom-Json
    }
    catch { return $null }
}

function Get-SeedLatestJson {
    $example = Join-Path $root "docs\update-latest.example.json"
    if (-not (Test-Path $example)) { return $null }
    $raw = (Get-Content -Raw -Encoding UTF8 $example) -replace "YOUR_SITE_HOST", ($SiteBase -replace "^https?://", "")
    try { return $raw | ConvertFrom-Json } catch { return $null }
}

function ConvertTo-VersionTuple([string]$v) {
    $clean = ($v -replace '^v', '').Trim()
    $parts = $clean.Split('.')
    $maj = 0; $min = 0; $pat = 0
    if ($parts.Count -gt 0) { [void][int]::TryParse($parts[0], [ref]$maj) }
    if ($parts.Count -gt 1) { [void][int]::TryParse($parts[1], [ref]$min) }
    if ($parts.Count -gt 2) {
        $patchRaw = ($parts[2] -split '[^0-9]')[0]
        [void][int]::TryParse($patchRaw, [ref]$pat)
    }
    return ,@($maj, $min, $pat)
}

function Get-MaxVersion([string[]]$versions) {
    $best = $null
    $bestT = @(-1, -1, -1)
    foreach ($v in $versions) {
        if (-not $v) { continue }
        $t = ConvertTo-VersionTuple $v
        $greater = ($t[0] -gt $bestT[0]) -or
            (($t[0] -eq $bestT[0]) -and ($t[1] -gt $bestT[1])) -or
            (($t[0] -eq $bestT[0]) -and ($t[1] -eq $bestT[1]) -and ($t[2] -gt $bestT[2]))
        if ($greater) {
            $best = $v
            $bestT = $t
        }
    }
    return $best
}

function New-LatestJson([string]$ver, [string[]]$plats, [string]$outDir, $existing) {
    Write-Step "生成 latest.json（每平台独立 version）"
    if (-not $existing) {
        $existing = Get-SeedLatestJson
        if ($existing) { Write-Info "无远端清单，用 docs/update-latest.example.json 打底（保留各平台键）" }
    }

    $platforms = @{}
    if ($existing -and $existing.platforms) {
        foreach ($p in $existing.platforms.PSObject.Properties) {
            $entry = @{
                url  = [string]$p.Value.url
                name = [string]$p.Value.name
            }
            $pv = $null
            if ($p.Value.PSObject.Properties.Name -contains "version") {
                $pv = [string]$p.Value.version
            }
            if ($pv) { $entry.version = $pv }
            # 本轮没重出的平台，保留上一轮的校验值，否则客户端会拒绝自动更新。
            foreach ($field in @("sha256", "size")) {
                if ($p.Value.PSObject.Properties.Name -contains $field) {
                    $entry[$field] = $p.Value.$field
                }
            }
            if (-not $entry.sha256 -or $entry.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
                Write-Warn2 "平台 $($p.Name) 没有有效 sha256，客户端会拒绝该平台自动更新（本轮重出该平台即可补上）"
            }
            $platforms[$p.Name] = $entry
        }
    }

    function Set-Plat([string]$key, [string]$name) {
        $entry = @{
            version = $ver
            url     = "$SiteBase/downloads/$name"
            name    = $name
        }
        $file = Join-Path $outDir $name
        if (Test-Path $file) {
            $entry.sha256 = (Get-FileHash -Algorithm SHA256 $file).Hash.ToLower()
            $entry.size = (Get-Item $file).Length
        }
        else {
            Write-Warn2 "缺少 $name，清单不写 sha256；该平台自动更新会被客户端拒绝，需手动下载"
        }
        $platforms[$key] = $entry
    }

    if ($plats -contains "windows") { Set-Plat "windows-x86_64" "Queqiao-Setup-$ver.exe" }
    if ($plats -contains "macos") {
        Set-Plat "darwin-aarch64" "Queqiao-$ver-macos-arm64.dmg"
        Set-Plat "darwin-x86_64" "Queqiao-$ver-macos-x64.dmg"
    }
    if ($plats -contains "linux") { Set-Plat "linux-x86_64" "Queqiao-$ver-linux-x64.AppImage" }

    # 顶层 version：取各平台 version 的最大值（旧客户端回退）；-ForceLatestVersion 则强制用本轮 $ver
    $platVersions = @()
    foreach ($key in $platforms.Keys) {
        if ($platforms[$key].ContainsKey("version") -and $platforms[$key].version) {
            $platVersions += [string]$platforms[$key].version
        }
    }
    $manifestVer = Get-MaxVersion $platVersions
    if (-not $manifestVer) { $manifestVer = $ver }
    if ($ForceLatestVersion) { $manifestVer = $ver }
    Write-Info "顶层 version=$manifestVer（平台: $($platVersions -join ', ')）"

    $noteText = $Notes
    if (-not $noteText) {
        $noteText = "发布 $($plats -join '/') $ver；各平台版本见 platforms.*.version。"
    }

    $doc = [ordered]@{
        version   = $manifestVer
        notes     = $noteText
        pub_date  = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        platforms = $platforms
    }
    $jsonPath = Join-Path $outDir "latest.json"
    $json = $doc | ConvertTo-Json -Depth 6
    if ($DryRun) {
        Write-Info "[dry-run] latest.json:"
        Write-Host $json
    }
    else {
        Set-Content -Path $jsonPath -Value $json -Encoding UTF8
        Write-Info "写 $jsonPath"
    }
    return $jsonPath
}

function Publish-LatestJson([string]$jsonPath) {
    if (-not (Test-Path $jsonPath) -and -not $DryRun) { throw "无 latest.json: $jsonPath" }
    Send-RemoteFile $jsonPath "/tmp/latest.json"
    Invoke-Ssh "install -o rasite -g rasite -m 644 /tmp/latest.json /var/lib/remote-assist-site/downloads/latest.json; rm -f /tmp/latest.json"
}

function Publish-SiteBinary {
    Write-Step "编译并替换官网 site 二进制（含首页链接）"
    $outBin = Join-Path $root "artifacts\remote-assist-site"
    Ensure-Dir (Split-Path $outBin)
    $env:CGO_ENABLED = "0"
    $env:GOOS = "linux"
    $env:GOARCH = "amd64"
    Push-Location (Join-Path $root "site")
    try {
        if ($DryRun) { Write-Info "[dry-run] go build site"; return }
        & go build -o $outBin ./cmd/site
        if ($LASTEXITCODE -ne 0) { throw "go build site 失败" }
    }
    finally {
        Pop-Location
        Remove-Item Env:CGO_ENABLED -ErrorAction SilentlyContinue
        Remove-Item Env:GOOS -ErrorAction SilentlyContinue
        Remove-Item Env:GOARCH -ErrorAction SilentlyContinue
    }

    Send-RemoteFile $outBin "/tmp/remote-assist-site-new"
    $ts = Get-Date -Format "yyyyMMdd-HHmm"
    Invoke-Ssh @"
set -e
mkdir -p /var/backups/remote-assist-site
cp -a /usr/local/bin/remote-assist-site /var/backups/remote-assist-site/remote-assist-site.bak-$ts
install -o root -g root -m 755 /tmp/remote-assist-site-new /usr/local/bin/remote-assist-site
rm -f /tmp/remote-assist-site-new
systemctl restart remote-assist-site
systemctl is-active remote-assist-site
"@
    Write-Info "site 已重启；中继未触碰"
}

function Test-Public([string]$ver, [string[]]$plats) {
    Write-Step "公网验收 HEAD"
    $urls = @()
    if ($plats -contains "windows") { $urls += "$SiteBase/downloads/Queqiao-Setup-$ver.exe" }
    if ($plats -contains "macos") {
        $urls += "$SiteBase/downloads/Queqiao-$ver-macos-arm64.dmg"
        $urls += "$SiteBase/downloads/Queqiao-$ver-macos-x64.dmg"
    }
    if ($plats -contains "linux") {
        $urls += "$SiteBase/downloads/Queqiao-$ver-linux-x64.AppImage"
        $urls += "$SiteBase/downloads/Queqiao-MCP-$ver-linux-x64.tar.gz"
    }
    $urls += "$SiteBase/downloads/latest.json"
    foreach ($u in $urls) {
        if ($DryRun) { Write-Info "[dry-run] HEAD $u"; continue }
        try {
            $r = Invoke-WebRequest -Uri $u -Method Head -UseBasicParsing -TimeoutSec 20
            Write-Info "$($r.StatusCode) $u  ($($r.Headers['Content-Length']) bytes)"
        }
        catch {
            Write-Warn2 "失败 $u : $($_.Exception.Message)"
        }
    }
}

# --- main ---
if (-not $Platforms -or $Platforms.Count -eq 0) {
    $Platforms = Select-PlatformsInteractive
}
$Platforms = @(
    $Platforms |
        ForEach-Object { $_ -split "[,\s]+" } |
        Where-Object { $_ } |
        ForEach-Object { $_.Trim().ToLowerInvariant() }
)
$alias = @{ win = "windows"; mac = "macos"; osx = "macos" }
$Platforms = @(
    $Platforms | ForEach-Object {
        if ($alias.ContainsKey($_)) { $alias[$_] } else { $_ }
    } | Select-Object -Unique
)
foreach ($p in $Platforms) {
    if ($p -notin @("windows", "macos", "linux")) {
        throw "未知平台: $p（可选 windows / macos / linux）"
    }
}

$cargoVer = Get-CargoVersion
if (-not $Version) { $Version = $cargoVer }
elseif ($Version -ne $cargoVer) {
    Set-ProjectVersion $Version
}

if (-not $ArtifactsRoot) {
    $ArtifactsRoot = Join-Path $root "artifacts\release-$Version"
}
Ensure-Dir $ArtifactsRoot

Write-Step "计划"
Write-Info "版本: $Version"
Write-Info "平台: $($Platforms -join ', ')"
Write-Info "产物: $ArtifactsRoot"
Write-Info "SSH:  $SshTarget"
Write-Info "站点: $SiteBase"
if ($DryRun) { Write-Warn2 "DryRun：不写盘/不上传真实执行预览" }

Update-WorkflowFilenames $Version $Platforms
$siteChanged = Update-SiteIndex $Version $Platforms

if (-not $SkipBuild) {
    if ($Platforms -contains "windows") {
        Build-Windows $Version $ArtifactsRoot
    }
    if ($Platforms -contains "macos") {
        Write-Step "macOS"
        Write-Warn2 "本机脚本不交叉编译 macOS。请把 dmg 放到:`n      $ArtifactsRoot\Queqiao-$Version-macos-arm64.dmg`n      $ArtifactsRoot\Queqiao-$Version-macos-x64.dmg"
        Write-Info "或用 gh: gh workflow run `"Release packages (MCP)`" -f include_windows=false"
    }
    if ($Platforms -contains "linux") {
        Write-Step "Linux"
        Write-Warn2 "本机脚本不交叉编译 Linux UI。请把 AppImage / MCP tar 放到:`n      $ArtifactsRoot\Queqiao-$Version-linux-x64.AppImage`n      $ArtifactsRoot\Queqiao-MCP-$Version-linux-x64.tar.gz"
    }
}
else {
    Write-Step "跳过构建（-SkipBuild），使用已有产物"
}

Assert-PlatformArtifacts $Version $ArtifactsRoot $Platforms

$existingLatest = Get-RemoteLatestJson
$latestPath = New-LatestJson $Version $Platforms $ArtifactsRoot $existingLatest

if (-not $SkipUpload) {
    Publish-Downloads $Version $ArtifactsRoot $Platforms
    Publish-LatestJson $latestPath
}
else {
    Write-Step "跳过上传（-SkipUpload）"
    Write-Info "产物已在 $ArtifactsRoot ，可手动 scp 或 ssh-connect 上传"
}

if (-not $SkipSite -and $siteChanged) {
    Publish-SiteBinary
}
elseif ($SkipSite) {
    Write-Step "跳过官网二进制（-SkipSite）。若改了 index.html，请稍后自行部署 site。"
}
elseif (-not $siteChanged) {
    Write-Info "首页链接无变化，跳过 site 二进制替换"
}

if (-not $SkipUpload) {
    Test-Public $Version $Platforms
}

Write-Step "完成"
Write-Info "本地产物: $ArtifactsRoot"
if (-not $SkipUpload) {
    Write-Info "下载例: $SiteBase/downloads/…"
}
Write-Host ""
