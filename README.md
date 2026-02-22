# Employee Fixed-Width TXT Generator (Windows)

Windows WinForms desktop application (.NET 8) that reads an Excel `.xlsx` employee template and generates a fixed-width `.txt` file where each employee record is exactly **150 characters**.

## Required Excel columns

The app matches headers case-insensitively and normalizes extra spaces. These columns are required:

- `FULL_NAME`
- `SSN`
- `SALARY`
- `Numero de cuenta patronal`
- `Trimestre (3 characters)`

If any required column is missing, generation is blocked with an error.

## Row behavior

- Completely empty rows are skipped.
- Non-empty rows must contain all required fields.
- First validation error stops generation (no partial output).

## Record format

Each record has 27 fields and total length 150 characters. Output uses UTF-8 without BOM and CRLF (`\r\n`) newlines.

### Output file naming (updated)

The app now enforces this filename format:

- `WagesYYQ.txt`

Where:
- `YY` = last two digits of selected year.
- `Q` = selected quarter (`1`..`4`).

Examples:
- Year 2025, Quarter 1 -> `Wages251.txt`
- Year 2026, Quarter 4 -> `Wages264.txt`

The UI includes **Year** and **Quarter** selectors, and filename is auto-generated/read-only.

### Key transformations

- **SSN**: remove dashes/spaces, must be numeric, max 9 digits, right-pad with zeros to 9 (right-aligned with `0`).
- **SALARY**: parse decimal, round to 2 decimals (standard rounding), remove decimal point, left-pad with `0` to 7; error if > 7 chars.
- **Account number** (`Numero de cuenta patronal`): trim, remove trailing `.0` if present, require len >= 2, then drop last character, format to field length 9 (left aligned).
- **Batch Number (6)** UI setting:
  - default: `0000001`
  - enforced to 6 chars by taking rightmost 6 when length >= 6 (e.g. `0000001` -> `000001`)
  - shorter values are left-padded with `0`
- **Name parsing from FULL_NAME**:
  - normalize spaces, uppercase, split by space tokens
  - 4+ tokens: first=t1, middle initial=t2[0], paternal=t3, maternal=t4
  - 3 tokens: first=t1, middle initial=t2[0], paternal=t3, maternal empty
  - 2 tokens: first=t1, middle initial blank, paternal=t2, maternal empty
  - <2 tokens: error

## UI workflow

1. Select Excel file (`.xlsx`)
2. Select output folder
3. Select **Year** and **Quarter**
4. Optionally adjust **Batch Number (6)** and trailing empty line option
5. Click **Generate**

Optional: click **Preview first record** to inspect all 27 fields (field #, expected length, value, actual length).

## Build & publish (Windows x64)

From repository root:

```powershell
dotnet restore .\EmployeeFixedWidthGenerator.sln
dotnet build .\EmployeeFixedWidthGenerator.sln -c Release
```

Self-contained publish for Windows x64:

```powershell
dotnet publish .\EmployeeFixedWidthGenerator.App\EmployeeFixedWidthGenerator.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Published executable will be in:

`EmployeeFixedWidthGenerator.App\bin\Release\net8.0-windows\win-x64\publish\`

## Notes

- Excel is read with **ClosedXML**, so Microsoft Excel does not need to be installed.
- All validation errors include Excel row number and column context.
