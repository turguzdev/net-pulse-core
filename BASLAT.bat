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
echo 📊 Web Dashboard: http://localhost:5000 / http://localhost:5247
echo 📖 OpenAPI / Swagger Docs: http://localhost:5000/swagger
echo.
echo Tarayicinizda acmak icin URL'leri kullanabilirsiniz.
echo Durdurmak icin Ctrl+C tuslarina basin.
echo =====================================================================
dotnet run -c Release --urls "http://localhost:5000"
pause
