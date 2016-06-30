call SetCurrentVersion.cmd

if exist %DROP% (
echo output location already exists: %DROP%
goto :EOF
)

call BuildAndTestElfie.cmd

@IF NOT "%MSBuildResult%"=="0" (
  @ECHO Error. Build failed.
@REM  @GOTO :EOF
)

call DeployElfieToDropPoint.cmd


