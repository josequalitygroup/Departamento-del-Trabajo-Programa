# Employee Fixed-Width TXT Generator (Windows)

Windows WinForms desktop application (.NET 8) that creates a fixed-width `.txt` file where each employee record is exactly **150 characters**.

## New UX additions

- **Splash screen** at startup:
  - `Jose's Department of Labour Uploader`
- **Login screen** before main app opens
  - Username is fixed to `Kiri`
  - Password verification now uses PBKDF2 hashing (password is not stored in plaintext in source code)
  - Basic brute-force protection: temporary lock after repeated failed attempts
- **Language selector** on Login and Main UI:
  - English
  - Español

## Input options

1. Use Excel template (`.xlsx`)
2. Enter employees manually in app

## Required Excel columns

- `FULL_NAME`
- `SSN`
- `SALARY`
- `Numero de cuenta patronal`
- `Trimestre (3 characters)`

## Output naming

Output filename is enforced as `WagesYYQ.txt`.

## Build and make Windows EXE

### Build

```powershell
dotnet restore .\EmployeeFixedWidthGenerator.sln
dotnet build .\EmployeeFixedWidthGenerator.sln -c Release
```

### Publish executable (Windows x64)

```powershell
dotnet publish .\EmployeeFixedWidthGenerator.App\EmployeeFixedWidthGenerator.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Generated EXE location:

`EmployeeFixedWidthGenerator.App\bin\Release\net8.0-windows\win-x64\publish\EmployeeFixedWidthGenerator.App.exe`

## Notes

- Excel handling uses ClosedXML (Excel app not required).
- Generation logic and field validation rules remain intact.

## Beta security hardening

- Password is validated using PBKDF2-SHA256 hash comparison with constant-time check.
- Plaintext password literals were removed from app code.
- Login applies temporary lockout after repeated failed attempts.
- For stronger production protection, add code signing + obfuscation + remote license/auth service.
