@ECHO OFF
CALL "%~dp0..\FindMSBuild.cmd"



ECHO - Building XForm...
PUSHD %~dp0
"%MSBuildPath%" "XForm.sln" /p:Configuration=Release /p:Platform="x64"
SET MSBuildError=%ERRORLEVEL%
POPD
IF NOT "%MSBuildError%"=="0" GOTO Error

ECHO - Building XForm.Web...
PUSHD %~dp0XForm.Web
IF NOT EXIST node_modules\.bin\WebPack.cmd (
  ECHO Error. XForm.Web can't build because webpack wasn't found.
  ECHO Have you installed NPM and run 'npm install' from the XForm.Web folder?
  GOTO Error
)
CALL node_modules\.bin\WebPack.cmd
SET WebPackError=%ERRORLEVEL%
POPD
IF NOT "%WebPackError%"=="0" GOTO Error

GOTO :EOF
:Error
  ECHO Error. Build Stopping.
  EXIT /B -1
GOTO :EOF