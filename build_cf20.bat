@cls
@SET OUTDIR=build\cf20
@rd %OUTDIR% /Q /S
@md %OUTDIR%
@set FLIB=%ProgramFiles%\Microsoft.NET\SDK\CompactFramework\v2.0\WindowsCE
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /noconfig /nostdlib "/out:%OUTDIR%\SampleDTO.dll" /target:library /recurse:SampleDTO\*.cs "/r:%FLIB%\mscorlib.dll" "/r:%FLIB%\system.dll" /define:CF
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /noconfig /nostdlib "/out:%OUTDIR%\protobuf-net.dll" /target:library /unsafe /recurse:protobuf-net\*.cs "/r:%FLIB%\mscorlib.dll" "/r:%FLIB%\system.dll" /define:CF
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /noconfig /nostdlib "/out:%OUTDIR%\test.exe" "/r:%OUTDIR%\protobuf-net.dll" "/r:%OUTDIR%\SampleDTO.dll" /target:exe /recurse:FX11\*.cs "/r:%FLIB%\mscorlib.dll" "/r:%FLIB%\system.dll" /define:CF
@cd %OUTDIR%
@test
@cd ..\..