namespace EmployeeFixedWidthGenerator.App;

internal sealed record EmployeeRow(
    int RowNumber,
    string FullName,
    string Ssn,
    string Salary,
    string AccountNumber,
    string Trimestre);

internal sealed record ParsedName(
    string First,
    string MiddleInitial,
    string PaternalLast,
    string MaternalLast);

internal sealed record FieldPreview(int Number, int ExpectedLength, string Value)
{
    public int ActualLength => Value.Length;
}

internal sealed record BuildResult(string Line, IReadOnlyList<FieldPreview> Fields);
