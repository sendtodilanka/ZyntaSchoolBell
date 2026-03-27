; ZyntaSchoolBell Inno Setup Script
; Per-user installation - no admin rights required

#define MyAppName "ZyntaSchoolBell"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ZyntaSchoolBell"
#define MyAppExeName "ZyntaSchoolBell.exe"

[Setup]
AppId={{B7E3F2A1-9C4D-4E5F-A6B8-1234567890AB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=ZyntaSchoolBell-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Main application files
Source: "..\src\ZyntaSchoolBell\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Audio files
Source: "..\audio\*"; DestDir: "{app}\audio"; Flags: ignoreversion recursesubdirs createallsubdirs

; Default profiles - only install if not already present (preserve user edits)
Source: "default_profiles\regular_day.json"; DestDir: "{userappdata}\{#MyAppName}\profiles"; Flags: onlyifdoesntexist
Source: "default_profiles\exam_day.json"; DestDir: "{userappdata}\{#MyAppName}\profiles"; Flags: onlyifdoesntexist
Source: "default_profiles\half_day.json"; DestDir: "{userappdata}\{#MyAppName}\profiles"; Flags: onlyifdoesntexist
Source: "default_profiles\saturday_school.json"; DestDir: "{userappdata}\{#MyAppName}\profiles"; Flags: onlyifdoesntexist
Source: "default_profiles\special_day.json"; DestDir: "{userappdata}\{#MyAppName}\profiles"; Flags: onlyifdoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
Name: "autostart"; Description: "Start {#MyAppName} when Windows starts"; GroupDescription: "Startup:"; Flags: checkedonce

[Registry]
; Auto-start entry (only if user selected autostart task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Dirs]
; Create profile and log directories
Name: "{userappdata}\{#MyAppName}\profiles"
Name: "{userappdata}\{#MyAppName}\logs"

[UninstallDelete]
; Clean up log files on uninstall, but preserve profiles
Type: filesandordirs; Name: "{userappdata}\{#MyAppName}\logs"
; Note: We intentionally do NOT delete {userappdata}\{#MyAppName}\profiles
; to preserve user customizations across reinstalls
