@echo off
rem regasm.exe /nologo /CodeBase "%1"
rem if errorlevel 1 goto BuildEventFailed

gacutil.exe /nologo /f /i "%1"
rem if errorlevel 1 goto BuildEventFailed

Regpkg.exe /pkgdeffile:..\bin\debug\ProtoBufGenerator.pkgdef /codebase ..\bin\debug\ProtoBufGenerator.dll

REM Exit properly because the build will not fail
REM unless the final step exits with an error code
goto BuildEventOK

:BuildEventFailed
echo POSTBUILDSTEP FAILED
exit 1

:BuildEventOK
echo POSTBUILDSTEP COMPLETED OK
exit 0