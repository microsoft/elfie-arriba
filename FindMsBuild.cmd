@ECHO OFF
SET MSBuildPath=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe
IF NOT EXIST "%MSBuildPath%" SET MSBuildPath=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe
IF NOT EXIST "%MSBuildPath%" SET MSBuildPath=%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe
IF NOT EXIST "%MSBuildPath%" SET MSBuildPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe
