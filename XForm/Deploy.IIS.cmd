@ECHO OFF
SET AppCmdPath=%windir%\system32\inetsrv\appcmd.exe
SET Target=%1
IF "%Target%"=="" (
  ECHO Error. No destination path specified.
  ECHO Ex: Deploy.WebSite bin\x64\Release
  GOTO :EOF
)

ECHO - Deploying Website..
"%AppCmdPath%" stop sites "XForm.IIS"
ROBOCOPY /MIR /NJH /NJS "%~dp0XForm.IIS\bin" "%Target%\bin"
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error
COPY /Y "%~dp0XForm.IIS\Web.config" "%Target%\Web.config"
"%AppCmdPath%" start sites "XForm.IIS"

GOTO :End
:Error
  ECHO Error. Copy failed.
  EXIT /B -1

:End
EXIT /B 0