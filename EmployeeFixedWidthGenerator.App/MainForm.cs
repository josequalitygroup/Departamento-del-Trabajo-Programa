using System.Text;

namespace EmployeeFixedWidthGenerator.App;

public sealed class MainForm : Form
{
    private readonly TextBox _excelPath = new() { Width = 520 };
    private readonly TextBox _outputFolder = new() { Width = 520 };
    private readonly TextBox _outputFilename = new() { Width = 280, ReadOnly = true };
    private readonly NumericUpDown _yearSelector = new() { Width = 100, Minimum = 2000, Maximum = 2099 };
    private readonly ComboBox _quarterSelector = new() { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _batchNumber = new() { Width = 120, Text = "0000001" };
    private readonly CheckBox _includeTrailingBlank = new() { Text = "Include trailing empty line", AutoSize = true };
    private readonly TextBox _status = new() { Multiline = true, ReadOnly = true, Width = 760, Height = 160, ScrollBars = ScrollBars.Vertical };
    private readonly DataGridView _previewGrid = new() { Width = 760, Height = 260, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

    private readonly ExcelEmployeeReader _reader = new();
    private readonly FixedWidthGenerator _generator = new();

    private IReadOnlyList<EmployeeRow> _lastRows = Array.Empty<EmployeeRow>();

    public MainForm()
    {
        Text = "Employee Fixed-Width TXT Generator";
        Width = 820;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        _previewGrid.Columns.Add("FieldNo", "Field #");
        _previewGrid.Columns.Add("Expected", "Expected Length");
        _previewGrid.Columns.Add("Value", "Value");
        _previewGrid.Columns.Add("Actual", "Actual Length");

        _quarterSelector.Items.AddRange(new object[] { "1", "2", "3", "4" });

        var now = DateTime.Now;
        _yearSelector.Value = now.Year;
        _quarterSelector.SelectedItem = GetQuarterFromMonth(now.Month).ToString();

        _yearSelector.ValueChanged += (_, _) => RefreshOutputFilename();
        _quarterSelector.SelectedIndexChanged += (_, _) => RefreshOutputFilename();

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        layout.Controls.Add(BuildFileRow("Excel input (.xlsx):", _excelPath, OnBrowseExcel));
        layout.Controls.Add(BuildFileRow("Output folder:", _outputFolder, OnBrowseFolder));
        layout.Controls.Add(BuildYearQuarterRow());
        layout.Controls.Add(BuildSimpleRow("Output filename:", _outputFilename));
        layout.Controls.Add(BuildSimpleRow("Batch Number (6):", _batchNumber));
        layout.Controls.Add(_includeTrailingBlank);

        var buttonRow = new FlowLayoutPanel { AutoSize = true };
        var generateButton = new Button { Text = "Generate", Width = 140, Height = 36 };
        var previewButton = new Button { Text = "Preview first record", Width = 180, Height = 36 };
        generateButton.Click += (_, _) => Generate();
        previewButton.Click += (_, _) => PreviewFirstRecord();
        buttonRow.Controls.Add(generateButton);
        buttonRow.Controls.Add(previewButton);

        layout.Controls.Add(buttonRow);
        layout.Controls.Add(new Label { Text = "Preview", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        layout.Controls.Add(_previewGrid);
        layout.Controls.Add(new Label { Text = "Status", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        layout.Controls.Add(_status);

        Controls.Add(layout);

        RefreshOutputFilename();
    }

    private static int GetQuarterFromMonth(int month) => ((month - 1) / 3) + 1;

    private Control BuildYearQuarterRow()
    {
        var panel = new FlowLayoutPanel { AutoSize = true };
        panel.Controls.Add(new Label { Text = "Year:", Width = 160, TextAlign = ContentAlignment.MiddleLeft });
        panel.Controls.Add(_yearSelector);
        panel.Controls.Add(new Label { Text = "Quarter:", Width = 60, TextAlign = ContentAlignment.MiddleLeft });
        panel.Controls.Add(_quarterSelector);
        return panel;
    }

    private static Control BuildSimpleRow(string label, Control control)
    {
        var panel = new FlowLayoutPanel { AutoSize = true };
        panel.Controls.Add(new Label { Text = label, Width = 160, TextAlign = ContentAlignment.MiddleLeft });
        panel.Controls.Add(control);
        return panel;
    }

    private static Control BuildFileRow(string label, TextBox textbox, EventHandler browseAction)
    {
        var panel = new FlowLayoutPanel { AutoSize = true };
        var button = new Button { Text = "Browse...", Width = 90 };
        button.Click += browseAction;
        panel.Controls.Add(new Label { Text = label, Width = 160, TextAlign = ContentAlignment.MiddleLeft });
        panel.Controls.Add(textbox);
        panel.Controls.Add(button);
        return panel;
    }

    private void OnBrowseExcel(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            Title = "Select employee template"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _excelPath.Text = dialog.FileName;

            if (string.IsNullOrWhiteSpace(_outputFolder.Text))
            {
                _outputFolder.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            }
        }
    }

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Select output folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputFolder.Text = dialog.SelectedPath;
        }
    }

    private void RefreshOutputFilename()
    {
        _outputFilename.Text = BuildOutputFilename();
    }

    private string BuildOutputFilename()
    {
        int year = (int)_yearSelector.Value;
        string yy = (year % 100).ToString("00");

        string quarter = _quarterSelector.SelectedItem?.ToString() ?? string.Empty;
        if (quarter is not "1" and not "2" and not "3" and not "4")
        {
            throw new Exception("Quarter must be selected (1-4).");
        }

        return $"Wages{yy}{quarter}.txt";
    }

    private void PreviewFirstRecord()
    {
        try
        {
            var rows = LoadRows();
            if (rows.Count == 0)
            {
                throw new Exception("No non-empty employee rows found.");
            }

            var preview = _generator.BuildRecord(rows[0], DateTime.Now, _batchNumber.Text);
            _previewGrid.Rows.Clear();
            foreach (var field in preview.Fields)
            {
                _previewGrid.Rows.Add(field.Number, field.ExpectedLength, field.Value, field.ActualLength);
            }

            SetStatus($"Preview generated for row {rows[0].RowNumber}. Line length: {preview.Line.Length}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Generate()
    {
        try
        {
            var rows = LoadRows();
            if (rows.Count == 0)
            {
                throw new Exception("No non-empty employee rows found.");
            }

            string folder = _outputFolder.Text.Trim();

            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new Exception("Output folder is required.");
            }

            string filename = BuildOutputFilename();
            _outputFilename.Text = filename;

            string path = Path.Combine(folder, filename);
            DateTime now = DateTime.Now;
            var lines = new List<string>(rows.Count);

            foreach (var row in rows)
            {
                var record = _generator.BuildRecord(row, now, _batchNumber.Text);
                lines.Add(record.Line);
            }

            string content = string.Join("\r\n", lines);
            if (_includeTrailingBlank.Checked)
            {
                content += "\r\n";
            }

            File.WriteAllText(path, content, new UTF8Encoding(false));
            SetStatus($"Success. Generated {lines.Count} records at: {path}");
            MessageBox.Show(this, $"Generated {lines.Count} record(s).\nOutput: {filename}", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private IReadOnlyList<EmployeeRow> LoadRows()
    {
        string path = _excelPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new Exception("Excel input path is required.");
        }

        if (!File.Exists(path))
        {
            throw new Exception($"Excel file not found: {path}");
        }

        _lastRows = _reader.Read(path);
        return _lastRows;
    }

    private void SetStatus(string message)
    {
        _status.Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
    }
}
