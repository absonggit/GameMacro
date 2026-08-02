#ifndef MyAppVersion
  #define MyAppVersion "1.0.2"
#endif

#define MyAppName "按键助手"
#define MyAppExeName "GameMacro.App.exe"

[Setup]
AppId={{7A054983-3708-4C13-B76E-352A3719B160}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=GameMacro
DefaultDirName={localappdata}\Programs\GameMacro
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=GameMacro-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
UsedUserAreasWarning=no
SetupIconFile=..\src\GameMacro.App\Assets\AppIcon.ico

[Languages]
Name: "chinesesimp"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："; Flags: unchecked

[Files]
Source: "..\artifacts\win-x64-installer-source\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动{#MyAppName}"; Flags: nowait postinstall skipifsilent
