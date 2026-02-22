using System.Text;

namespace EmployeeFixedWidthGenerator.App;

public sealed class MainForm : Form
{
    private AppLanguage _language;

    private readonly RadioButton _uploadMode = new() { AutoSize = true, Checked = true };
    private readonly RadioButton _manualMode = new() { AutoSize = true };

    private readonly TextBox _excelPath = new() { Width = 420, ReadOnly = true };
    private readonly TextBox _outputFolder = new() { Width = 420, ReadOnly = true };
    private readonly TextBox _outputFilename = new() { Width = 220, ReadOnly = true };
    private readonly NumericUpDown _yearSelector = new() { Width = 100, Minimum = 2000, Maximum = 2099 };
    private readonly ComboBox _quarterSelector = new() { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _languageSelector = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _batchNumber = new() { Width = 120, Text = "0000001" };
    private readonly CheckBox _includeTrailingBlank = new() { AutoSize = true };
    private readonly TextBox _status = new() { Multiline = true, ReadOnly = true, Width = 1060, Height = 86, ScrollBars = ScrollBars.Vertical };
    private readonly DataGridView _previewGrid = new() { Width = 1060, Height = 260, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false };
    private readonly DataGridView _manualGrid = new() { Width = 1060, Height = 190, AllowUserToAddRows = true, RowHeadersVisible = false };

    private readonly ExcelEmployeeReader _reader = new();
    private readonly FixedWidthGenerator _generator = new();

    private readonly Panel _excelPanel = UiTheme.CreateCard(1088, 112);
    private readonly Panel _manualPanel = UiTheme.CreateCard(1088, 280);

    private Label? _headerTitle;
    private Label? _headerSubtitle;

    private IReadOnlyList<EmployeeRow> _lastRows = Array.Empty<EmployeeRow>();

    public MainForm(AppLanguage language)
    {
        _language = language;

        Width = 1150;
        Height = 980;
        MinimumSize = new Size(1080, 900);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.AppBackground;

        BuildPreviewGrid();
        BuildManualGrid();

        _quarterSelector.Items.AddRange(new object[] { "1", "2", "3", "4" });
        _languageSelector.Items.AddRange(new object[] { "English", "Español" });
        _languageSelector.SelectedIndex = _language == AppLanguage.Spanish ? 1 : 0;
        _languageSelector.SelectedIndexChanged += (_, _) =>
        {
            _language = _languageSelector.SelectedIndex == 1 ? AppLanguage.Spanish : AppLanguage.English;
            ApplyLanguage();
        };

        var now = DateTime.Now;
        _yearSelector.Value = now.Year;
        _quarterSelector.SelectedItem = GetQuarterFromMonth(now.Month).ToString();

        _yearSelector.ValueChanged += (_, _) => RefreshOutputFilename();
        _quarterSelector.SelectedIndexChanged += (_, _) => RefreshOutputFilename();
        _uploadMode.CheckedChanged += (_, _) => RefreshMode();
        _manualMode.CheckedChanged += (_, _) => RefreshMode();

        BuildLayout();

        RefreshOutputFilename();
        RefreshMode();
        ApplyLanguage();
    }

    private static int GetQuarterFromMonth(int month) => ((month - 1) / 3) + 1;

    private void BuildLayout()
    {
        var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
        Controls.Add(host);

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = 1100
        };

        stack.Controls.Add(BuildHeaderCard());
        stack.Controls.Add(BuildModeCard());
        stack.Controls.Add(BuildExcelPanel());
        stack.Controls.Add(BuildManualPanel());
        stack.Controls.Add(BuildActionsCard());
        stack.Controls.Add(BuildPreviewCard());
        stack.Controls.Add(BuildStatusCard());

        host.Controls.Add(stack);
    }

