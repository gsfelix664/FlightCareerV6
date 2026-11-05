@echo off
title FlightCareer V6
cd /d "%~dp0"

if exist "src\FlightCareer.UI\bin\Debug\net10.0-windows\FlightCareer.UI.exe" (
    echo Iniciando FlightCareer V6...
    start "" "src\FlightCareer.UI\bin\Debug\net10.0-windows\FlightCareer.UI.exe"
) else (
    echo Compilando e iniciando FlightCareer V6...
    "%USERPROFILE%\.dotnet\dotnet.exe" run --project "src\FlightCareer.UI\FlightCareer.UI.csproj"
)
