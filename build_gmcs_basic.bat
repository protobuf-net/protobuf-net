cls
del protobuf-net-gmcs-basic.dll
del gmcs-test.exe

gmcs -recurse:protobuf-net\*.cs -out:protobuf-net.dll -target:library -define:NO_RUNTIME -define:FEAT_SAFE

@remo gmcs -recurse:FX11\*.cs -target:exe -out:gmcs-test.exe -define:FEAT_COMPILER -r:protobuf-net-gmcs-basic

@rem mono gmcs-test.exe