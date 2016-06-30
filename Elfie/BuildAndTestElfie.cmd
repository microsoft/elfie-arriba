@echo off
SETLOCAL
@REM Uncomment this line to update nuget.exe
@REM Doing so can break SLN build (which uses nuget.exe to
@REM create a nuget package for elfie) so must opt-in
@REM %~dp0..\.nuget\NuGet.exe update -self

call SetCurrentVersion.cmd

set VERSION_CONSTANTS=Shared\VersionConstants.cs

if exist bin\Debug (rd /s /q bin\Debug)
if exist bin\Release (rd /s /q bin\Release)

@REM Rewrite VersionConstants.cs
echo // Copyright (c) Microsoft. All rights reserved.                                                      >  %VERSION_CONSTANTS%
echo // Licensed under the MIT license. See LICENSE file in the project root for full license information. >> %VERSION_CONSTANTS%
echo.                                                                                                      >> %VERSION_CONSTANTS%
echo namespace Microsoft.CodeAnalysis.Elfie                                                                >> %VERSION_CONSTANTS%
echo {                                                                                                     >> %VERSION_CONSTANTS%
echo     public static class VersionConstants                                                              >> %VERSION_CONSTANTS%
echo     {                                                                                                 >> %VERSION_CONSTANTS%
echo         public const string Prerelease = "%PRERELEASE%";                                              >> %VERSION_CONSTANTS%
echo         public const string AssemblyVersion = "%MAJOR%.%MINOR%.%PATCH%";                              >> %VERSION_CONSTANTS%
echo         public const string FileVersion = AssemblyVersion + ".%REVISION%";                            >> %VERSION_CONSTANTS%
echo         public const string Version = AssemblyVersion + Prerelease;                                   >> %VERSION_CONSTANTS%
echo     }                                                                                                 >> %VERSION_CONSTANTS%
echo  }                                                                                                    >> %VERSION_CONSTANTS%

@REM unsigned build
%~dp0..\.nuget\NuGet.exe restore Elfie.sln 
msbuild /verbosity:minimal /target:rebuild Elfie.sln  /p:Configuration=Release /p:"Platform=Any CPU"
if "%ERRORLEVEL%" NEQ "0" (goto ExitFailed)

@REM delay-signed build
set DELAY_SIGNED=1
msbuild /verbosity:minimal /target:rebuild Elfie.sln /p:OutputPath=%~dp0bin\DelaySigned /p:DefineConstants=DELAY_SIGNED /p:SignAssembly=true /p:DelaySign=true /p:AssemblyOriginatorKeyFile=%~dp035MSSharedLib1024.snk /p:Configuration=Release /p:"Platform=Any CPU"
if "%ERRORLEVEL%" NEQ "0" (goto ExitFailed)

@REM run unit tests
SET PASSED=true

mstest /testContainer:bin\Release\Elfie.Test.dll /testsettings:Elfie.testsettings
if "%ERRORLEVEL%" NEQ "0" (set PASSED=false)

@REM add new tests here

if "%PASSED%" NEQ "true" (goto ExitFailed)
goto Exit

:ExitFailed
@echo  
@echo SCRIPT FAILED

:Exit
