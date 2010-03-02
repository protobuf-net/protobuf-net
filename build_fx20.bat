@cls
@SET OUTDIR=build\fx20
@rd %OUTDIR% /Q /S
@md %OUTDIR%
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /debug- /optimize+ /define:FX30 "/out:%OUTDIR%\SampleDTO.dll" /target:library /recurse:SampleDTO\*.cs "/r:%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.0\System.Runtime.Serialization.dll"
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /debug- /optimize+ "/out:%OUTDIR%\protobuf-net.dll" /define:FEAT_COMPILER /target:library /unsafe /recurse:protobuf-net\*.cs
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /debug- /optimize+ /define:FX30 "/out:%OUTDIR%\test.exe" /define:FEAT_COMPILER "/r:%OUTDIR%\protobuf-net.dll" /target:exe /recurse:FX11\*.cs "/r:%OUTDIR%\SampleDTO.dll" "/r:%ProgramFiles%\Reference Assemblies\Microsoft\Framework\v3.0\System.Runtime.Serialization.dll"
@cd %OUTDIR%
@test
@cd ..\..