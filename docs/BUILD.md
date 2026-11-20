# Build

## Voraussetzungen

- Windows 10/11
- .NET 10 SDK

## Vollständiger Build

```powershell
.\build.ps1
```

Ergebnis:

```text
artifacts\Network-Share-Assistant.exe
```

## Nur native Builds

```powershell
.\build.ps1 -Target Native
```

Dabei werden native x64- und ARM64-Payloads erzeugt. Diese sind interne
Buildbestandteile der Universal-EXE.
