@echo off
cd /d "%~dp0.."
adb reverse tcp:5000 tcp:5000
dotnet build VitalBand.App\VitalBand.App.csproj -t:Run -f net10.0-android
