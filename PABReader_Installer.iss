[Setup]
AppName=PAB Reader
AppVersion=1.2.0
AppPublisher=Shekel Scales 2008 LTD
AppPublisherURL=https://www.shekelscales.com
AppContact=Claudio Kudlach
DefaultDirName={autopf}\PAB Reader
DefaultGroupName=PAB Reader
UninstallDisplayIcon={app}\PABReader.exe
Compression=lzma2
SolidCompression=yes
OutputDir=.\Installer_Output
OutputBaseFilename=PABReader_Setup_v1.2.0
SetupIconFile=Shekel.ico
WizardImageFile=compiler:WizModernImage-IS.bmp
WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
; Main executable and all required DLLs
Source: "bin\Release\SingleFile\PABReader.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\SingleFile\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\SingleFile\PABReader.pdb"; DestDir: "{app}"; Flags: ignoreversion

; Additional files if needed
Source: "Shekel.ico"; DestDir: "{app}"; Flags: ignoreversion

; Documentation (if you have any)
; Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion
; Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\PAB Reader"; Filename: "{app}\PABReader.exe"; IconFilename: "{app}\Shekel.ico"
Name: "{group}\{cm:UninstallProgram,PAB Reader}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\PAB Reader"; Filename: "{app}\PABReader.exe"; IconFilename: "{app}\Shekel.ico"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\PAB Reader"; Filename: "{app}\PABReader.exe"; Tasks: quicklaunchicon

[Registry]
; Register file associations if needed
; Root: HKCR; Subkey: ".pab"; ValueType: string; ValueName: ""; ValueData: "PABReaderFile"
; Root: HKCR; Subkey: "PABReaderFile"; ValueType: string; ValueName: ""; ValueData: "PAB Reader File"
; Root: HKCR; Subkey: "PABReaderFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\PABReader.exe,0"
; Root: HKCR; Subkey: "PABReaderFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\PABReader.exe"" ""%1"""

[Run]
Filename: "{app}\PABReader.exe"; Description: "{cm:LaunchProgram,PAB Reader}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := 0;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;