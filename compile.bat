@echo off
cd /d "%~dp0"
echo Compiling UniversalTestLab.exe ...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF @build.rsp
echo.
echo Exit code: %ERRORLEVEL%
if "%ERRORLEVEL%"=="0" (echo OK - exe updated) else (echo FAILED - check above)
pause
