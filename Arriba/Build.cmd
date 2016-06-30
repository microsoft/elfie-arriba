@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
SET MsBuildPath="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"

@REM unsigned build
%~dp0..\.nuget\NuGet.exe restore Arriba.sln 

CALL :Build Arriba.sln

ENDLOCAL
GOTO :EOF

:: Build(Solution)
:Build
  ECHO.
  ECHO ======================================== %DATE% %TIME%
  ECHO %MsBuildPath% %~dp0%~1 /p:Configuration=Release /p:Platform="Any CPU"
  %MsBuildPath% %~dp0%~1 /p:Configuration=Release /p:Platform="Any CPU"
  ECHO ======================================== %DATE% %TIME%
  SET MsBuildError=!ERRORLEVEL!
  IF NOT "!MsBuildError!"=="0" GOTO Error
GOTO :EOF

:Error
  ECHO Error. Build Stopping.
  EXIT /B -1
GOTO :EOF