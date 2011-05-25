cls
@del protobuf-net.dll
@del gmcs-test.exe

gmcs -recurse:protobuf-net\*.cs -out:protobuf-net.dll -target:library -unsafe+ -define:FEAT_COMPILER -doc:protobuf-net.xml

gmcs -recurse:protobuf-net\*.cs -out:protobuf-net-gmcs.dll -target:library -unsafe+ -define:FEAT_COMPILER

gmcs -recurse:FX11\*.cs -target:exe -out:gmcs-test.exe -define:FEAT_COMPILER -r:protobuf-net-gmcs

mono gmcs-test.exe