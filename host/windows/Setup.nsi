; SPDX-License-Identifier: GPL-3.0-or-later
Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
!include "WinVer.nsh"

!define Product "Generic Wupstream Program"
!define Version "2.1.0"
!define Exe "${Product}.exe"
!define UninstallKey "Software\Microsoft\Windows\CurrentVersion\Uninstall\GenericWupstreamProgram"

Name "${Product} ${Version}"
OutFile "${WUP_INSTALLER}"
InstallDir "$LOCALAPPDATA\Programs\${Product}"
InstallDirRegKey HKCU "${UninstallKey}" "InstallLocation"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails show
VIProductVersion "${Version}.0"
VIAddVersionKey "ProductName" "${Product}"
VIAddVersionKey "FileDescription" "${Product} Setup"
VIAddVersionKey "FileVersion" "${Version}"
VIAddVersionKey "LegalCopyright" "GPL-3.0-or-later"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\${Exe}"
!define MUI_FINISHPAGE_RUN_NOTCHECKED
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Spanish"

LangString Unsupported ${LANG_ENGLISH} "This setup requires 64-bit Windows 10 or 11."
LangString Unsupported ${LANG_SPANISH} "El instalador requiere Windows 10 u 11 de 64 bits."
LangString NeedFramework ${LANG_ENGLISH} "Install .NET Framework 4.8 or later before continuing."
LangString NeedFramework ${LANG_SPANISH} "Instala .NET Framework 4.8 o posterior antes de continuar."
LangString NeedBus ${LANG_ENGLISH} "The app requires ViGEmBus for the virtual controller. The included guide links to the official driver."
LangString NeedBus ${LANG_SPANISH} "La app requiere ViGEmBus para el mando virtual. La guía incluida enlaza al controlador oficial."
LangString Running ${LANG_ENGLISH} "Close Generic Wupstream Program before continuing."
LangString Running ${LANG_SPANISH} "Cierra Generic Wupstream Program antes de continuar."
LangString WriteError ${LANG_ENGLISH} "Could not write the application files. Check the folder and close other copies of the app."
LangString WriteError ${LANG_SPANISH} "No se pudieron escribir los archivos. Revisa la carpeta y cierra otras copias de la app."

!macro RejectIfRunning
  System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\GenericWupstreamProgram") p .r0'
  ${If} $0 P<> 0
    System::Call 'kernel32::CloseHandle(p r0)'
    MessageBox MB_OK|MB_ICONEXCLAMATION "$(Running)" /SD IDOK
    SetErrorLevel 2
    Quit
  ${EndIf}
!macroend

Function .onInit
  IfSilent checks
  !insertmacro MUI_LANGDLL_DISPLAY
  checks:
  ${IfNot} ${RunningX64}
  ${OrIfNot} ${AtLeastWin10}
    MessageBox MB_OK|MB_ICONSTOP "$(Unsupported)" /SD IDOK
    SetErrorLevel 3
    Quit
  ${EndIf}
  !insertmacro RejectIfRunning
  SetRegView 64
  ReadRegDWORD $0 HKLM "Software\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  ${If} $0 < 528040
    MessageBox MB_OK|MB_ICONSTOP "$(NeedFramework)" /SD IDOK
    SetErrorLevel 4
    Quit
  ${EndIf}
  ReadRegStr $0 HKLM "SYSTEM\CurrentControlSet\Services\ViGEmBus" "ImagePath"
  ${If} $0 == ""
    MessageBox MB_OK|MB_ICONINFORMATION "$(NeedBus)" /SD IDOK
  ${EndIf}
  SetRegView 32
FunctionEnd

Section "Install"
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  ClearErrors
  File "${WUP_BIN}\${Exe}"
  File "${WUP_BIN}\WupStreamNative.dll"
  File "${WUP_BIN}\Nefarius.ViGEm.Client.dll"
  File "..\..\README.md"
  File "..\..\README.en.md"
  File "..\..\LICENSE"
  File "LICENSE.ViGEm.txt"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  IfErrors install_error
  CreateShortcut "$SMPROGRAMS\${Product}.lnk" "$INSTDIR\${Exe}"
  WriteRegStr HKCU "${UninstallKey}" "DisplayName" "${Product}"
  WriteRegStr HKCU "${UninstallKey}" "DisplayVersion" "${Version}"
  WriteRegStr HKCU "${UninstallKey}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UninstallKey}" "DisplayIcon" "$INSTDIR\${Exe}"
  WriteRegStr HKCU "${UninstallKey}" "URLInfoAbout" "https://github.com/JLiveOf/Generic-Wupstream-Program"
  WriteRegStr HKCU "${UninstallKey}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegDWORD HKCU "${UninstallKey}" "NoModify" 1
  WriteRegDWORD HKCU "${UninstallKey}" "NoRepair" 1
  Goto done
  install_error:
    MessageBox MB_OK|MB_ICONSTOP "$(WriteError)" /SD IDOK
    SetErrorLevel 5
    Quit
  done:
SectionEnd

Function un.onInit
  !insertmacro RejectIfRunning
FunctionEnd

Section "Uninstall"
  SetShellVarContext current
  Delete "$SMPROGRAMS\${Product}.lnk"
  Delete "$INSTDIR\${Exe}"
  Delete "$INSTDIR\WupStreamNative.dll"
  Delete "$INSTDIR\Nefarius.ViGEm.Client.dll"
  Delete "$INSTDIR\README.md"
  Delete "$INSTDIR\README.en.md"
  Delete "$INSTDIR\LICENSE"
  Delete "$INSTDIR\LICENSE.ViGEm.txt"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "${UninstallKey}"
SectionEnd
