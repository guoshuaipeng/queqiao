$ErrorActionPreference='Stop'
$win='E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe'
$lin='E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz'
foreach ($p in @($win,$lin)) {
  $i=Get-Item -LiteralPath $p
  $h=(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
  $fs=[IO.File]::OpenRead($p); $b=New-Object byte[] 2; [void]$fs.Read($b,0,2); $fs.Close()
  $magic=('{0:X2} {1:X2}' -f $b[0],$b[1])
  Write-Output ('PATH='+$i.FullName)
  Write-Output ('SIZE='+$i.Length)
  Write-Output ('SHA256='+$h)
  Write-Output ('MTIME='+$i.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
  Write-Output ('MAGIC='+$magic)
  Write-Output '---'
}
Set-Location 'E:\IdeaProjects\remote-assist'
Write-Output ('REPO_HEAD='+(git rev-parse HEAD))
git merge-base --is-ancestor 8d61398d6393015e96bb721c4be344e072b0ddb9 HEAD | Out-Null
Write-Output ('ANCESTOR_8d61398_EXIT='+$LASTEXITCODE)
git merge-base --is-ancestor aeafeeb HEAD | Out-Null
Write-Output ('ANCESTOR_aeafeeb_EXIT='+$LASTEXITCODE)
git merge-base --is-ancestor 5c276ec HEAD | Out-Null
Write-Output ('ANCESTOR_5c276ec_EXIT='+$LASTEXITCODE)
