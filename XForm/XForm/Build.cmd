@ECHO OFF
CALL "%~dp0..\..\FindMSBuild.cmd"

PUSHD %~dp0..

ECHO - Building XForm...
"%MSBuildPath%" "XForm.sln" /p:Configuration=Release /p:Platform="x64"
SET MSBuildError=%ERRORLEVEL%
POPD
IF NOT "%MSBuildError%"=="0" GOTO Error

GOTO :EOF
:Error
  ECHO Error. Build Stopping.
  EXIT /B -1
GOTO :EOF