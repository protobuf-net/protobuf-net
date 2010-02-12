@cls
@del fx11-test.exe
@%SystemRoot%\Microsoft.Net\Framework\v1.1.4322\csc.exe /out:protobuf-net-11.dll /define:FX11 /target:library /unsafe /recurse:protobuf-net\*.cs
@%SystemRoot%\Microsoft.Net\Framework\v1.1.4322\csc.exe /out:fx11-test.exe /reference:protobuf-net-11.dll /target:exe /recurse:FX11\*.cs
@fx11-test.exe