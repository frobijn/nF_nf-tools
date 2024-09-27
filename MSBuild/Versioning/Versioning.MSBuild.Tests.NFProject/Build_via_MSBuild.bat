@echo off
setlocal enabledelayedexpansion

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  "%%i" Versioning.MSBuild.Tests.NFProject.nfproj /p:Configuration=Release
  goto End
)

:Not_Found
echo.
echo Cannot find Visual Studio's MSBuild.exe
echo Run %~n0 from the Visual Studio Developer Command Prompt
echo See the menu: Tools - Command Line
echo.
goto End

:End
pause
