using NetworkShareAssistant.Core;
using NetworkShareAssistant.Discovery;

namespace NetworkShareAssistant.UI;

public sealed class DiscoveryDialog : Form
{
    private readonly ListBox _devices = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly Label _status = new() { AutoSize = true };
    private readonly Button _search = new() { AutoSize = true };
    private readonly Button _wake = new() { Text = "Wake-on-LAN", AutoSize = true };
    private readonly Button _select = new() { AutoSize = true };
    private readonly DiscoveryService _discovery = new();
    private readonly WakeOnLanService _wakeOnLan = new();
    private readonly CancellationTokenSource _closing = new();
    public DiscoveredDevice? SelectedDevice { get; private set; }

    public DiscoveryDialog()
    {
        Text = L.Search;
        _status.Text = L.Ready;
        _search.Text = L.De ? "Neu suchen" : "Search again";
        _select.Text = L.De ? "Auswählen" : "Select";
        Size = new Size(720, 460);
        MinimumSize = new Size(620, 400);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(_status, 0, 0);
        root.Controls.Add(_devices, 0, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(_select); buttons.Controls.Add(_wake); buttons.Controls.Add(_search);
        root.Controls.Add(buttons, 0, 2); Controls.Add(root);

        _search.Click += async (_, _) => await SearchAsync();
        _select.Click += (_, _) => AcceptSelection();
        _wake.Click += async (_, _) => await WakeSelectedAsync();
        _devices.DoubleClick += (_, _) => AcceptSelection();
        _devices.SelectedIndexChanged += (_, _) => {
            var d = _devices.SelectedItem as DiscoveredDevice;
            _select.Enabled = d is not null;
            _wake.Enabled = d is not null && !string.IsNullOrWhiteSpace(d.MacAddress);
        };
        Shown += async (_, _) => await SearchAsync();

        // Stop a running scan when the dialog is closed; otherwise the scan
        // keeps running and later touches disposed controls.
        FormClosing += (_, _) => _closing.Cancel();
        Disposed += (_, _) => _closing.Dispose();
    }

    private async Task SearchAsync()
    {
        SetBusy(true);
        try
        {
            var devices = await _discovery.DiscoverAsync(
                cancellationToken: _closing.Token);
            if (IsDisposed) return;
            _devices.DataSource = devices.ToList();
            _status.Text = L.De
                ? $"{devices.Count} Gerät(e) mit offenem SMB TCP 445 gefunden. MAC/Hersteller werden soweit verfügbar angezeigt."
                : $"{devices.Count} device(s) with SMB TCP 445 found. MAC/vendor are shown when available.";
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested) { }
        catch (Exception ex) { if (!IsDisposed) _status.Text = ex.Message; }
        finally { if (!IsDisposed) SetBusy(false); }
    }

    private async Task WakeSelectedAsync()
    {
        if (_devices.SelectedItem is not DiscoveredDevice d || string.IsNullOrWhiteSpace(d.MacAddress)) return;
        try
        {
            await _wakeOnLan.SendAsync(d.MacAddress);
            _status.Text = L.De ? $"Magic Packet an {d.MacAddress} gesendet." : $"Magic Packet sent to {d.MacAddress}.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Wake-on-LAN", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void AcceptSelection()
    {
        if (_devices.SelectedItem is not DiscoveredDevice d) return;
        SelectedDevice = d; DialogResult = DialogResult.OK; Close();
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        _search.Enabled = !busy;
        _select.Enabled = !busy && _devices.SelectedItem is not null;
        _wake.Enabled = !busy && _devices.SelectedItem is DiscoveredDevice d && !string.IsNullOrWhiteSpace(d.MacAddress);
    }
}
