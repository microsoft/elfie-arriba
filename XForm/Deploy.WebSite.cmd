@ECHO OFF
SET Target=%1
IF "%Target%"=="" (
  ECHO Error. No destination path specified.
  ECHO Ex: Deploy.WebSite bin\x64\Release
  GOTO :EOF
)

ECHO - Copying XForm.Web required files to '%Target%'
ROBOCOPY /PURGE /NJH /NJS "%~dp0XForm.Web" "%Target%\Web" /XF *.jsx *.scss *.json webpack.config.js
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

ROBOCOPY /MIR /NJH /NJS "%~dp0XForm.Web\node_modules\monaco-editor\min\vs" "%Target%\Web\node_modules\monaco-editor\min\vs" /XD basic-languages language contrib standalone /XF editor.main.nls.*.js
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

GOTO :End
:Error
  ECHO Error. Copy failed.
  EXIT /B -1

:End
EXIT /B 0