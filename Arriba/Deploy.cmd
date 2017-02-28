@ECHO OFF

IF "%1"=="" (
  ECHO Error. A target computer with a writeable 'Production' share must be passed to Deploy.cmd.
  GOTO :EOF
)

SET TargetLocation=\\%1\Production
PUSHD "%~dp0"

ECHO - Deploying Arriba...
ROBOCOPY /E /XO /NJH /NJS /MIR "bin\Release" "%TargetLocation%\bin\Release"
ROBOCOPY /E /XO /NJH /NJS /MIR "Arriba.IIS" "%TargetLocation%\Arriba.IIS"
ROBOCOPY /E /XO /NJH /NJS "Arriba.Web" "%TargetLocation%\Arriba.Web" /XD node_modules
ROBOCOPY /E /XO /NJH /NJS /MIR "Redirect" "%TargetLocation%\Redirect"
ROBOCOPY /E /XO /NJH /NJS "Configuration" "%TargetLocation%\Configuration"

POPD