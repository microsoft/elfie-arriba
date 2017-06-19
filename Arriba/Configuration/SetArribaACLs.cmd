@ECHO OFF
PUSHD %~dp0..
SET ServiceAccount=%1

:: The Arriba service runs as an App Pool account, which needs read-write access to DiskCache and Logs.
:: Users access the site as themselves, due to Windows Authentication, so Everyone needs read/execute access to Arriba.IIS, Arriba.Web, and Redirect.
:: If Tables are written directly only (not imported to the Arriba HTTP service), the DiskCache folder can be read only for IIS_IUSRS for extra security.

ECHO - DiskCache [Arriba Data]
IF NOT EXIST DiskCache (MD DiskCache)
ICACLS DiskCache /Grant IIS_IUSRS:(OI)(CI)(F)
IF NOT "%1"=="" ICACLS DiskCache /Grant %1:(OI)(CI)(F)
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