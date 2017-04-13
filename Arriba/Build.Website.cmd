@ECHO OFF

IF EXIST "%1" (
  ECHO - Synchronizing Configuration...
  ROBOCOPY /E /XO /NJH /NJS "%~dp0Arriba.Web\configuration" "%1"
  ROBOCOPY /E /NJH /NJS /MIR "%1" "%~dp0Arriba.Web\configuration"
) ELSE (
  IF EXIST "%~dp0Arriba.Web\configuration" (
    IF EXIST "%~dp0Arriba.Web\configuration.BAK" RMDIR /S /Q "%~dp0Arriba.Web\configuration.BAK"
    MOVE "%~dp0Arriba.Web\configuration" "%~dp0Arriba.Web\configuration.BAK"
  )
)

ECHO - Building Website...
PUSHD "%~dp0Arriba.Web
CALL webpack
POPD