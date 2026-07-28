using System.Drawing.Drawing2D;

namespace NetworkShareAssistant.UI;

internal sealed class RoundedButton : Button
{
    public int Radius { get; set; } = 10;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(238, 238, 240);
        ForeColor = Color.FromArgb(32, 32, 34);
        Padding = new Padding(12, 6, 12, 6);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Cursor = Cursors.Hand;
        Margin = new Padding(0, 0, 8, 8);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;

        using var path = RoundedPath(ClientRectangle, Radius);
        Region = new Region(path);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (Enabled && BackColor != Color.FromArgb(0, 122, 255))
            BackColor = Color.FromArgb(225, 225, 228);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (Enabled && ForeColor != Color.White)
            BackColor = Color.FromArgb(238, 238, 240);
    }

    private static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(2, radius * 2);
        var r = rect;
        r.Width -= 1;
        r.Height -= 1;

        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class CardPanel : Panel
{
    public int Radius { get; set; } = 16;

    public CardPanel()
    {
        BackColor = Color.White;
        Padding = new Padding(18);
        Margin = new Padding(0, 0, 0, 14);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int d = Radius * 2;
        var r = ClientRectangle;
        r.Width -= 1;
        r.Height -= 1;

        using var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        using var pen = new Pen(Color.FromArgb(220, 220, 224));
        e.Graphics.DrawPath(pen, path);
    }
}
