@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
SET MsBuildPath=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

@REM unsigned build
%~dp0..\.nuget\NuGet.exe restore %~dp0Arriba.All.sln 

CALL :Build Arriba.All.sln

ENDLOCAL
GOTO :EOF

:: Build(Solution)
:Build
  ECHO.
  ECHO ======================================== %DATE% %TIME%
  ECHO "%MsBuildPath%" %~dp0%~1 /p:Configuration=Release /p:Platform="Any CPU"
  "%MsBuildPath%" %~dp0%~1 /p:Configuration=Release /p:Platform="Any CPU"
  ECHO ======================================== %DATE% %TIME%
  SET MsBuildError=!ERRORLEVEL!
  IF NOT "!MsBuildError!"=="0" GOTO Error
GOTO :EOF

:Error
  ECHO Error. Build Stopping.
  EXIT /B -1
GOTO :EOF