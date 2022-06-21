@echo off
set INCLUDE=
set LIB=
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
echo PATH=%PATH%
echo INCLUDE=%INCLUDE%
echo LIB=%LIB%;%LIBPATH%
