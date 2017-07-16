SETLOCAL
SET PREMERGE=%CD%\trace.premerge.etl
SET PREMERGE_USER=%CD%\trace_u.premerge.etl
SET MERGE=%CD%\trace.etl

:: I/O and Memory
xperf -start -on PROC_THREAD+LOADER+PROFILE+CSWITCH+DISPATCHER+DISK_IO+VAMAP+HARD_FAULTS -stackwalk PROFILE+CSWITCH+READYTHREAD+DiskReadInit+DiskWriteInit+MapFile+UnMapFile -SetProfInt 3000 -BufferSize 1024 -f %PREMERGE%

:: Memory only
REM xperf -start -on PROC_THREAD+LOADER+PROFILE -stackwalk PROFILE -SetProfInt 3000 -BufferSize 1024 -f %PREMERGE%


REM xperf -start "tracesession" -on 01e4ad87-6545-45de-a198-f6398f3f1b51 -f %PREMERGE_USER%
REM %*

"%~dp0V5.ConsoleTest\bin\x64\Release\V5.ConsoleTest.exe"

xperf -stop "tracesession" -stop -d %MERGE%

:: To find V5.Native.dll module load address:
:: xperf -i trace.etl | findstr /ic:"ImageId" | findstr /ic:"v5.native.dll"