    private Control BuildHeaderCard()
    {
        var card = UiTheme.CreateCard(1088, 130);
        _headerTitle = new Label { Font = UiTheme.TitleFont, ForeColor = UiTheme.HeaderText, AutoSize = true, Location = new Point(18, 14) };
        _headerSubtitle = new Label { Font = UiTheme.SubtitleFont, ForeColor = UiTheme.MutedText, AutoSize = true, Location = new Point(21, 58) };

        var langLabel = new Label { Name = "lblLangHeader", Font = UiTheme.BodyFont, ForeColor = UiTheme.BodyText, AutoSize = true, Location = new Point(840, 20) };
        _languageSelector.Location = new Point(900, 16);
        UiTheme.StyleInput(_languageSelector);

        card.Controls.Add(_headerTitle);
        card.Controls.Add(_headerSubtitle);
        card.Controls.Add(langLabel);
        card.Controls.Add(_languageSelector);
        return card;
    }

    private Control BuildModeCard()
    {
        var card = UiTheme.CreateCard(1088, 78);
        var title = CreateSectionTitle(Localization.T("InputMode", _language));
        title.Name = "lblModeTitle";
        title.Location = new Point(18, 12);

        var row = new FlowLayoutPanel { AutoSize = true, Location = new Point(18, 40) };
        _uploadMode.Font = UiTheme.BodyFont;
        _manualMode.Font = UiTheme.BodyFont;
        row.Controls.Add(_uploadMode);
        row.Controls.Add(new Label { Width = 22 });
        row.Controls.Add(_manualMode);

        card.Controls.Add(title);
        card.Controls.Add(row);
        return card;
    }

    private Control BuildExcelPanel()
    {
        _excelPanel.Controls.Clear();
        var title = CreateSectionTitle(Localization.T("TemplateUpload", _language));
        title.Name = "lblExcelTitle";
        title.Location = new Point(18, 12);

        var desc = new Label
        {
            Name = "lblExcelDesc",
            Font = UiTheme.SubtitleFont,
            ForeColor = UiTheme.MutedText,
            AutoSize = true,
            Location = new Point(18, 34),
            Text = ""
        };

        var row = new FlowLayoutPanel { AutoSize = true, Location = new Point(18, 62) };
        var browseButton = new Button { Name = "btnBrowseExcel", Width = 92, Height = 32 };
        var generateTemplateButton = new Button { Name = "btnGenerateTemplate", Width = 146, Height = 32 };

        UiTheme.StyleSecondaryButton(browseButton);
        UiTheme.StyleSecondaryButton(generateTemplateButton);
        UiTheme.StyleInput(_excelPath);

        browseButton.Click += OnBrowseExcel;
        generateTemplateButton.Click += OnGenerateTemplate;

        row.Controls.Add(new Label { Name = "lblExcelInput", Width = 140, TextAlign = ContentAlignment.MiddleLeft, Font = UiTheme.BodyFont, ForeColor = UiTheme.BodyText });
        row.Controls.Add(_excelPath);
        row.Controls.Add(browseButton);
        row.Controls.Add(generateTemplateButton);

        _excelPanel.Controls.Add(title);
        _excelPanel.Controls.Add(desc);
        _excelPanel.Controls.Add(row);
        return _excelPanel;
    }

    private Control BuildManualPanel()
    {
        _manualPanel.Controls.Clear();

        var title = CreateSectionTitle(Localization.T("ManualEmployeeEntry", _language));
        title.Name = "lblManualTitle";
        title.Location = new Point(18, 12);

        var desc = new Label
        {
            Name = "lblManualDesc",
            Font = UiTheme.SubtitleFont,
            ForeColor = UiTheme.MutedText,
            AutoSize = true,
            Location = new Point(18, 34),
            Text = ""
        };

        _manualGrid.Location = new Point(18, 68);

        _manualPanel.Controls.Add(title);
        _manualPanel.Controls.Add(desc);
        _manualPanel.Controls.Add(_manualGrid);
        return _manualPanel;
    }

