; RemoteAssist Agent — Inno Setup 6 script
; 构建：先 publish Agent，再 ISCC 本脚本。见 scripts\build-installer.ps1

#define MyAppName "远端协助"
#define MyAppNameEn "RemoteAssist"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "RemoteAssist"
#define MyAppExeName "RemoteAssist.Agent.exe"
#ifndef AgentSource
  #define AgentSource "..\artifacts\agent-win-x64-sc"
#endif

[Setup]
AppId={{A8F3C2E1-9B47-4D6A-8F21-REMOTEASSIST01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; 默认装到当前用户 LocalAppData，无需管理员（亲友友好）
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts\installer
OutputBaseFilename=RemoteAssist-Agent-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
; 中文向导（系统有中文语言包时）
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"; Flags: unchecked
Name: "autostart"; Description: "登录时启动（托盘常驻）"; GroupDescription: "附加选项:"; Flags: unchecked

[Files]
; publish 出的自包含 Agent（至少 exe；同目录其它文件一并带走）
Source: "{#AgentSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; 可选开机自启
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "RemoteAssistAgent"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即打开 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\RemoteAssist\audit.log"
; 保留审计目录本身；仅提示性清理主日志（可按需调整）

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

