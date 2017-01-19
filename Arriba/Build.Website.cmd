@ECHO OFF
SET BabelBuildPaths=%~dp0Arriba.Web
IF EXIST %1 SET BabelBuildPaths=%1 %BabelBuildPaths%

ECHO - Building Website...
CALL babel %BabelBuildPaths% --out-file %~dp0Arriba.Web/lib/Search.js --source-maps true

:: Can add --no-comments --minified to reduce the bundle, but this version prefers easier debuggability.