    private Control BuildActionsCard()
    {
        var card = UiTheme.CreateCard(1088, 194);
        var title = CreateSectionTitle(Localization.T("ActionsOutput", _language));
        title.Name = "lblActionsTitle";
        title.Location = new Point(18, 12);

        var controls = new TableLayoutPanel { Location = new Point(18, 40), Width = 1048, Height = 106, ColumnCount = 4, RowCount = 3 };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var browseFolderButton = new Button { Name = "btnBrowseFolder", Width = 92, Height = 30 };
        UiTheme.StyleSecondaryButton(browseFolderButton);
        browseFolderButton.Click += OnBrowseFolder;

        UiTheme.StyleInput(_outputFolder);
        UiTheme.StyleInput(_outputFilename);
        UiTheme.StyleInput(_yearSelector);
        UiTheme.StyleInput(_quarterSelector);
        UiTheme.StyleInput(_batchNumber);
        _includeTrailingBlank.Font = UiTheme.BodyFont;

        controls.Controls.Add(CreateLabel(Localization.T("OutputFolder", _language)), 0, 0);
        controls.Controls.Add(_outputFolder, 1, 0);
        controls.Controls.Add(browseFolderButton, 2, 0);

        var yearQuarterPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        yearQuarterPanel.Controls.Add(CreateLabel(Localization.T("Year", _language)));
        yearQuarterPanel.Controls.Add(_yearSelector);
        yearQuarterPanel.Controls.Add(CreateLabel(Localization.T("Quarter", _language)));
        yearQuarterPanel.Controls.Add(_quarterSelector);
        controls.Controls.Add(yearQuarterPanel, 3, 0);

        controls.Controls.Add(CreateLabel(Localization.T("OutputFilename", _language)), 0, 1);
        controls.Controls.Add(_outputFilename, 1, 1);

        var batchPanel = new FlowLayoutPanel { AutoSize = true };
        batchPanel.Controls.Add(CreateLabel(Localization.T("Batch", _language)));
        batchPanel.Controls.Add(_batchNumber);
        controls.Controls.Add(batchPanel, 3, 1);

        controls.Controls.Add(_includeTrailingBlank, 1, 2);

        var buttonRow = new FlowLayoutPanel { AutoSize = true, Location = new Point(18, 148) };
        var generateButton = new Button { Name = "btnGenerate", Width = 140, Height = 34 };
        var previewButton = new Button { Name = "btnPreview", Width = 190, Height = 34 };
        UiTheme.StylePrimaryButton(generateButton);
        UiTheme.StyleSecondaryButton(previewButton);
        generateButton.Click += (_, _) => Generate();
        previewButton.Click += (_, _) => PreviewFirstRecord();
        buttonRow.Controls.Add(generateButton);
        buttonRow.Controls.Add(previewButton);

        card.Controls.Add(title);
        card.Controls.Add(controls);
        card.Controls.Add(buttonRow);

        return card;
    }

    private Control BuildPreviewCard()
    {
        var card = UiTheme.CreateCard(1088, 328);
        var title = CreateSectionTitle(Localization.T("RecordPreview", _language));
        title.Location = new Point(18, 12);

        var desc = new Label
        {
            Name = "lblPreviewDesc",
            Font = UiTheme.SubtitleFont,
            ForeColor = UiTheme.MutedText,
            AutoSize = true,
            Location = new Point(18, 34)
        };

        _previewGrid.Location = new Point(18, 60);

        card.Controls.Add(title);
        card.Controls.Add(desc);
        card.Controls.Add(_previewGrid);
        return card;
    }

