@ECHO OFF
SET Target=%1
IF "%Target%"=="" (
  ECHO Error. No destination path specified.
  ECHO Ex: Deploy.WebSite bin\x64\Release
  GOTO :EOF
)

ECHO - Copying XForm runtime required files to '%Target%\Database'...
ROBOCOPY /NJH /NJS "%~dp0bin\x64\Release" "%Target%\Database" /XD Source Table Config Query
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

ECHO - Copying XForm WebSite to '%Target%\XForm.IIS'...
ROBOCOPY /E  /NJH /NJS "%~dp0bin\x64\Release" "%~dp0XForm.IIS\bin"
ROBOCOPY /MIR /NJH /NJS "%~dp0XForm.IIS\bin" "%Target%\XForm.IIS\bin"
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

GOTO :End
:Error
  ECHO Error. Copy failed.
  EXIT /B -1

:End
EXIT /B 0