@ECHO OFF
SET AppCmdPath=%windir%\system32\inetsrv\appcmd.exe
SET Target=%1
IF "%Target%"=="" (
  ECHO Error. No destination path specified.
  ECHO Usage: Deploy.IIS [TargetPath] [WebConfigSourcePath?]
  ECHO Ex: Deploy.IIS C:\inetpub\XForm.IIS C:\Configs\XForm.IIS\Web.config
  GOTO :EOF
)

SET WebConfigPath=%~dp0XForm.IIS\Web.config
IF NOT "%2"=="" SET WebConfigPath=%2

ECHO - Deploying Website...
"%AppCmdPath%" stop apppool "XForm.IIS"

ROBOCOPY /MIR /NJH /NJS "%~dp0XForm.IIS\bin" "%Target%\bin"
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

ECHO - Copying Web.config from [%WebConfigPath%]...
COPY /Y "%WebConfigPath%" "%Target%\Web.config"

"%AppCmdPath%" start apppool "XForm.IIS"


GOTO :End
:Error
  ECHO Error. Copy failed.
  EXIT /B -1

:End
EXIT /B 0