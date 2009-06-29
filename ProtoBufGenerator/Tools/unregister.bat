rem @echo off
gacutil.exe /nologo /uf "%~n1"
regasm.exe /nologo /unregister "%1"
exit 0