    private Control BuildStatusCard()
    {
        var card = UiTheme.CreateCard(1088, 144);
        var title = CreateSectionTitle(Localization.T("Status", _language));
        title.Location = new Point(18, 12);
        _status.Location = new Point(18, 42);
        _status.BackColor = Color.FromArgb(249, 251, 254);
        _status.ForeColor = UiTheme.BodyText;
        _status.BorderStyle = BorderStyle.FixedSingle;
        _status.Font = new Font("Consolas", 9F, FontStyle.Regular);

        card.Controls.Add(title);
        card.Controls.Add(_status);
        return card;
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("AppTitle", _language);
        if (_headerTitle is not null) _headerTitle.Text = Localization.T("HeaderTitle", _language);
        if (_headerSubtitle is not null) _headerSubtitle.Text = Localization.T("HeaderSubtitle", _language);

        _uploadMode.Text = Localization.T("UseTemplate", _language);
        _manualMode.Text = Localization.T("ManualEntry", _language);
        _includeTrailingBlank.Text = Localization.T("Trailing", _language);

        SetTextByName(this, "lblLangHeader", Localization.T("Language", _language));
        SetTextByName(this, "lblModeTitle", Localization.T("InputMode", _language));
        SetTextByName(this, "lblExcelTitle", Localization.T("TemplateUpload", _language));
        SetTextByName(this, "lblManualTitle", Localization.T("ManualEmployeeEntry", _language));
        SetTextByName(this, "lblActionsTitle", Localization.T("ActionsOutput", _language));
        SetTextByName(this, "lblExcelInput", "Excel input (.xlsx):");
        SetTextByName(this, "lblExcelDesc", _language == AppLanguage.Spanish ? "Suba su plantilla completada o genere una nueva plantilla." : "Upload your completed template or generate a fresh one.");
        SetTextByName(this, "lblManualDesc", _language == AppLanguage.Spanish ? "Ingrese un empleado por fila con todos los campos requeridos." : "Enter one employee per row with all required fields.");
        SetTextByName(this, "lblPreviewDesc", _language == AppLanguage.Spanish ? "Revise campos y longitudes antes de generar." : "Review fields and lengths before generating.");

        SetButtonTextByName(this, "btnBrowseExcel", Localization.T("Browse", _language));
        SetButtonTextByName(this, "btnGenerateTemplate", Localization.T("GenerateTemplate", _language));
        SetButtonTextByName(this, "btnBrowseFolder", Localization.T("Browse", _language));
        SetButtonTextByName(this, "btnGenerate", Localization.T("GenerateTxt", _language));
        SetButtonTextByName(this, "btnPreview", Localization.T("PreviewFirst", _language));

        RefreshOutputFilename();
    }

    private static void SetTextByName(Control root, string name, string text)
    {
        foreach (Control c in root.Controls)
        {
            if (c.Name == name)
            {
                c.Text = text;
                return;
            }
            SetTextByName(c, name, text);
        }
    }

