@ECHO OFF

IF "%1"=="" (
  ECHO Error. A target computer with a writeable 'Production' share must be passed to Deploy.cmd.
  GOTO :EOF
)

SET TargetLocation=\\%1\Production
PUSHD "%~dp0"

IF /I "%2"=="bung" (
  ROBOCOPY /E /NJH /NJS /MIR "Databases" "%TargetLocation%\Databases"
)

ECHO - Deploying Arriba...
ROBOCOPY /E /NJH /NJS /MIR "bin\Release" "%TargetLocation%\bin\Release"
ROBOCOPY /E /NJH /NJS /MIR "Arriba.IIS" "%TargetLocation%\Arriba.IIS"
ROBOCOPY /E /NJH /NJS /MIR "Arriba.Web" "%TargetLocation%\Arriba.Web" /XD node_modules
ROBOCOPY /E /NJH /NJS /MIR "Redirect" "%TargetLocation%\Redirect"
ROBOCOPY /E /NJH /NJS "Configuration" "%TargetLocation%\Configuration"

POPD