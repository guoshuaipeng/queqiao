using RemoteAssist.Protocol;
var args = new[]{"--code","38U8P8","--pwd","V6UM","--perm","Assist","--insecure","procs"};
var ok = AssistConnectOptions.TryParse(args, _ => null, out var opts, out var err);
Console.WriteLine($"ok={ok} err={err}");
if (ok) Console.WriteLine($"via={opts.ViaRelay} code={opts.Code} pwd={opts.Password} insecure={opts.Insecure} relay={opts.RelayEndpoint}");
