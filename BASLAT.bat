@echo off
title NetPulse Core (.NET 8 Enterprise Platform)
chcp 65001 >nul
cls
echo =====================================================================
echo   ⚡ NetPulse Core - High-Performance .NET 8 Microservice Platform
echo =====================================================================
echo.
echo [1/2] Proje yapilandiriliyor ve derleniyor...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [HATA] Derleme basarisiz oldu. Lutfen .NET 8 SDK'nin yuklu oldugunu dogrulayin.
    pause
    exit /b %errorlevel%
)
echo.
echo [2/2] NetPulse Core baslatiliyor...
echo.
echo 📊 Web Dashboard: http://localhost:5055
echo 📖 OpenAPI / Swagger Docs: http://localhost:5055/swagger
echo.
echo Tarayicinizda aciliyor...
start http://localhost:5055
echo Durdurmak icin Ctrl+C tuslarina basin.
echo =====================================================================
dotnet run -c Release --no-build
pause
