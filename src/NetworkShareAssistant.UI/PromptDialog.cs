namespace NetworkShareAssistant.UI;

internal sealed class PromptDialog : Form
{
    private readonly TextBox _text = new() { Dock = DockStyle.Top };
    public string Value => _text.Text.Trim();

    public PromptDialog(string title, string prompt, string initialValue = "")
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(520, 190);
        MinimumSize = new Size(420, 170);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 3,
            ColumnCount = 1
        };

        root.Controls.Add(new Label { Text = prompt, AutoSize = true }, 0, 0);
        _text.Text = initialValue;
        root.Controls.Add(_text, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };

        var ok = new RoundedButton { Text = "OK" };
        var cancel = new RoundedButton { Text = "Cancel" };

        ok.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
