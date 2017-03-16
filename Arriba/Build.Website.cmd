@ECHO OFF
SET BabelBuildPaths="%~dp0Arriba.Web\jsx"

IF EXIST "%1" (
 ECHO - Importing Configuration...
 XCOPY /S /Y /D "%1" "%~dp0Arriba.Web\configuration\"
 SET BabelBuildPaths="%~dp0Arriba.Web\configuration" %BabelBuildPaths%
)

ECHO - Building Website...

pushd "%~dp0Arriba.Web
CALL webpack
popd

:: Can add --no-comments --minified to reduce the bundle, but this version prefers easier debuggability.