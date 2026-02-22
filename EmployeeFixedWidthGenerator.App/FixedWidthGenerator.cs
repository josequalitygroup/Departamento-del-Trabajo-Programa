using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EmployeeFixedWidthGenerator.App;

internal sealed class FixedWidthGenerator
{
    private static readonly int[] FieldLengths =
    {
        9, 1, 2, 4, 8, 4, 6, 6, 3, 1, 7, 9, 4, 8, 2, 1, 6, 6, 3, 3, 2, 16, 1, 16, 16, 1, 5
    };

    public BuildResult BuildRecord(EmployeeRow row, DateTime now, string batchNumberRaw)
    {
        var parsedName = ParseName(row);
        var fields = new List<FieldPreview>(27);

        string ssnDigits = Regex.Replace(row.Ssn, "[-\\s]", string.Empty);
        if (string.IsNullOrWhiteSpace(ssnDigits) || !ssnDigits.All(char.IsDigit))
        {
            throw new ValidationException(row.RowNumber, "SSN", "SSN must contain only digits after removing dashes/spaces.");
        }

        if (ssnDigits.Length > 9)
        {
            throw new ValidationException(row.RowNumber, "SSN", "SSN has more than 9 digits.");
        }

        string trimestre = row.Trimestre.Trim();
        if (trimestre.Length != 3)
        {
            throw new ValidationException(row.RowNumber, "Trimestre (3 characters)", "Trimestre must be exactly 3 characters after trimming.");
        }

        string salaryField = BuildSalary(row.RowNumber, row.Salary);
        string accountField = BuildAccount(row.RowNumber, row.AccountNumber);
        string batchField = BuildBatch(row.RowNumber, batchNumberRaw);

        string date = now.ToString("yyMMdd", CultureInfo.InvariantCulture);
        string time = now.ToString("HHmmss", CultureInfo.InvariantCulture);

        AddField(fields, 1, FieldLengths[0], FormatField(ssnDigits, 9, Alignment.Right, '0'));
        AddField(fields, 2, FieldLengths[1], " ");
        AddField(fields, 3, FieldLengths[2], "W4");
        AddField(fields, 4, FieldLengths[3], FormatField(parsedName.PaternalLast, 4, Alignment.Left, ' '));
        AddField(fields, 5, FieldLengths[4], "12345678");
        AddField(fields, 6, FieldLengths[5], "SWCA");
        AddField(fields, 7, FieldLengths[6], date);
        AddField(fields, 8, FieldLengths[7], time);
        AddField(fields, 9, FieldLengths[8], FormatField(trimestre, 3, Alignment.Left, ' '));
        AddField(fields, 10, FieldLengths[9], "2");
        AddField(fields, 11, FieldLengths[10], salaryField);
        AddField(fields, 12, FieldLengths[11], FormatField(accountField, 9, Alignment.Left, ' '));
        AddField(fields, 13, FieldLengths[12], "    ");
        AddField(fields, 14, FieldLengths[13], "00000000");
        AddField(fields, 15, FieldLengths[14], "  ");
        AddField(fields, 16, FieldLengths[15], "1");
        AddField(fields, 17, FieldLengths[16], batchField);
        AddField(fields, 18, FieldLengths[17], date);
        AddField(fields, 19, FieldLengths[18], "000");
        AddField(fields, 20, FieldLengths[19], "000");
        AddField(fields, 21, FieldLengths[20], "04");
        AddField(fields, 22, FieldLengths[21], FormatField(parsedName.First, 16, Alignment.Left, ' '));
        AddField(fields, 23, FieldLengths[22], FormatField(parsedName.MiddleInitial, 1, Alignment.Left, ' '));
        AddField(fields, 24, FieldLengths[23], FormatField(parsedName.PaternalLast, 16, Alignment.Left, ' '));
        AddField(fields, 25, FieldLengths[24], FormatField(parsedName.MaternalLast, 16, Alignment.Left, ' '));
        AddField(fields, 26, FieldLengths[25], "N");
        AddField(fields, 27, FieldLengths[26], "     ");

        string line = string.Concat(fields.Select(f => f.Value));
        if (line.Length != 150)
        {
            throw new ValidationException(row.RowNumber, "Line Length", $"Generated line length is {line.Length}, expected 150.");
        }

        return new BuildResult(line, fields);
    }

    public static string FormatField(string? value, int length, Alignment align, char padChar)
    {
        string normalized = value ?? string.Empty;

        if (normalized.Length > length)
        {
            return normalized[..length];
        }

        return align == Alignment.Left
            ? normalized.PadRight(length, padChar)
            : normalized.PadLeft(length, padChar);
    }

    private static string BuildBatch(int rowNumber, string raw)
    {
        string trimmed = (raw ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException(rowNumber, "Batch Number (6)", "Batch number is required.");
        }

        if (trimmed.Length >= 6)
        {
            return trimmed[^6..];
        }

        return trimmed.PadLeft(6, '0');
    }

    private static string BuildSalary(int rowNumber, string salaryRaw)
    {
        string cleaned = salaryRaw.Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new ValidationException(rowNumber, "SALARY", "Salary is required.");
        }

        if (!decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salary) &&
            !decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out salary))
        {
            throw new ValidationException(rowNumber, "SALARY", "Salary is not a valid decimal number.");
        }

        decimal rounded = Math.Round(salary, 2, MidpointRounding.AwayFromZero);
        string withoutDot = rounded.ToString("F2", CultureInfo.InvariantCulture).Replace(".", string.Empty);
        if (withoutDot.Length > 7)
        {
            throw new ValidationException(rowNumber, "SALARY", "Salary is too large for 7-character field.");
        }

        return withoutDot.PadLeft(7, '0');
    }

    private static string BuildAccount(int rowNumber, string accountRaw)
    {
        string account = accountRaw.Trim();
        if (account.EndsWith(".0", StringComparison.Ordinal))
        {
            account = account[..^2];
        }

        if (account.Length < 2)
        {
            throw new ValidationException(rowNumber, "Numero de cuenta patronal", "Account number must have at least 2 characters before dropping the last character.");
        }

        return account[..^1];
    }

    private static ParsedName ParseName(EmployeeRow row)
    {
        string normalized = Regex.Replace(row.FullName.ToUpperInvariant().Trim(), "\\s+", " ");
        string[] tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < 2)
        {
            throw new ValidationException(row.RowNumber, "FULL_NAME", "Full name must contain at least 2 tokens.");
        }

        if (tokens.Length >= 4)
        {
            return new ParsedName(tokens[0], tokens[1][0].ToString(), tokens[2], tokens[3]);
        }

        if (tokens.Length == 3)
        {
            return new ParsedName(tokens[0], tokens[1][0].ToString(), tokens[2], string.Empty);
        }

        return new ParsedName(tokens[0], " ", tokens[1], string.Empty);
    }

    private static void AddField(List<FieldPreview> fields, int fieldNumber, int expectedLength, string value)
    {
        if (value.Length != expectedLength)
        {
            throw new InvalidOperationException($"Field {fieldNumber} has length {value.Length}; expected {expectedLength}.");
        }

        fields.Add(new FieldPreview(fieldNumber, expectedLength, value));
    }
}

internal enum Alignment
{
    Left,
    Right
}

internal sealed class ValidationException : Exception
{
    public ValidationException(int rowNumber, string columnName, string message)
        : base($"Row {rowNumber}, Column '{columnName}': {message}")
    {
    }
}
