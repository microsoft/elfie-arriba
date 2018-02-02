@ECHO OFF

:: Kill IIS Express.
::  We have to turn off 'shadow copy' of binaries to ensure XForm.exe can find the Web folder and XForm.Native.dll.
::  If we do that, we have to kill IIS Express so that XForm can rebuild (the files are locked).

TASKKILL /F /IM iisexpress.exe > NUL 2>&1
EXIT /B 0