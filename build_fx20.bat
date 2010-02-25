@cls
@del protobuf-net-20.dll
@del fx20-test.exe
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /out:protobuf-net-20.dll /define:FEAT_COMPILER /target:library /unsafe /recurse:protobuf-net\*.cs
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /out:fx20-test.exe /define:FEAT_COMPILER /reference:protobuf-net-20.dll /target:exe /recurse:FX11\*.cs
@fx20-test.exe