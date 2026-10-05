#define MyAppName "HyperX Battery Monitor"
#ifndef MyAppVersion
  #error MyAppVersion must be supplied by Release\Build-Release.ps1
#endif
#define MyAppPublisher "Dave Santana"
#define MyAppExeName "HyperX Battery Monitor.exe"
#define MyAppUserModelId "DaveSantana.HyperXBatteryMonitor"
#define CleanInstallTaskName "cleaninstall"
#define CleanInstallTaskDescription "Perform a clean installation (delete existing settings and battery history)"
#define CleanInstallTaskGroup "Installation options:"
#define ResetUserDataArgument "--reset-user-data"
#define CleanInstallErrorMessage "Could not complete the clean installation. Existing user data may not have been fully removed."

[Setup]
AppId={{8D7E0A6C-9D5B-4F0B-9C3E-5F5E9C2A1B71}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\Releases
OutputBaseFilename=HyperXBatteryMonitor-Setup-v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin

[Files]
Source: "..\bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; AppUserModelID: "{#MyAppUserModelId}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; AppUserModelID: "{#MyAppUserModelId}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "{#CleanInstallTaskName}"; Description: "{#CleanInstallTaskDescription}"; GroupDescription: "{#CleanInstallTaskGroup}"; Flags: unchecked checkedonce

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep = ssPostInstall) and
     WizardIsTaskSelected('{#CleanInstallTaskName}') then
  begin
    if (not ExecAsOriginalUser(
          ExpandConstant('{app}\{#MyAppExeName}'),
          '{#ResetUserDataArgument}',
          ExpandConstant('{app}'),
          SW_HIDE,
          ewWaitUntilTerminated,
          ResultCode)) or
       (ResultCode <> 0) then
    begin
      RaiseException('{#CleanInstallErrorMessage}');
    end;
  end;
end;
