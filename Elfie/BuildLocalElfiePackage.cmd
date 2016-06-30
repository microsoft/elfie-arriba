@echo on
SETLOCAL
@REM Uncomment this line to update nuget.exe
@REM Doing so can break SLN build (which uses nuget.exe to
@REM create a nuget package for binskim) so must opt-in
@REM %~dp0.nuget\NuGet.exe update -self

call SetCurrentVersion.cmd

@REM In order to build a NuGet package of signed Elfie bits, you must first
@REM copy the results of a code-signing job to the bin\Signed directory

set DROP=.\bin\
if exist %DROP%nuget (rd /s /q %DROP%nuget)
md %DROP%nuget

.nuget\NuGet.exe pack .\Nuget\Microsoft.CodeAnalysis.Elfie.nuspec -Symbols -Properties source_root=.;id=Microsoft.CodeAnalysis.Elfie;major=%MAJOR%;minor=%MINOR%;patch=%PATCH%;prerelease=%PRERELEASE% -Verbosity Quiet -BasePath %DROP%\Release -OutputDirectory %DROP%Nuget 

if "%ERRORLEVEL%" EQU "0" (echo Package created %DROP%Nuget\Microsoft.CodeAnalysis.Elfie.%MAJOR%.%MINOR%.%PATCH%%PRERELEASE%.nupkg)