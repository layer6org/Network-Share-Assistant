using NetworkShareAssistant.Core;
using NetworkShareAssistant.Network;

namespace NetworkShareAssistant.UI;

internal sealed class MappedDrivesDialog : Form
{
    private readonly NetworkDriveService _network;
    private readonly UiText _t;

    private readonly CheckedListBox _list =
        new()
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle
        };

    private List<MappedDriveInfo> _items =
        new();

    public MappedDrivesDialog(
        NetworkDriveService network,
        UiText text)
    {
        _network = network;
        _t = text;

        Text = _t.Connected;
        StartPosition =
            FormStartPosition.CenterParent;

        Size = new Size(820, 500);
        MinimumSize = new Size(620, 380);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 3,
            ColumnCount = 1
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));

        var hint = new Label
        {
            AutoSize = true,
            Text =
                _t.Language == UiLanguage.German
                    ? "Laufwerk — Name — UNC-Pfad — Wiederverbinden"
                    : "Drive — Name — UNC path — Reconnect"
        };

        root.Controls.Add(
            hint,
            0,
            0);

        root.Controls.Add(
            _list,
            0,
            1);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true
        };

        var selectAll =
            new RoundedButton
            {
                Text = _t.SelectAll
            };

        var rename =
            new RoundedButton
            {
                Text = _t.Rename
            };

        var disconnect =
            new RoundedButton
            {
                Text = _t.DisconnectSelected
            };

        var refresh =
            new RoundedButton
            {
                Text = _t.Refresh
            };

        selectAll.Click += (_, _) =>
        {
            for (int i = 0;
                 i < _list.Items.Count;
                 i++)
            {
                _list.SetItemChecked(
                    i,
                    true);
            }
        };

        rename.Click += (_, _) =>
            RenameSelected();

        disconnect.Click += (_, _) =>
            DisconnectSelected();

        refresh.Click += (_, _) =>
            LoadItems();

        buttons.Controls.Add(selectAll);
        buttons.Controls.Add(rename);
        buttons.Controls.Add(disconnect);
        buttons.Controls.Add(refresh);

        root.Controls.Add(
            buttons,
            0,
            2);

        Controls.Add(root);

        LoadItems();
    }

    private void LoadItems()
    {
        _items =
            _network.GetMappedNetworkDrives()
                .ToList();

        _list.Items.Clear();

        foreach (var item in _items)
        {
            string persistent =
                item.Persistent
                    ? _t.Yes
                    : _t.No;

            _list.Items.Add(
                $"{item.DriveLetter}:  " +
                $"{item.DisplayName}  |  " +
                $"{item.RemotePath}  |  " +
                $"{_t.Reconnect}: {persistent}");
        }
    }

    private void DisconnectSelected()
    {
        var indexes =
            _list.CheckedIndices
                .Cast<int>()
                .OrderByDescending(i => i)
                .ToList();

        if (indexes.Count == 0 &&
            _list.SelectedIndex >= 0)
        {
            indexes.Add(
                _list.SelectedIndex);
        }

        var errors = new List<string>();

        foreach (int index in indexes)
        {
            var item =
                _items[index];

            try
            {
                _network.Disconnect(
                    item.DriveLetter,
                    item.Persistent);
            }
            catch (Exception ex)
            {
                // Keep going: one failing drive must not block the others
                // nor crash the application.
                errors.Add($"{item.DriveLetter}: {ex.Message}");
            }
        }

        LoadItems();

        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                string.Join(Environment.NewLine, errors),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RenameSelected()
    {
        if (_list.SelectedIndex < 0)
            return;

        var item =
            _items[_list.SelectedIndex];

        using var prompt =
            new PromptDialog(
                _t.Rename,
                _t.Language == UiLanguage.German
                    ? "Neuer Name im Explorer:"
                    : "New Explorer name:",
                item.DisplayName);

        if (prompt.ShowDialog(this) !=
                DialogResult.OK ||
            string.IsNullOrWhiteSpace(
                prompt.Value))
            return;

        try
        {
            _network.SetExplorerCustomLabel(
                item.DriveLetter,
                prompt.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        LoadItems();
    }
}
