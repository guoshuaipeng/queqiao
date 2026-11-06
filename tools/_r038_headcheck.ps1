$urls = @(
  'http://121.196.45.81/downloads/RemoteAssist-Setup-0.1.0.exe',
  'http://121.196.45.81/downloads/queqiao-linux-x64.tar.gz'
)
foreach($u in $urls){
  $r = Invoke-WebRequest -Uri $u -Method Head -UseBasicParsing
  Write-Output ("URL={0}" -f $u)
  Write-Output ("Status={0}" -f [int]$r.StatusCode)
  Write-Output ("Content-Length={0}" -f $r.Headers['Content-Length'])
  Write-Output ("Content-Type={0}" -f $r.Headers['Content-Type'])
  Write-Output ("Content-Disposition={0}" -f $r.Headers['Content-Disposition'])
  Write-Output ("Last-Modified={0}" -f $r.Headers['Last-Modified'])
  Write-Output '---'
}
foreach($p in @('/','/ask','/admin')){
  $r = Invoke-WebRequest -Uri ("http://121.196.45.81{0}" -f $p) -Method Head -UseBasicParsing
  Write-Output ("PATH={0} Status={1}" -f $p, [int]$r.StatusCode)
}
$html = (Invoke-WebRequest -Uri 'http://121.196.45.81/' -UseBasicParsing).Content
$out = 'C:\Users\user\Downloads\_r038_home.html'
[IO.File]::WriteAllText($out, $html, [Text.UTF8Encoding]::new($false))
Write-Output ("HOME_BYTES={0}" -f $html.Length)
Write-Output ("HOME_SAVED={0}" -f $out)
