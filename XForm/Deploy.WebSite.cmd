@ECHO OFF
SET Target=%1
IF "%Target%"=="" (
  ECHO Error. No destination path specified.
  ECHO Ex: Deploy.WebSite bin\x64\Release
  GOTO :EOF
)

IF NOT EXIST "%~dp0XForm.Web\node_modules\monaco-editor\min\vs" (
  ECHO WebSite not yet built. Run Build.cmd for instructions.
  GOTO :End
)

ECHO - Copying XForm.Web required files to '%Target%'
ROBOCOPY /PURGE /NJH /NJS "%~dp0XForm.Web" "%Target%\Web" /XF *.jsx *.scss *.json webpack.config.js
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

XCOPY /Y "%~dp0XForm.Web\node_modules\codemirror\lib\codemirror.*" "%Target%\Web\node_modules\codemirror\lib\"
XCOPY /Y "%~dp0XForm.Web\node_modules\codemirror\addon\mode\simple.*" "%Target%\Web\node_modules\codemirror\addon\mode\"
XCOPY /Y "%~dp0XForm.Web\node_modules\codemirror\addon\hint\show-hint.*" "%Target%\Web\node_modules\codemirror\addon\hint\"
IF NOT %ERRORLEVEL% LEQ 7 GOTO :Error

GOTO :End
:Error
  ECHO Error. Copy failed.
  EXIT /B -1

:End
EXIT /B 0