; This is an Inno Setup configuration file
; http://www.jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\UIMF_Viewer\bin\Release\UIMF_Viewer.exe')
#if ApplicationVersion == ""
     #error UIMF_Viewer.exe was not found in the Release directory; rebuild the app in Release mode
#endif

[CustomMessages]
AppName=UIMF_Viewer
[Messages]
; WelcomeLabel2 is set using the code section
;WelcomeLabel2=This will install [name/ver] on your computer.%n%nUIMF_Viewer is a standalone Windows graphical user interface tool for viewing IMS and SLIM data in UIMF files.

[Setup]
; As AnyCPU, we can install as 32-bit or 64-bit, so allow installing on 32-bit Windows, but make sure it installs as 64-bit on 64-bit Windows
ArchitecturesAllowed=x64 x86
ArchitecturesInstallIn64BitMode=x64
AppName=UIMF_Viewer
AppVersion={#ApplicationVersion}
;AppVerName=UIMF_Viewer
AppID={{8CCFDF1F-E30C-4A78-83F3-ABF74D501F2E}}
AppPublisher=Pacific Northwest National Laboratory
AppPublisherURL=http://omics.pnl.gov/software
AppSupportURL=http://omics.pnl.gov/software
AppUpdatesURL=http://omics.pnl.gov/software
DefaultDirName={pf}\UIMF_Viewer
DefaultGroupName=PAST Toolkit
AppCopyright=© PNNL
PrivilegesRequired=poweruser
SetupIconFile=UIMF_Viewer\Images\App.ico
OutputBaseFilename=UIMF_Viewer_{#ApplicationVersion}_Setup
;VersionInfoVersion=1.1.16
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=UIMF_Viewer
VersionInfoCopyright=PNNL
ShowLanguageDialog=no
ChangesAssociations=true
EnableDirDoesntExistWarning=true
AlwaysShowDirOnReadyPage=true
ShowTasksTreeLines=true
OutputDir=Installer\Output
SourceDir=..\
Compression=lzma
SolidCompression=yes

[UninstallDelete]
Name: {app}; Type: filesandordirs
Name: {app}\Tools; Type: filesandordirs

[Files]
; Application files
Source: UIMF_Viewer\bin\Release\UIMF_Viewer.exe;                           DestDir: {app}
Source: UIMF_Viewer\bin\Release\UIMF_Viewer.exe.config;                    DestDir: {app}   
Source: UIMF_Viewer\bin\Release\UIMF_DataViewer.dll;                       DestDir: {app}

; Nuget-Installed libraries
Source: UIMF_Viewer\bin\Release\CsvHelper.dll;                             DestDir: {app}
Source: UIMF_Viewer\bin\Release\DynamicData.dll;                           DestDir: {app}
Source: UIMF_Viewer\bin\Release\Microsoft.WindowsAPICodePack*.dll;         DestDir: {app}
Source: UIMF_Viewer\bin\Release\ReactiveUI*.dll;                           DestDir: {app}
Source: UIMF_Viewer\bin\Release\Splat*.dll;                                DestDir: {app}
Source: UIMF_Viewer\bin\Release\System.Reactive.dll;                       DestDir: {app}
Source: UIMF_Viewer\bin\Release\System.Runtime.CompilerServices.Unsafe.dll; DestDir: {app}
Source: UIMF_Viewer\bin\Release\System.Threading.Tasks.Extensions.dll;     DestDir: {app}
Source: UIMF_Viewer\bin\Release\System.ValueTuple.dll;                     DestDir: {app}
Source: UIMF_Viewer\bin\Release\UIMFLibrary.dll;                           DestDir: {app}
Source: UIMF_Viewer\bin\Release\Xceed.Wpf.Toolkit.dll;                     DestDir: {app}
Source: UIMF_Viewer\bin\Release\ZedGraph.dll;                              DestDir: {app}

; SQLite
Source: UIMF_Viewer\bin\Release\System.Data.SQLite.dll;                    DestDir: {app}
Source: UIMF_Viewer\bin\Release\x64\SQLite.Interop.dll;                    DestDir: {app}\x64
Source: UIMF_Viewer\bin\Release\x86\SQLite.Interop.dll;                    DestDir: {app}\x86

[Dirs]
Name: {commonappdata}\UIMF_Viewer; Flags: uninsalwaysuninstall

[Icons]
Name: {commondesktop}\{cm:AppName}; Filename: {app}\UIMF_Viewer.exe; Tasks: desktopicon; IconFilename: {app}..\Resources\iconSmall.ico; Comment: UIMF_Viewer; IconIndex: 0
Name: {userappdata}\Microsoft\Internet Explorer\Quick Launch\{cm:AppName}; Filename: {app}\UIMF_Viewer.exe; Tasks: quicklaunchicon; IconFilename: {app}..\Resources\iconSmall.ico; Comment: UIMF_Viewer; IconIndex: 0
Name: {group}\UIMF_Viewer; Filename: {app}\UIMF_Viewer.exe; Comment: LCMS Spectator

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Code]
procedure InitializeWizard;
var
  message2: string;
  appname: string;
  appversion: string;
begin
  appname := '{#SetupSetting("AppName")}';
  appversion := '{#SetupSetting("AppVersion")}';
  (* #13 is carriage return, #10 is new line *)
  message2 := 'This will install ' + appname + ' version ' + appversion + ' on your computer.' + #10#10 + 
              'UIMF_Viewer is a standalone Windows graphical user interface tool for viewing IMS and SLIM data in UIMF files.';
  WizardForm.WelcomeLabel2.Caption := message2;
end;
