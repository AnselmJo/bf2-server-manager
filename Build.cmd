@echo off
rem Builds out\BF2Manager.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
rem "Build.cmd test" also builds and runs the automated tests.
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
cd /d "%~dp0"
if not exist out mkdir out
set REFS=/r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Web.Extensions.dll
set COMMON=/nologo /langversion:5 /codepage:65001 /resource:src\Server\coopbots.py,coopbots.py %REFS%
"%CSC%" %COMMON% /target:winexe /optimize+ /win32icon:build\app.ico /win32manifest:build\app.manifest /out:out\BF2Manager.exe /recurse:src\*.cs || goto fail
echo Built out\BF2Manager.exe
if /i not "%1"=="test" exit /b 0
"%CSC%" %COMMON% /target:exe /main:BF2Coop.Tests.Runner /out:out\Tests.exe /recurse:src\*.cs /recurse:tests\*.cs || goto fail
out\Tests.exe || goto fail
exit /b 0
:fail
echo Build failed.
exit /b 1
