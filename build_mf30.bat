@cls
@SET MF_VER=v3.0
@SET OUTDIR=build\mf30
@rd %OUTDIR% /Q /S
@md %OUTDIR%
@SET FLIB=%ProgramFiles%\Microsoft .NET Micro Framework\%MF_VER%\Assemblies
@SET FBIN=%ProgramFiles%\Microsoft .NET Micro Framework\%MF_VER%\Tools 
@%SystemRoot%\Microsoft.Net\Framework\v2.0.50727\csc.exe /debug- /optimize+ /noconfig /nostdlib /define:FX11 "/out:%OUTDIR%\protobuf-net.dll" /target:library /unsafe /recurse:protobuf-net\*.cs "/r:%FLIB%\mscorlib.dll" "/r:%FLIB%\system.dll" "/r:%FLIB%\System.IO.dll" /define:MF /define:NO_GENERICS /define:NO_RUNTIME "/r:%FLIB%\Microsoft.SPOT.Native.dll"

@copy build\fx11\CustomerModel.dll "%OUTDIR%"
@copy build\fx11\SampleDTO.dll "%OUTDIR%"

@"%FBIN%\MetaDataProcessor.exe" -loadHints mscorlib "%FLIB%\mscorlib.dll" -parse "%OUTDIR%\protobuf-net.dll" -minimize -compile "%OUTDIR%\protobuf-net.pe"
@"%FBIN%\MetaDataProcessor.exe" -loadHints mscorlib "%FLIB%\mscorlib.dll" -parse "%OUTDIR%\SampleDTO.dll" -minimize -compile "%OUTDIR%\SampleDTO.pe"
@"%FBIN%\MetaDataProcessor.exe" -loadHints mscorlib "%FLIB%\mscorlib.dll" -parse "%OUTDIR%\CustomerModel.dll" -minimize -compile "%OUTDIR%\CustomerModel.pe"

@REM "%MF_BIN%/Microsoft.SPOT.Emulator.Sample.SampleEmulator.exe" /load:HelloWorld.pe /load:"%MF_LIB%\mscorlib.pe" /load:"%MF_LIB%\Microsoft.SPOT.Native.pe" /load:"%MF_LIB%\Microsoft.SPOT.Graphics.pe" /load:"%MF_LIB%\Microsoft.SPOT.Hardware.pe" /load:"%MF_LIB%\Microsoft.SPOT.TinyCore.pe"