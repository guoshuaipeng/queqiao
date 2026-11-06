<#
.SYNOPSIS
  Generate installer/assets/app.ico (and App project copy) for PV-003 brand icon.
.EXAMPLE
  .\scripts\generate-app-icon.ps1
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing

function New-BrandBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $pad = [Math]::Max(1, [int]($size * 0.06))
    $diam = $size - 2 * $pad
    $blue = [System.Drawing.Color]::FromArgb(0x1A, 0x73, 0xE8)
    $green = [System.Drawing.Color]::FromArgb(0x0F, 0x9D, 0x58)
    $brush = New-Object System.Drawing.SolidBrush $blue
    $g.FillEllipse($brush, $pad, $pad, $diam, $diam)
    $brush.Dispose()
    $penW = [Math]::Max(1.5, $size / 12.0)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $penW
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $m = $size * 0.22
    $g.DrawArc($pen, $m, $m * 0.85, $size - 2 * $m, $size - 2 * $m, 200, 140)
    $yDeck = $size * 0.58
    $g.DrawLine($pen, $size * 0.28, $yDeck, $size * 0.72, $yDeck)
    $g.DrawLine($pen, $size * 0.34, $yDeck, $size * 0.34, $size * 0.72)
    $g.DrawLine($pen, $size * 0.66, $yDeck, $size * 0.66, $size * 0.72)
    $pen.Dispose()
    $dot = [Math]::Max(2, [int]($size * 0.14))
    $gb = New-Object System.Drawing.SolidBrush $green
    $g.FillEllipse($gb, [int]($size * 0.62), [int]($size * 0.22), $dot, $dot)
    $gb.Dispose()
    $g.Dispose()
    return $bmp
}

function Write-IcoFile([string]$path, [int[]]$sizes) {
    $images = @()
    foreach ($s in $sizes) {
        $bmp = New-BrandBitmap $s
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $ms.ToArray()
        $ms.Dispose(); $bmp.Dispose()
        $images += ,[pscustomobject]@{ Size = $s; Bytes = $pngBytes }
    }
    $fs = [System.IO.File]::Create($path)
    $bw = New-Object System.IO.BinaryWriter $fs
    $bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$images.Count)
    $offset = 6 + (16 * $images.Count)
    foreach ($img in $images) {
        $w = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
        $bw.Write([byte]$w); $bw.Write([byte]$w); $bw.Write([byte]0); $bw.Write([byte]0)
        $bw.Write([uint16]1); $bw.Write([uint16]32)
        $bw.Write([uint32]$img.Bytes.Length); $bw.Write([uint32]$offset)
        $offset += $img.Bytes.Length
    }
    foreach ($img in $images) { $bw.Write($img.Bytes) }
    $bw.Close(); $fs.Close()
}

$assetsDir = Join-Path $root "installer\assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
$icoPath = Join-Path $assetsDir "app.ico"
Write-IcoFile $icoPath @(16, 32, 48, 64, 128, 256)
$appRes = Join-Path $root "src-net48\RemoteAssist.App\assets"
New-Item -ItemType Directory -Force -Path $appRes | Out-Null
Copy-Item $icoPath (Join-Path $appRes "app.ico") -Force
Write-Host "OK: $icoPath ($((Get-Item $icoPath).Length) bytes)"
