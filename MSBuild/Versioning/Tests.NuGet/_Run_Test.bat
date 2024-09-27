@echo off
setlocal enabledelayedexpansion

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
	echo.
	echo ======================================================================
	echo.
	echo Setup: build MSBuild task first and set up the target project
	echo.
	echo ======================================================================
	echo.
	"%%i" ..\Versioning.MSBuild\Versioning.MSBuild.csproj /p:Configuration=Release
	if errorlevel 1 goto End
	goto Test
)

:Not_Found
echo.
echo Cannot find Visual Studio's MSBuild.exe
echo Run %~n0 from the Visual Studio Developer Command Prompt
echo See the menu: Tools - Command Line
echo.
goto End

:Test
rmdir Test.NFProject /s /q
if errorlevel 1 goto End
mkdir Test.NFProject
if errorlevel 1 goto End
xcopy /s Test.NFProject.Template Test.NFProject
if errorlevel 1 goto End
cd Test.NFProject
..\nuget restore packages.config
cd ..
echo.
echo ======================================================================
echo.
echo Test of NuGet package creation, installation and use
echo.
echo ======================================================================
echo.
echo ----------------------------------------------------------------------
echo Create package
echo ----------------------------------------------------------------------
echo.
nuget pack ..\Versioning.MSBuild\nanoFramework.Versioning.nuspec -Version 1.2.3.4 -OutputDirectory Test.NFProject
if errorlevel 1 goto End

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest -requires Microsoft.VisualStudio.Workload.NativeDesktop -find Common7\IDE\devenv.exe`) do (
	echo.
	echo ----------------------------------------------------------------------
	echo Add package in Visual Studio from the "Test" source
	echo ----------------------------------------------------------------------
	echo.
	"%%i" Test.NFProject\Test.NFProject.sln
	if errorlevel 1 goto End
	goto End
)

echo.
echo Cannot find Visual Studio
echo Start Visual Studio manually and open Test.NFProject\Test.NFProject.sln
echo.
goto End

:End
pause