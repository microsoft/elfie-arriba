@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
CALL "%~dp0..\FindMSBuild.cmd"

@REM unsigned build
%~dp0..\.nuget\NuGet.exe restore %~dp0Arriba.All.sln 

CALL :Build Arriba.All.sln

IF /I "%1"=="bung" (
  ROBOCOPY /E /NJH /NJS "%~dp0Tools\bin\Release" "%~dp0bin\Release"
)

ENDLOCAL
EXIT /B 0
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