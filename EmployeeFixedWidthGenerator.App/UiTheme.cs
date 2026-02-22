using System.Drawing.Drawing2D;

namespace EmployeeFixedWidthGenerator.App;

internal static class UiTheme
{
    public static readonly Color AppBackground = Color.FromArgb(236, 240, 246);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color Border = Color.FromArgb(216, 223, 232);
    public static readonly Color HeaderText = Color.FromArgb(24, 34, 52);
    public static readonly Color BodyText = Color.FromArgb(66, 81, 102);
    public static readonly Color MutedText = Color.FromArgb(109, 125, 147);
    public static readonly Color Accent = Color.FromArgb(30, 104, 201);
    public static readonly Color AccentHover = Color.FromArgb(24, 89, 176);
    public static readonly Color Success = Color.FromArgb(43, 126, 93);

    public static Font TitleFont => new("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font SubtitleFont => new("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font SectionTitleFont => new("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font BodyFont => new("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

    public static Panel CreateCard(int width, int height)
    {
        var panel = new Panel
        {
            Width = width,
            Height = height,
            BackColor = CardBackground,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 12)
        };

        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var borderPen = new Pen(Border, 1f);
            using var shadowBrush = new SolidBrush(Color.FromArgb(18, 40, 56, 84));

            var shadowRect = new Rectangle(4, 4, panel.Width - 8, panel.Height - 8);
            e.Graphics.FillRoundedRectangle(shadowBrush, shadowRect, 10);

            var rect = new Rectangle(1, 1, panel.Width - 3, panel.Height - 3);
            using var backgroundBrush = new SolidBrush(CardBackground);
            e.Graphics.FillRoundedRectangle(backgroundBrush, rect, 10);
            e.Graphics.DrawRoundedRectangle(borderPen, rect, 10);
        };

        return panel;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;

        button.MouseEnter += (_, _) => button.BackColor = AccentHover;
        button.MouseLeave += (_, _) => button.BackColor = Accent;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.White;
        button.ForeColor = HeaderText;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        button.Cursor = Cursors.Hand;
    }

    public static void StyleInput(Control control)
    {
        control.Font = BodyFont;
        control.ForeColor = HeaderText;
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        var path = new GraphicsPath();

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
