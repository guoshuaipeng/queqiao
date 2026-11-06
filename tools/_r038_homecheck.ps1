$html = [IO.File]::ReadAllText('C:\Users\user\Downloads\_r038_home.html', [Text.UTF8Encoding]::new($false))
$checks = @(
  '鹊桥帮你连上对方的电脑：查进程、看日志、读文件、在对方确认后执行命令。Windows 和 Linux 都可下载使用',
  '若浏览器提示通常不会下载，选择保留即可。',
  '解开后运行。需要先装好 .NET 8 桌面方向运行时。打开',
  'https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0',
  '选运行时、不要选 SDK。',
  '即将提供'
)
foreach($c in $checks){
  $ok = $html.Contains($c)
  Write-Output ("FOUND={0} :: {1}" -f $ok, $c)
}
$forbidden = @('17891','放行入站','也可下载自包含包','TLS','stdio','证书','签名','SmartScreen')
foreach($f in $forbidden){
  $bad = $html.Contains($f)
  Write-Output ("FORBIDDEN={0} present={1}" -f $f, $bad)
}
# also print download section snippet
$idx = $html.IndexOf('download')
if($idx -lt 0){ $idx = $html.IndexOf('下载') }
Write-Output '==== HOME SNIPPET ===='
Write-Output $html
