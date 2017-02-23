@ECHO OFF
PUSHD %~dp0..

:: The Arriba service runs as an App Pool account, which has read/execute access to the Arriba site, DiskCache, and read-write access to the Arriba Logs.
:: Users browsing access the site as themselves (Windows Auth). Everyone needs read/execute access to the ConfluxSearch site and redirect site only.

ECHO - DiskCache [Arriba Data]
IF NOT EXIST DiskCache (MD DiskCache)
ICACLS DiskCache /Grant IIS_IUSRS:(OI)(CI)(RX)
ICACLS DiskCache\* /Reset /T

ECHO - Arriba.IIS
ICACLS Arriba.IIS /Grant Everyone:(OI)(CI)(RX)
ICACLS Arriba.IIS\* /Reset /T

ECHO - Arriba Usage Logging
IF NOT EXIST Logs (MD Logs)
ICACLS Logs /Grant IIS_IUSRS:(OI)(CI)(F)
ICACLS Logs\* /Reset /T

ECHO - Arriba.Web
ICACLS Arriba.Web /Grant Everyone:(OI)(CI)(RX)
ICACLS Arriba.Web\* /Reset /T

ECHO - Redirect Site
ICACLS Redirect /Grant Everyone:(OI)(CI)(RX)
ICACLS Redirect\* /Reset /T

POPD