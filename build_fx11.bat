@cls
@SET OUTDIR=build\fx11
@rd %OUTDIR% /Q /S
@md %OUTDIR%
@%SystemRoot%\Microsoft.Net\Framework\v1.1.4322\csc.exe /debug- /optimize+ "/out:%OUTDIR%\SampleDTO.dll" /define:FX11 /define:NO_GENERICS /target:library /recurse:SampleDTO\*.cs
@%SystemRoot%\Microsoft.Net\Framework\v1.1.4322\csc.exe /debug- /optimize+ "/out:%OUTDIR%\protobuf-net.dll" /define:FX11 /define:NO_GENERICS  /define:FEAT_COMPILER /target:library /unsafe /recurse:protobuf-net\*.cs
@%SystemRoot%\Microsoft.Net\Framework\v1.1.4322\csc.exe /debug- /optimize+ "/out:%OUTDIR%\test.exe" /define:FX11 /define:NO_GENERICS /define:FEAT_COMPILER "/r:%OUTDIR%\protobuf-net.dll" /target:exe /recurse:FX11\*.cs "/r:%OUTDIR%\SampleDTO.dll"

@cd %OUTDIR%
@test
@cd ..\..