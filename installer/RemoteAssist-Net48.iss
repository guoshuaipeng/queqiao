; RemoteAssist net48 — Inno Setup 6（亲友主路径：FW4.8 依赖小包）
; 构建：.\scripts\build-installer-net48.ps1
; 兼容备用：installer\RemoteAssist.iss（net8 自包含）勿作主推
; PV-004：简体中文 + English；uilanguage；不弹选语言框；english 第一条作未支持语种保底

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
AppName={cm:AppDisplayName}
AppVersion={#MyAppVersion}
AppVerName={cm:AppDisplayName} {#MyAppVersion} (net48)
AppPublisher={#MyAppPublisher}
AppComments={cm:AppComments}
DefaultDirName={localappdata}\Programs\{#MyAppNameEn}
DefaultGroupName={cm:AppDisplayName}
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
LanguageDetectionMethod=uilanguage
UsePreviousLanguage=no
; 全局 InfoBefore 由各 [Languages] 条目覆盖；勿再设单一中文文件以免英中混搭

[Languages]
; english 必须第一条：未匹配系统显示语言时保底英文（ShowLanguageDialog=no）
Name: "english"; MessagesFile: "compiler:Default.isl"; InfoBeforeFile: "net48-runtime-note.en.txt"
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"; InfoBeforeFile: "net48-runtime-note.zh-CN.txt"

[CustomMessages]
; --- English ---
english.AppDisplayName=QueQiao Remote Assist
english.AppComments=Requires .NET Framework 4.8 already installed (not self-contained)
english.TaskDesktopIcon=Create a desktop shortcut
english.TaskAdditionalIcons=Additional options:
english.TaskAutoStart=Start when I sign in (then choose Accept assist / Assist others)
english.IconUninstall=Uninstall QueQiao Remote Assist
english.LaunchAfterInstall=Open QueQiao Remote Assist now
; --- Simplified Chinese ---
chinesesimplified.AppDisplayName=鹊桥远程协助
chinesesimplified.AppComments=需要本机已安装 .NET Framework 4.8（非自包含包）
chinesesimplified.TaskDesktopIcon=创建桌面快捷方式
chinesesimplified.TaskAdditionalIcons=附加选项:
chinesesimplified.TaskAutoStart=登录时启动（进入后可选被控/控制）
chinesesimplified.IconUninstall=卸载鹊桥远程协助
chinesesimplified.LaunchAfterInstall=立即打开鹊桥远程协助

[Tasks]
Name: "desktopicon"; Description: "{cm:TaskDesktopIcon}"; GroupDescription: "{cm:TaskAdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:TaskAutoStart}"; GroupDescription: "{cm:TaskAdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 安装目录旁注：随所选语言写入对应说明（同名目标文件）
Source: "net48-runtime-note.en.txt"; DestDir: "{app}"; DestName: "net48-runtime-note.txt"; Languages: english; Flags: ignoreversion
Source: "net48-runtime-note.zh-CN.txt"; DestDir: "{app}"; DestName: "net48-runtime-note.txt"; Languages: chinesesimplified; Flags: ignoreversion

[Icons]
Name: "{group}\{cm:AppDisplayName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{group}\{cm:IconUninstall}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{cm:AppDisplayName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "RemoteAssist"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchAfterInstall}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\RemoteAssist\audit.log"