using ClosedXML.Excel;

namespace EmployeeFixedWidthGenerator.App;

internal sealed class ExcelEmployeeReader
{
    public static readonly string[] RequiredColumns =
    {
        "FULL_NAME",
        "SSN",
        "SALARY",
        "Numero de cuenta patronal",
        "Trimestre (3 characters)"
    };

    public IReadOnlyList<EmployeeRow> Read(string path)
    {
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.FirstRowUsed() ?? throw new Exception("Excel file has no data.");
        var headerMap = MapHeaders(headerRow);

        var missing = RequiredColumns.Where(c => !headerMap.ContainsKey(Normalize(c))).ToList();
        if (missing.Count > 0)
        {
            throw new Exception($"Missing required columns: {string.Join(", ", missing)}");
        }

        var rows = new List<EmployeeRow>();
        int start = headerRow.RowNumber() + 1;
        int last = worksheet.LastRowUsed()?.RowNumber() ?? start - 1;

        for (int rowNumber = start; rowNumber <= last; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            string fullName = GetCellAsString(row, headerMap, "FULL_NAME");
            string ssn = GetCellAsString(row, headerMap, "SSN");
            string salary = GetCellAsString(row, headerMap, "SALARY");
            string account = GetCellAsString(row, headerMap, "Numero de cuenta patronal");
            string trimestre = GetCellAsString(row, headerMap, "Trimestre (3 characters)");

            bool isEmpty = string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(ssn) &&
                           string.IsNullOrWhiteSpace(salary) && string.IsNullOrWhiteSpace(account) &&
                           string.IsNullOrWhiteSpace(trimestre);

            if (isEmpty)
            {
                continue;
            }

            EnsurePresent(rowNumber, "FULL_NAME", fullName);
            EnsurePresent(rowNumber, "SSN", ssn);
            EnsurePresent(rowNumber, "SALARY", salary);
            EnsurePresent(rowNumber, "Numero de cuenta patronal", account);
            EnsurePresent(rowNumber, "Trimestre (3 characters)", trimestre);

            rows.Add(new EmployeeRow(rowNumber, fullName, ssn, salary, account, trimestre));
        }

        return rows;
    }

    public void GenerateTemplate(string destinationPath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Employees");

        for (int i = 0; i < RequiredColumns.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = RequiredColumns[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        worksheet.Cell(2, 1).Value = "JUAN CARLOS PEREZ LOPEZ";
        worksheet.Cell(2, 2).Value = "123-45-6789";
        worksheet.Cell(2, 3).Value = "1234.56";
        worksheet.Cell(2, 4).Value = "1234567890";
        worksheet.Cell(2, 5).Value = "001";

        worksheet.Columns(1, RequiredColumns.Length).AdjustToContents();
        workbook.SaveAs(destinationPath);
    }

    private static void EnsurePresent(int rowNumber, string columnName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(rowNumber, columnName, "Field is required.");
        }
    }

    private static string GetCellAsString(IXLRow row, Dictionary<string, int> headerMap, string columnName)
    {
        int column = headerMap[Normalize(columnName)];
        var cell = row.Cell(column);
        return cell.GetFormattedString().Trim();
    }

    private static Dictionary<string, int> MapHeaders(IXLRow row)
    {
        var map = new Dictionary<string, int>();
        foreach (var cell in row.CellsUsed())
        {
            string normalized = Normalize(cell.GetString());
            if (!string.IsNullOrEmpty(normalized) && !map.ContainsKey(normalized))
            {
                map[normalized] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    private static string Normalize(string value)
    {
        return string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    }
}
