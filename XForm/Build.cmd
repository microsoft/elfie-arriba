@ECHO OFF
CALL "%~dp0..\FindMSBuild.cmd"

ECHO - Building XForm.Web...
PUSHD %~dp0XForm.Web
IF NOT EXIST node_modules\.bin\WebPack.cmd (
  ECHO Error. XForm.Web can't build because webpack wasn't found.
  ECHO.
  ECHO First Time Setup:
  ECHO  - Install VS and NPM [https://github.com/Microsoft/elfie-arriba/wiki/XForm-QuickStart]
  ECHO  - In XForm.Web, run 'npm install'
  ECHO  - In XForm, run '..\.nuget\NuGet.exe restore XForm.sln'
  ECHO  - In XForm, run 'Build.cmd'
  GOTO Error
)
CALL node_modules\.bin\WebPack.cmd
SET WebPackError=%ERRORLEVEL%
POPD
IF NOT "%WebPackError%"=="0" GOTO Error

ECHO - Building XForm...
PUSHD %~dp0
"%MSBuildPath%" "XForm.sln" /p:Configuration=Release /p:Platform="x64"
SET MSBuildError=%ERRORLEVEL%
POPD
IF NOT "%MSBuildError%"=="0" GOTO Error

ECHO - Copying Extensions...
XCOPY /Y "%~dp0\bin\x64\Release" "%~dp0\XForm.IIS\bin"

GOTO :EOF
:Error
  ECHO Error. Build Stopping.
  EXIT /B -1
GOTO :EOF