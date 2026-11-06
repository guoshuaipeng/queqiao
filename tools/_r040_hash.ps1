$ErrorActionPreference='Stop'
$win='E:\IdeaProjects\remote-assist\artifacts\installer\RemoteAssist-Setup-0.1.0.exe'
$lin='E:\IdeaProjects\remote-assist\artifacts\queqiao-linux-x64-fd.tar.gz'
foreach($p in @($win,$lin)){
  $i=Get-Item $p
  $h=(Get-FileHash $p -Algorithm SHA256).Hash
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
Write-Output ('HEAD='+(git rev-parse HEAD))
git merge-base --is-ancestor 49a4292727b01e615e16a0a95835ec491410ffaa HEAD
Write-Output ('ancestor49a429='+$LASTEXITCODE)
git merge-base --is-ancestor 5c276ec HEAD
Write-Output ('ancestor5c276ec='+$LASTEXITCODE)
git log -1 --oneline 49a4292727b01e615e16a0a95835ec491410ffaa
