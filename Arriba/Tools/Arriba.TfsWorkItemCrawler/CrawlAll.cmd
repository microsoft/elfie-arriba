
@ECHO OFF
SET ConfigurationsFolder=%~dp0..\..\Databases

SET YearMonthDay=%DATE:~10,4%-%DATE:~4,2%-%DATE:~7,2%
ECHO %YearMonthDay%

ECHO.
ECHO [%DATE% %TIME%] Crawling all Databases under '%ConfigurationsFolder%'...
MD "%~dp0Logs\Arriba.TfsWorkItemCrawler"
ECHO [%DATE% %TIME%] Crawling all Databases under '%ConfigurationsFolder%'... >> %~dp0Logs\Arriba.TfsWorkItemCrawler\%YearMonthDay%.log

IF EXIST %ConfigurationsFolder% (
  ECHO.
  FOR /F "delims=" %%A IN ('DIR /B "%ConfigurationsFolder%\*"') DO (
    ECHO.
    ECHO ========================================
    IF EXIST %ConfigurationsFolder%\%%A\ManualCrawl.marker (
      ECHO   - %%A [Manual; Skipping]
    ) ELSE (
      ECHO "%~dp0Arriba.TfsWorkItemCrawler.exe" %%A -i
      "%~dp0Arriba.TfsWorkItemCrawler.exe" %%A -i
    )
    ECHO ========================================
  )
)

ECHO [%DATE% %TIME%] Crawl All Pass Done. >> %~dp0Logs\Arriba.TfsWorkItemCrawler\%YearMonthDay%.log
ECHO. >> %~dp0Logs\Arriba.TfsWorkItemCrawler\%YearMonthDay%.log
