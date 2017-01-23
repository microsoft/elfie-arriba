@ECHO OFF
SET BabelBuildPaths="%~dp0Arriba.Web\jsx"

IF EXIST %1 (
 ECHO - Importing Configuration...
 XCOPY /S /Y /D "%1" "%~dp0Arriba.Web\configuration\"
 SET BabelBuildPaths="%~dp0Arriba.Web\configuration" %BabelBuildPaths%
)

ECHO - Building Website...
ECHO CALL "%~dp0Arriba.Web\node_modules\.bin\babel.cmd" %BabelBuildPaths% --out-file %~dp0Arriba.Web/lib/Search.js --source-maps true
CALL "%~dp0Arriba.Web\node_modules\.bin\babel.cmd" %BabelBuildPaths% --out-file %~dp0Arriba.Web/lib/Search.js --source-maps true

:: Can add --no-comments --minified to reduce the bundle, but this version prefers easier debuggability.