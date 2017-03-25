@echo off
rem "%windir%\Microsoft.NET\Framework\v2.0.50727\regasm.exe" /nologo /CodeBase "%1"
rem if errorlevel 1 goto BuildEventFailed

rem "%ProgramFiles%\Microsoft SDKs\Windows\v6.0A\bin\gacutil.exe" /nologo /f /i "%1"
rem if errorlevel 1 goto BuildEventFailed

rem "%ProgramFiles%\Microsoft Visual Studio 2008 SDK\VisualStudioIntegration\Tools\Bin\RegPkg.exe" /pkgdeffile:"%2" /codebase "%1"
rem "%ProgramFiles%\Microsoft Visual Studio 2008 SDK\VisualStudioIntegration\Tools\Bin\RegPkg.exe" /regfile:"%3" /codebase "%1"

REM Exit properly because the build will not fail
REM unless the final step exits with an error code
goto BuildEventOK

:BuildEventFailed
echo POSTBUILDSTEP FAILED
exit 1

:BuildEventOK
echo POSTBUILDSTEP COMPLETED OK
exit 0