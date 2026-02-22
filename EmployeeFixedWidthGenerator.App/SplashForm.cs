namespace EmployeeFixedWidthGenerator.App;

public sealed class SplashForm : Form
{
    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 640;
        Height = 300;
        BackColor = Color.FromArgb(22, 34, 56);

        var title = new Label
        {
            Text = "Jose's Department of Labour Uploader",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        Controls.Add(title);
    }
}
