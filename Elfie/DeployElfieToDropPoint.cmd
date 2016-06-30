call SetCurrentVersion.cmd

if exist %DROP% (
echo output location already exists: %DROP%
goto :EOF
)

set FILTERED_EXTENSIONS=%FILTERED_EXTS%

ROBOCOPY %~dp0bin\Release     %DROP%\binaries\Release /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0bin\DelaySigned %DROP%\binaries\DelaySigned /E /XO /XF %FILTERED_EXTS%

ROBOCOPY %~dp0Elfie           %DROP%\source\Elfie /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0Elfie.EndToEnd  %DROP%\source\Elfie.EndToEnd /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0Elfie.Indexer   %DROP%\source\Elfie.Indexer /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0Elfie.Merger    %DROP%\source\Elfie.Merger /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0Elfie.Search    %DROP%\source\Elfie.Search /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0SourceIndex     %DROP%\source\SourceIndex /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0SymbolSourceGet %DROP%\source\SymbolSourceGet /E /XO /XF %FILTERED_EXTS%
ROBOCOPY %~dp0Nuget           %DROP%\source\Nuget /E /XO /XF %FILTERED_EXTS%

goto :EOF