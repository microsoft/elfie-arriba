PUSHD "%~dp0V5.ConsoleTest\bin\x64\Release"
VSPerf /launch:"V5.ConsoleTest.exe" /file:"%~dp0Profile.vsp" 
::VSPerf /stop
POPD