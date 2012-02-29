@rem cls
@rem @del protobuf-net.dll
@rem del gmcs-test.exe

@gmcs -recurse:protobuf-net\*.cs -out:protobuf-net.dll -target:library -unsafe+ -define:FEAT_COMPILER -define:PLAT_BINARYFORMATTER -doc:protobuf-net.xml -define:FEAT_SERVICEMODEL -define:PLAT_XMLSERIALIZER -r:System.Runtime.Serialization.dll -r:System.ServiceModel.dll /keyfile:ProtoBuf.snk

@rem gmcs -recurse:protobuf-net\*.cs -out:protobuf-net-gmcs.dll -target:library -unsafe+ -define:FEAT_COMPILER

@rem gmcs -recurse:FX11\*.cs -target:exe -out:gmcs-test.exe -define:FEAT_COMPILER -r:protobuf-net-gmcs

@rem mono gmcs-test.exe