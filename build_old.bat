@echo off
rem Don't put the value into quotes if uncommenting the following line
rem set PDN_HOME=C:\Program Files\Paint.NET

if "%PDN_HOME%a"=="a" goto NO_PDN_HOME
csc /target:library /r:"%PDN_HOME%\PaintDotNet.Base.dll","%PDN_HOME%\PaintDotNet.Core.dll","%PDN_HOME%\PaintDotNet.Data.dll" PdnJsonFileTypeOld.cs
exit 0

:NO_PDN_HOME
echo !!! Before building the plugin, set PDN_HOME variable to location where Paint.NET is installed !!!
exit 1