    private static void SetButtonTextByName(Control root, string name, string text)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Button b && b.Name == name)
            {
                b.Text = text;
                return;
            }
            SetButtonTextByName(c, name, text);
        }
    }

    private void BuildPreviewGrid()
    {
        _previewGrid.Columns.Add("FieldNo", "Field #");
        _previewGrid.Columns.Add("Expected", "Expected Length");
        _previewGrid.Columns.Add("Value", "Value");
        _previewGrid.Columns.Add("Actual", "Actual Length");
        StyleGrid(_previewGrid);
        _previewGrid.Columns[0].Width = 70;
        _previewGrid.Columns[1].Width = 140;
        _previewGrid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _previewGrid.Columns[3].Width = 120;
    }

    private void BuildManualGrid()
    {
        _manualGrid.Columns.Add("FULL_NAME", "FULL_NAME");
        _manualGrid.Columns.Add("SSN", "SSN");
        _manualGrid.Columns.Add("SALARY", "SALARY");
        _manualGrid.Columns.Add("NumeroCuenta", "Numero de cuenta patronal");
        _manualGrid.Columns.Add("Trimestre", "Trimestre (3 characters)");
        StyleGrid(_manualGrid);
        _manualGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
        _manualGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(234, 238, 244);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(221, 233, 250);
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.HeaderText;
        grid.DefaultCellStyle.ForeColor = UiTheme.BodyText;
        grid.DefaultCellStyle.Font = UiTheme.BodyFont;
        grid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        grid.RowTemplate.Height = 32;

        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 251);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.HeaderText;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
    }

    private static Label CreateSectionTitle(string text) => new() { Text = text, Font = UiTheme.SectionTitleFont, ForeColor = UiTheme.HeaderText, AutoSize = true };
    private static Label CreateLabel(string text) => new() { Text = text, Font = UiTheme.BodyFont, ForeColor = UiTheme.BodyText, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true, Margin = new Padding(0, 6, 4, 0) };

    private void RefreshMode()
    {
        _excelPanel.Enabled = _uploadMode.Checked;
        _excelPanel.BackColor = _uploadMode.Checked ? UiTheme.CardBackground : Color.FromArgb(245, 247, 251);

        _manualPanel.Enabled = _manualMode.Checked;
        _manualPanel.BackColor = _manualMode.Checked ? UiTheme.CardBackground : Color.FromArgb(245, 247, 251);
    }

    private void OnBrowseExcel(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", Title = "Select employee template" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _excelPath.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_outputFolder.Text))
            {
                _outputFolder.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            }
        }
    }

    private void OnGenerateTemplate(object? sender, EventArgs e)
    {
        try
        {
            using var dialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = "EmployeeTemplate.xlsx", Title = "Save template" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            _reader.GenerateTemplate(dialog.FileName);
            _excelPath.Text = dialog.FileName;
            SetStatus($"{Localization.T("TemplateSuccess", _language)} {dialog.FileName}");
            MessageBox.Show(this, Localization.T("TemplateSuccess", _language), Localization.T("GenerateTemplate", _language), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Template Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Select output folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _outputFolder.Text = dialog.SelectedPath;
    }

    private void RefreshOutputFilename() => _outputFilename.Text = BuildOutputFilename();

    private string BuildOutputFilename()
    {
        int year = (int)_yearSelector.Value;
        string yy = (year % 100).ToString("00");

        string quarter = _quarterSelector.SelectedItem?.ToString() ?? string.Empty;
        if (quarter is not "1" and not "2" and not "3" and not "4") throw new Exception("Quarter must be selected (1-4).");

        return $"Wages{yy}{quarter}.txt";
    }

    private void PreviewFirstRecord()
    {
        try
        {
            var rows = LoadRows();
            if (rows.Count == 0) throw new Exception("No non-empty employee rows found.");

            var preview = _generator.BuildRecord(rows[0], DateTime.Now, _batchNumber.Text);
            _previewGrid.Rows.Clear();
            foreach (var field in preview.Fields) _previewGrid.Rows.Add(field.Number, field.ExpectedLength, field.Value, field.ActualLength);

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
            if (rows.Count == 0) throw new Exception("No non-empty employee rows found.");

            string folder = _outputFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder)) throw new Exception("Output folder is required.");

            string filename = BuildOutputFilename();
            _outputFilename.Text = filename;

            string path = Path.Combine(folder, filename);
            DateTime now = DateTime.Now;
            var lines = new List<string>(rows.Count);
            foreach (var row in rows) lines.Add(_generator.BuildRecord(row, now, _batchNumber.Text).Line);

            string content = string.Join("\r\n", lines);
            if (_includeTrailingBlank.Checked) content += "\r\n";

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
        if (_uploadMode.Checked)
        {
            string path = _excelPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(path)) throw new Exception("Excel input path is required when using template mode.");
            if (!File.Exists(path)) throw new Exception($"Excel file not found: {path}");
            _lastRows = _reader.Read(path);
            return _lastRows;
        }

        var rows = new List<EmployeeRow>();
        for (int i = 0; i < _manualGrid.Rows.Count; i++)
        {
            var gridRow = _manualGrid.Rows[i];
            if (gridRow.IsNewRow) continue;

            string fullName = (gridRow.Cells[0].Value?.ToString() ?? string.Empty).Trim();
            string ssn = (gridRow.Cells[1].Value?.ToString() ?? string.Empty).Trim();
            string salary = (gridRow.Cells[2].Value?.ToString() ?? string.Empty).Trim();
            string account = (gridRow.Cells[3].Value?.ToString() ?? string.Empty).Trim();
            string trimestre = (gridRow.Cells[4].Value?.ToString() ?? string.Empty).Trim();

            bool isEmpty = string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(ssn) && string.IsNullOrWhiteSpace(salary) && string.IsNullOrWhiteSpace(account) && string.IsNullOrWhiteSpace(trimestre);
            if (isEmpty) continue;

            int rowNumber = i + 2;
            EnsureManualField(rowNumber, "FULL_NAME", fullName);
            EnsureManualField(rowNumber, "SSN", ssn);
            EnsureManualField(rowNumber, "SALARY", salary);
            EnsureManualField(rowNumber, "Numero de cuenta patronal", account);
            EnsureManualField(rowNumber, "Trimestre (3 characters)", trimestre);

            rows.Add(new EmployeeRow(rowNumber, fullName, ssn, salary, account, trimestre));
        }

        _lastRows = rows;
        return _lastRows;
    }

    private static void EnsureManualField(int rowNumber, string columnName, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationException(rowNumber, columnName, "Field is required.");
    }

    private void SetStatus(string message) => _status.Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
}
