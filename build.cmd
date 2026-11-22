@echo off
REM Build NetworkSwitch.exe with the in-box C# compiler (.NET Framework).
REM No SDK or Visual Studio needed - csc.exe ships with Windows.
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /target:winexe /nologo /out:NetworkSwitch.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll NetworkSwitch.cs
if %ERRORLEVEL%==0 echo Done: NetworkSwitch.exe
