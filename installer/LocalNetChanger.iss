; LocalNetChanger kurulum paketi - Inno Setup 6

#define MyAppName "LocalNetChanger"
#define MyAppVersion "1.0.8"
#define MyAppPublisher "LocalNetChanger"
#define MyAppURL "https://www.gnu.org/licenses/gpl-3.0.html"
#define MyAppExeName "LocalNetChanger.exe"
#define OpenSettingsArg "--open-settings"
#define TrayOnlyArg "--tray-only"
#define PublishDir "..\publish\win-x64"

[Setup]
AppId={{B8E4F2A1-9C3D-4E5F-A1B2-3C4D5E6F7081}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=license.txt
InfoBeforeFile=info.txt
SetupIconFile=..\LocalNetChanger\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\dist
OutputBaseFilename=LocalNetChanger-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur (ayar paneli ile açılır)"; GroupDescription: "Ek kısayollar:"; Flags: checkedonce
Name: "startupicon"; Description: "Windows başlangıcında çalıştır (sistem tepsisinde)"; GroupDescription: "Ek kısayollar:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "license.txt"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
Source: "info.txt"; DestDir: "{app}"; DestName: "README-KURULUM.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "{#OpenSettingsArg}"
Name: "{group}\Lisans (GPL v3)"; Filename: "{app}\LICENSE.txt"
Name: "{group}\{#MyAppName} Kaldır"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "{#OpenSettingsArg}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "{#TrayOnlyArg}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "{#OpenSettingsArg}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\LocalNetChanger"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    { Profiller kullanıcı AppData'da kalır; kurulum dizini yalnızca uygulama dosyaları. }
  end;
end;
