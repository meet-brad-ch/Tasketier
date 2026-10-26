; The Tasketier installer: the tray app (TasketierApp.exe) and the CLI/MCP server (tasketier.exe) in one folder.
; Inno Setup 6.3 or later compiles it. The project uses Inno Setup 7.0.
;
;   dotnet publish src\Tasketier.App\Tasketier.App.csproj -c Release -r win-x64 --self-contained false -o out\publish
;   dotnet publish src\Tasketier.Cli\Tasketier.Cli.csproj -c Release -r win-x64 --self-contained false -o out\publish
;   iscc /DAppVersion=0.1.0 /DNumVersion=0.1.0.0 installer\Tasketier.iss      -> out\installer\Tasketier-Setup-0.1.0.exe
;
; Both exes are published without the runtime. They need the .NET Desktop Runtime 10.0 (the CLI needs only the
; base runtime, which the Desktop Runtime includes). When no 10.0.x Desktop Runtime is installed, the setup
; downloads Microsoft's installer (57 MB, SHA-256 checked) and runs it quietly before it copies the app files.
; The setup downloads nothing else.
;
; The install folder is added to the user's PATH so `tasketier` works in any shell, and the Claude Code plugin's
; .mcp.json finds the server at %ProgramFiles%\Tasketier\tasketier.exe.
;
; The uninstaller removes the program files and the PATH entry; the tasks and settings in %LOCALAPPDATA%\Tasketier
; stay, they are the user's data.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
#ifndef NumVersion
  #define NumVersion "0.0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\out\publish"
#endif

#define RuntimeVersion "10.0.14"
#define RuntimeName "windowsdesktop-runtime-10.0.14-win-x64.exe"
#define RuntimeUrl "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.14/windowsdesktop-runtime-10.0.14-win-x64.exe"
; SHA-256 of that file, computed 2026-10-26 from a download whose SHA-512 matched Microsoft's 10.0.14-sha.txt.
#define RuntimeHash "91c2795a7b1e2480caa83fce9514dc2e2d21e277d9a297311faba6040bad7330"

[Setup]
AppId={{9C2B7E8A-5D41-4F0B-9B6E-3A1C2D7F8E10}
AppName=Tasketier
AppVersion={#AppVersion}
AppVerName=Tasketier {#AppVersion}
AppPublisher=Brad Chamberlain
VersionInfoVersion={#NumVersion}
DefaultDirName={autopf}\Tasketier
DefaultGroupName=Tasketier
DisableProgramGroupPage=yes
; the .NET runtime installs under Program Files, which needs elevation
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; .NET 10 runs on Windows 10 1607 and later
MinVersion=10.0.14393
OutputDir=..\out\installer
OutputBaseFilename=Tasketier-Setup-{#AppVersion}
SetupIconFile=..\src\Tasketier.App\Tasketier.ico
UninstallDisplayIcon={app}\TasketierApp.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ChangesEnvironment=yes

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: autostart; Description: "Start Tasketier with Windows (the tray icon and the hotkeys)"; GroupDescription: "Start-up:"

[Registry]
; the same per-user Run value the Settings dialog writes; --tray shows only the icon
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Tasketier"; ValueData: """{app}\TasketierApp.exe"" --tray"; Flags: uninsdeletevalue; Tasks: autostart
; the CLI on the machine PATH, once
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Check: NotOnPath(ExpandConstant('{app}'))

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Tasketier"; Filename: "{app}\TasketierApp.exe"
Name: "{autoprograms}\Tasketier – add a task"; Filename: "{app}\TasketierApp.exe"; Parameters: "--capture"
Name: "{autodesktop}\Tasketier"; Filename: "{app}\TasketierApp.exe"; Tasks: desktopicon

[Run]
Filename: "{tmp}\{#RuntimeName}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing the .NET Desktop Runtime {#RuntimeVersion}..."; Flags: waituntilterminated; Check: RuntimeDownloaded
Filename: "{app}\TasketierApp.exe"; Parameters: "--tray"; Description: "Start Tasketier (in the tray)"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
var
  DownloadPage: TDownloadWizardPage;
  RuntimeWasDownloaded: Boolean;

{ A .NET Desktop Runtime 10.0.x is installed: the shared framework folder has a 10.0.* subfolder. }
function RuntimeMissing: Boolean;
var
  Found: TFindRec;
  Base: String;
begin
  Result := True;
  Base := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\');
  if FindFirst(Base + '10.0.*', Found) then
  try
    repeat
      if (Found.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
      begin
        Result := False;
        Exit;
      end;
    until not FindNext(Found);
  finally
    FindClose(Found);
  end;
end;

{ The folder is not yet on the machine PATH (compared case-insensitively, as whole entries). }
function NotOnPath(Folder: String): Boolean;
var
  Path: String;
begin
  Result := True;
  if not RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then Exit;
  Result := Pos(';' + Uppercase(Folder) + ';', ';' + Uppercase(Path) + ';') = 0;
end;

{ On uninstall: take the folder out of the machine PATH again. }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Path, Folder: String;
  P: Integer;
begin
  if CurUninstallStep <> usPostUninstall then Exit;
  if not RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then Exit;
  Folder := ExpandConstant('{app}');
  P := Pos(';' + Uppercase(Folder), Uppercase(Path));
  if P > 0 then
  begin
    Delete(Path, P, Length(Folder) + 1);
    RegWriteExpandStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path);
  end;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := MemoDirInfo + NewLine + NewLine;
  if RuntimeMissing then
    Result := Result + '.NET Desktop Runtime {#RuntimeVersion}:' + NewLine + Space +
              'not installed; it will be downloaded from Microsoft (57 MB) and installed' + NewLine + NewLine
  else
    Result := Result + '.NET Desktop Runtime 10.0:' + NewLine + Space + 'already installed' + NewLine + NewLine;
  if MemoTasksInfo <> '' then
    Result := Result + MemoTasksInfo + NewLine;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID <> wpReady then Exit;
  RuntimeWasDownloaded := False;
  if not RuntimeMissing then Exit;

  DownloadPage.Clear;
  DownloadPage.Add('{#RuntimeUrl}', '{#RuntimeName}', '{#RuntimeHash}');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;   { checks the SHA-256 }
      RuntimeWasDownloaded := True;
    except
      if DownloadPage.AbortedByUser then
        Log('The runtime download was cancelled.')
      else
        SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  finally
    DownloadPage.Hide;
  end;
end;

function RuntimeDownloaded: Boolean;
begin
  Result := RuntimeWasDownloaded and FileExists(ExpandConstant('{tmp}\{#RuntimeName}'));
end;
