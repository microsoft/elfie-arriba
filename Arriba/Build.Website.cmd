@ECHO OFF
SET here=%~dp0
SET MainConfiguration=%1

:: Build.Website can be passed any number of configuration folders to control the look of Arriba.Web.
:: They must be copied into Arriba.Web\configuration for webpack to bundle them correctly.

:: The first configuration is the "primary", and is copied to Arriba.Web\configuration itself.
:: Additional configurations are copied into subfolders. [ex: Databases\WorkItemDetails -> Arriba.Web\configuration\WorkItemDetails]

:: Build.Website will try to avoid losing your work by copying changed files in the Arriba.Web\configuration folder back to the sources.

:NextConfiguration
IF EXIST "%2" (
  ECHO - Copying base '%2'...
  IF EXIST "%here%Arriba.Web\configuration\%~nx2" ROBOCOPY /E /XO /NJH /NJS "%here%Arriba.Web\configuration\%~nx2" "%2"
  ROBOCOPY /E /NJH /NJS "%2" "%here%Arriba.Web\configuration\%~nx2"
)
SHIFT
IF NOT "%2"=="" GOTO :NextConfiguration

IF EXIST "%MainConfiguration%" (
  ECHO - Synchronizing Configuration...
  IF NOT EXIST "%MainConfiguration%\Configuration.jsx" (
    ECHO ERROR. '%MainConfiguration%' didn't contain a Configuration.jsx. Is your path right? 
    GOTO :EOF
  )

  IF EXIST "%here%Arriba.Web\configuration" ROBOCOPY /XO /NJH /NJS "%here%Arriba.Web\configuration" "%MainConfiguration%"
  ROBOCOPY /E /NJH /NJS "%MainConfiguration%" "%here%Arriba.Web\configuration"
) ELSE (
  IF EXIST "%here%Arriba.Web\configuration" (
    IF EXIST "%here%Arriba.Web\configuration.BAK" RMDIR /S /Q "%here%Arriba.Web\configuration.BAK"
    MOVE "%here%Arriba.Web\configuration" "%here%Arriba.Web\configuration.BAK"
  )
)

ECHO - Building Website...
PUSHD "%here%Arriba.Web"
CALL "node_modules\.bin\webpack.cmd"
POPD