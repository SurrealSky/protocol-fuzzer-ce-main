@echo off

Setlocal
set PYTHON=py -2.7
@%PYTHON% -x "%~dp0waf" %*
@exit /b %ERRORLEVEL%
