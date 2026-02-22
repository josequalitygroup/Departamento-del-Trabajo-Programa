namespace EmployeeFixedWidthGenerator.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using (var splash = new SplashForm())
        {
            splash.Show();
            Application.DoEvents();
            Thread.Sleep(1500);
            splash.Close();
        }

        using var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        Application.Run(new MainForm(login.SelectedLanguage));
    }
}
