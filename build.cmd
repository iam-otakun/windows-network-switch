@echo off
REM Build NetworkSwitch.exe with the in-box C# compiler (.NET Framework).
REM No SDK or Visual Studio needed - csc.exe ships with Windows.
REM The app icon ships as icon.ico.b64 (base64 text) so the repo stays
REM text-only; decode it here and hand it to csc as /win32icon.
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
set ICON=
if exist icon.ico.b64 certutil -f -decode icon.ico.b64 app.ico >nul
if exist app.ico set ICON=/win32icon:app.ico
"%CSC%" /target:winexe /nologo %ICON% /out:NetworkSwitch.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll NetworkSwitch.cs
if exist app.ico del app.ico
if %ERRORLEVEL%==0 echo Done: NetworkSwitch.exe
