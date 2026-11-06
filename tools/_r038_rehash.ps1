$ErrorActionPreference='Stop'
$win='E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe'
$lin='E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz'
foreach($p in @($win,$lin)){
  if(-not (Test-Path $p)){ Write-Output "MISSING $p"; continue }
  $i=Get-Item $p
  $h=(Get-FileHash $p -Algorithm SHA256).Hash
  $fs=[IO.File]::OpenRead($p); $b=New-Object byte[] 2; [void]$fs.Read($b,0,2); $fs.Close()
  $magic=('{0:X2} {1:X2}' -f $b[0],$b[1])
  Write-Output ("PATH={0}" -f $p)
  Write-Output ("SIZE={0}" -f $i.Length)
  Write-Output ("SHA256={0}" -f $h)
  Write-Output ("MTIME={0}" -f $i.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))
  Write-Output ("MAGIC={0}" -f $magic)
  Write-Output '---'
}
Set-Location 'E:\IdeaProjects\remote-assist'
Write-Output 'REPO_HEAD:'
git rev-parse HEAD
git log -1 --oneline
git merge-base --is-ancestor 0405491 HEAD; Write-Output ("has_R037_0405491_exit={0}" -f $LASTEXITCODE)
git merge-base --is-ancestor d24fb44 HEAD; Write-Output ("has_R036_d24fb44_exit={0}" -f $LASTEXITCODE)
git merge-base --is-ancestor b707804 HEAD; Write-Output ("has_R035_b707804_exit={0}" -f $LASTEXITCODE)
git merge-base --is-ancestor 0efc3c4 HEAD; Write-Output ("has_0efc3c4_exit={0}" -f $LASTEXITCODE)
