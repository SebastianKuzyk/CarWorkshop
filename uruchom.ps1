# Skrypt uruchamiający System Zarządzania Warsztatem
# Autor: Sebastian Kuzyk, Dariusz Wais

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "System Zarządzania Warsztatem" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Sprawdź czy .NET SDK jest zainstalowany
Write-Host "Sprawdzanie instalacji .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "BŁĄD: .NET SDK nie jest zainstalowany!" -ForegroundColor Red
    Write-Host "Pobierz i zainstaluj .NET SDK z: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}

Write-Host "✓ Znaleziono .NET SDK wersja: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Sprawdź czy jesteśmy w odpowiednim katalogu
if (-not (Test-Path "CarWorkshopWPF.csproj")) {
    Write-Host "BŁĄD: Nie znaleziono pliku projektu!" -ForegroundColor Red
    Write-Host "Upewnij się, że uruchamiasz skrypt z katalogu projektu." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}

# Przywróć pakiety NuGet
Write-Host "Przywracanie pakietów NuGet..." -ForegroundColor Yellow
dotnet restore --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "BŁĄD: Nie udało się przywrócić pakietów!" -ForegroundColor Red
    Write-Host ""
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}

Write-Host "✓ Pakiety przywrócone pomyślnie" -ForegroundColor Green
Write-Host ""

# Zbuduj projekt
Write-Host "Budowanie projektu..." -ForegroundColor Yellow
dotnet build --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "BŁĄD: Nie udało się zbudować projektu!" -ForegroundColor Red
    Write-Host "Uruchom 'dotnet build' aby zobaczyć szczegóły błędu." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}

Write-Host "✓ Projekt zbudowany pomyślnie" -ForegroundColor Green
Write-Host ""

# Uruchom aplikację
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Uruchamianie aplikacji..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Domyślne konta:" -ForegroundColor Yellow
Write-Host "  Administrator: admin / admin123" -ForegroundColor White
Write-Host "  Mechanik:      mechanic1 / mech123" -ForegroundColor White
Write-Host "  Recepcja:      recepcja1 / rec123" -ForegroundColor White
Write-Host "  Magazynier:    magazynier1 / mag123" -ForegroundColor White
Write-Host ""

dotnet run

Write-Host ""
Write-Host "Aplikacja została zamknięta." -ForegroundColor Yellow
