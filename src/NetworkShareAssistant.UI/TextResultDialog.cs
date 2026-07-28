namespace NetworkShareAssistant.UI;

internal sealed class TextResultDialog : Form
{
    public TextResultDialog(string title, string text)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 520);
        MinimumSize = new Size(560, 380);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Controls.Add(new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Cascadia Mono", 9F),
            Text = text,
            BackColor = Color.White
        });
    }
}
