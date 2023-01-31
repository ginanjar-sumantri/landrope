@echo off
echo Publishing the web for Ubuntu...
dotnet publish -c Debug -r ubuntu-x64 -o d:\pub\landrope-linux

echo Done!

pause
