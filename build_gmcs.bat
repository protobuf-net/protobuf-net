@echo Building Full...
@rd /s /q Package\Full\mono
@md Package\Full\mono
@call mcs -recurse:protobuf-net\*.cs -out:Package\Full\mono\protobuf-net.dll -target:library -unsafe+ -define:FEAT_COMPILER -define:PLAT_BINARYFORMATTER -doc:Package\Full\mono\protobuf-net.xml -define:FEAT_SERVICEMODEL -define:PLAT_XMLSERIALIZER -r:System.Runtime.Serialization.dll -r:System.ServiceModel.dll -r:System.Configuration.dll /keyfile:ProtoBuf.snk

@echo Building CoreOnly...
@rd /s /q Package\CoreOnly\mono
@md Package\CoreOnly\mono
@call mcs -recurse:protobuf-net\*.cs -out:Package\CoreOnly\mono\protobuf-net.dll -target:library -unsafe+ -define:PLAT_BINARYFORMATTER -doc:Package\CoreOnly\mono\protobuf-net.xml -define:FEAT_SERVICEMODEL -define:PLAT_XMLSERIALIZER -r:System.Runtime.Serialization.dll -r:System.ServiceModel.dll -r:System.Configuration.dll -define:NO_RUNTIME /keyfile:ProtoBuf.snk