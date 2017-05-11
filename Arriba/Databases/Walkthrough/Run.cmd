START CMD /K "PUSHD %~dp0..\..\bin\Release & Arriba.Server.exe"
START CMD /K "PUSHD %~dp0..\..\Arriba.Web & npm start"
TIMEOUT 5
START http://localhost:8080/Search.html