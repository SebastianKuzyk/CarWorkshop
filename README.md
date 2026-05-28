# System Zarządzania Warsztatem Samochodowym (WPF)

## Wymagania
- Windows 10 / 11
- .NET 10 SDK ([pobierz](https://dotnet.microsoft.com/download))

Plik bazy SQLite (`car_workshop.db`) z przykładowymi danymi jest dołączony
do repozytorium — aplikacja działa od razu po pobraniu, bez dodatkowej
konfiguracji.

## Uruchomienie

### Najprościej
```
uruchom.bat        # CMD
uruchom.ps1        # PowerShell
```

### Ręcznie z konsoli
```
dotnet restore
dotnet build
dotnet run
```

## Konta testowe

| Rola | Login | Hasło |
|---|---|---|
| Administrator | admin | admin123 |
| Mechanik | mechanik | mechanik123 |
| Recepcja | recepcja | recepcja123 |
| Magazynier | magazynier | magazynier123 |
