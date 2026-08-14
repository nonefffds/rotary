#define MyAppName "Rotary"
#define MyAppVersion "1.0.2"
#define MyAppExeName "RotaryMonitor.exe"

[Setup]
AppId={{8C1F6C2A-0F2B-4F5D-9C3E-4B7A5C8D1E2F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=nonefffds
AppPublisherURL=https://github.com/nonefffds/rotary
DefaultDirName={localappdata}\Programs\Rotary
DefaultGroupName=Rotary
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=RotarySetup
SetupIconFile=..\dist\Assets\rotary.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\RotaryMonitor.exe
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Rotary"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall Rotary"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Rotary"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
