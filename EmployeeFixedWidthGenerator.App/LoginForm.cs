namespace EmployeeFixedWidthGenerator.App;

public sealed class LoginForm : Form
{
    private readonly TextBox _user = new() { Width = 220 };
    private readonly TextBox _password = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly ComboBox _language = new() { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _error = new() { ForeColor = Color.Firebrick, AutoSize = true };

    private readonly Label _languageLabel = new() { AutoSize = true };
    private readonly Label _userLabel = new() { AutoSize = true };
    private readonly Label _passwordLabel = new() { AutoSize = true };
    private readonly Button _loginButton = new() { Width = 100, Height = 32 };

    private int _failedAttempts;
    private DateTime _lockedUntilUtc = DateTime.MinValue;

    public AppLanguage SelectedLanguage { get; private set; } = AppLanguage.English;

    public LoginForm()
    {
        Width = 420;
        Height = 300;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _language.Items.AddRange(new object[] { "English", "Español" });
        _language.SelectedIndex = 0;
        _language.SelectedIndexChanged += (_, _) =>
        {
            SelectedLanguage = _language.SelectedIndex == 1 ? AppLanguage.Spanish : AppLanguage.English;
            ApplyLanguage();
        };

        _loginButton.Click += (_, _) => DoLogin();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 2,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(_languageLabel, 0, 0);
        layout.Controls.Add(_language, 1, 0);
        layout.Controls.Add(_userLabel, 0, 1);
        layout.Controls.Add(_user, 1, 1);
        layout.Controls.Add(_passwordLabel, 0, 2);
        layout.Controls.Add(_password, 1, 2);
        layout.Controls.Add(_loginButton, 1, 3);
        layout.Controls.Add(_error, 1, 4);

        Controls.Add(layout);
        AcceptButton = _loginButton;

        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("LoginTitle", SelectedLanguage);
        _languageLabel.Text = Localization.T("Language", SelectedLanguage);
        _userLabel.Text = Localization.T("User", SelectedLanguage);
        _passwordLabel.Text = Localization.T("Password", SelectedLanguage);
        _loginButton.Text = Localization.T("Login", SelectedLanguage);
    }

    private void DoLogin()
    {
        if (DateTime.UtcNow < _lockedUntilUtc)
        {
            int seconds = Math.Max(1, (int)Math.Ceiling((_lockedUntilUtc - DateTime.UtcNow).TotalSeconds));
            _error.Text = string.Format(Localization.T("LockedTryLater", SelectedLanguage), seconds);
            return;
        }

        if (AuthService.Validate(_user.Text, _password.Text))
        {
            _failedAttempts = 0;
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        _failedAttempts++;

        if (_failedAttempts >= 5)
        {
            _lockedUntilUtc = DateTime.UtcNow.AddSeconds(30);
            _failedAttempts = 0;
            _error.Text = string.Format(Localization.T("LockedTryLater", SelectedLanguage), 30);
            return;
        }

        _error.Text = Localization.T("InvalidCredentials", SelectedLanguage);
    }
}
