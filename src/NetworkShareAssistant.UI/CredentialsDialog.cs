using NetworkShareAssistant.Credentials;

namespace NetworkShareAssistant.UI;

public sealed class CredentialsDialog : Form
{
    private readonly CredentialService _service;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly Button _delete = new() { AutoSize = true };

    public CredentialsDialog(CredentialService service)
    {
        _service = service;
        Text = L.De ? "Gespeicherte Zugangsdaten" : "Saved credentials";
        _delete.Text = L.De ? "Löschen" : "Delete";
        Size = new Size(640, 420); StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(_list, 0, 0); root.Controls.Add(_delete, 0, 1); Controls.Add(root);
        _delete.Click += (_, _) => DeleteSelected(); RefreshEntries();
    }

    private void RefreshEntries() => _list.DataSource = _service.ListStored().ToList();
    private void DeleteSelected()
    {
        if (_list.SelectedItem is not CredentialEntry entry) return;
        string question = L.De ? $"Zugangsdaten für '{entry.Target}' löschen?" : $"Delete credentials for '{entry.Target}'?";
        if (MessageBox.Show(this, question, L.De ? "Bestätigung" : "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            _service.Delete(entry.Target);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RefreshEntries();
    }
}
