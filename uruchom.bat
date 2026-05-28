@echo off
chcp 65001 >nul
title System Zarządzania Warsztatem

echo ========================================
echo System Zarządzania Warsztatem
echo ========================================
echo.

echo Sprawdzanie instalacji .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo BŁĄD: .NET SDK nie jest zainstalowany!
    echo Pobierz i zainstaluj .NET SDK z: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo ✓ .NET SDK zainstalowany
echo.

echo Przywracanie pakietów NuGet...
dotnet restore --verbosity quiet
if errorlevel 1 (
    echo BŁĄD: Nie udało się przywrócić pakietów!
    echo.
    pause
    exit /b 1
)

echo ✓ Pakiety przywrócone
echo.

echo Budowanie projektu...
dotnet build --verbosity quiet
if errorlevel 1 (
    echo BŁĄD: Nie udało się zbudować projektu!
    echo Uruchom 'dotnet build' aby zobaczyć szczegóły.
    echo.
    pause
    exit /b 1
)

echo ✓ Projekt zbudowany
echo.

echo ========================================
echo Uruchamianie aplikacji...
echo ========================================
echo.
echo Domyślne konta:
echo   Administrator: admin / admin123
echo   Mechanik:      mechanic1 / mech123
echo   Recepcja:      recepcja1 / rec123
echo   Magazynier:    magazynier1 / mag123
echo.

dotnet run

echo.
echo Aplikacja została zamknięta.
pause
