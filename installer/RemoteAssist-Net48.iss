; RemoteAssist net48 — Inno Setup 6（亲友主路径：FW4.8 依赖小包）
; 构建：.\scripts\build-installer-net48.ps1
; 兼容备用：installer\RemoteAssist.iss（net8 自包含）勿作主推

#define MyAppName "鹊桥远程协助"
#define MyAppNameEn "RemoteAssist"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "RemoteAssist"
#define MyAppExeName "RemoteAssist.Net48.exe"
#ifndef AppSource
  #define AppSource "..\artifacts\app-net48-fd"
#endif

[Setup]
AppId={{B7E4D3C2-8A51-4F7B-9C32-REMOTEASSIST48}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion} (net48)
AppPublisher={#MyAppPublisher}
AppComments=需要本机已安装 .NET Framework 4.8（非自包含包）
DefaultDirName={localappdata}\Programs\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts\installer
OutputBaseFilename=RemoteAssist-Net48-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=no
InfoBeforeFile=net48-runtime-note.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"; Flags: unchecked
Name: "autostart"; Description: "登录时启动（进入后可选被控/控制）"; GroupDescription: "附加选项:"; Flags: unchecked

[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "net48-runtime-note.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "RemoteAssist"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即打开 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\RemoteAssist\audit.log"
