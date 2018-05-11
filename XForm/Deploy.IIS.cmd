@ECHO OFF
SET Target=%1
IF "%Target%"=="" (
  ECHO Error. No destination path specified.
  ECHO Usage: Deploy.IIS [TargetPath] [WebConfigSourcePath?]
  ECHO Ex: Deploy.IIS C:\inetpub\XForm.IIS C:\Configs\XForm.IIS\Web.config
  GOTO :EOF
)

SET WebConfigPath=%~dp0XForm.IIS\Web.config
IF NOT "%2"=="" SET WebConfigPath=%2

ECHO - Deploying Web.config [to stop site]
COPY /Y "%WebConfigPath%" "%Target%\Web.config"

ECHO - Deploying Website...
ROBOCOPY /MIR /NJH /NJS "%~dp0XForm.IIS\bin" "%Target%\bin"
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

GOTO :End
:Error
  ECHO Error. Copy failed.
  EXIT /B -1

:End
EXIT /B 0