; LinkRouter Inno Setup Script
; Build with: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" LinkRouterSetup.iss

#define MyAppName "LinkRouter"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "LinkRouter"
#define MyAppURL "https://github.com/user/LinkRouter"
#define MyAppExeName "LinkRouter.exe"
#define MyAppDescription "Browser selection and URL routing utility"

[Setup]
; NOTE: Generate a new GUID for AppId when releasing a different application
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Install location (Program Files for all users)
DefaultDirName={commonpf64}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output settings
OutputDir=..\bin\Installer
OutputBaseFilename=LinkRouterSetup-{#MyAppVersion}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; Windows version requirement (Windows 10+)
MinVersion=10.0

; Privileges (admin required, install for all users)
PrivilegesRequired=admin

; Appearance
WizardStyle=modern
WizardSizePercent=100

; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Close running application before install/uninstall
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"
Name: "registerasbrowser"; Description: "Register as a browser handler"; GroupDescription: "Browser Registration:"

[Files]
; Main executable (single-file from publish output)
Source: "..\bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; Tasks: startmenuicon

; Desktop shortcut
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; ========================
; BROWSER REGISTRATION
; ========================

; Register in StartMenuInternet (for browser list)
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\{#MyAppName}"; ValueType: string; ValueData: "{#MyAppDescription}"; Flags: uninsdeletekey; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\{#MyAppName}\DefaultIcon"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\{#MyAppName}\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: registerasbrowser

; Register application
Root: HKCU; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueData: "{#MyAppDescription}"; Flags: uninsdeletekey; Tasks: registerasbrowser

; Register Capabilities (required for Windows Default Apps list)
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#MyAppName}"; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "{#MyAppDescription}"; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities"; ValueType: string; ValueName: "ApplicationIcon"; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: registerasbrowser

; URL Associations
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities\URLAssociations"; ValueType: string; ValueName: "http"; ValueData: "{#MyAppName}"; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities\URLAssociations"; ValueType: string; ValueName: "https"; ValueData: "{#MyAppName}"; Tasks: registerasbrowser

; File Associations
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".htm"; ValueData: "{#MyAppName}"; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".html"; ValueData: "{#MyAppName}"; Tasks: registerasbrowser

; StartMenu entry (required for browser list)
Root: HKCU; Subkey: "Software\{#MyAppName}\Capabilities\Startmenu"; ValueType: string; ValueName: "StartMenuInternet"; ValueData: "{#MyAppName}"; Tasks: registerasbrowser

; Register in RegisteredApplications (makes app appear in Default Apps)
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: "Software\{#MyAppName}\Capabilities"; Flags: uninsdeletevalue; Tasks: registerasbrowser

; URL Protocol handler class
Root: HKCU; Subkey: "Software\Classes\{#MyAppName}"; ValueType: string; ValueData: "URL:http"; Flags: uninsdeletekey; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\Classes\{#MyAppName}"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\Classes\{#MyAppName}\DefaultIcon"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: registerasbrowser
Root: HKCU; Subkey: "Software\Classes\{#MyAppName}\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: registerasbrowser

[Run]
; Launch app after installation (optional)
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up user data directory on uninstall
Type: filesandordirs; Name: "{userappdata}\LinkRouter"

[Code]
// Force kill running instance before uninstall
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Kill any running instance of the application
  Exec('taskkill', '/F /IM LinkRouter.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(500); // Wait for process to fully terminate
  Result := True;
end;

// Clean up after uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Delete user data folder completely
    DelTree(ExpandConstant('{userappdata}\LinkRouter'), True, True, True);

    // Clean up all registry keys (in case automatic cleanup missed any)
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\LinkRouter');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Clients\StartMenuInternet\LinkRouter');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\LinkRouter');
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\RegisteredApplications', 'LinkRouter');
  end;
end;

// Notify Windows shell after installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Notify shell of association changes
    // This helps Windows recognize the new browser registration
  end;
end;
