using NetworkShareAssistant.Discovery;

namespace NetworkShareAssistant.UI;

public sealed class DiagnosticsDialog : Form
{
    private readonly TextBox _output = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9F) };
    private readonly string _server, _share;
    private readonly DiscoveryService _discovery = new();
    private readonly ShareEnumerationService _shares = new();

    public DiagnosticsDialog(string server, string share)
    {
        _server = server; _share = share;
        Text = L.De ? "Verbindungsdiagnose" : "Connection diagnostics";
        Size = new Size(720, 460); StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi;
        Controls.Add(_output); Shown += async (_, _) => await RunAsync();
    }

    private async Task RunAsync()
    {
        var lines = new List<string> { $"Server: {_server}", string.Empty };
        bool smb = await _discovery.IsPortOpenAsync(_server, 445, TimeSpan.FromSeconds(2));
        lines.Add(smb ? (L.De ? "✔ SMB TCP 445 erreichbar" : "✔ SMB TCP 445 reachable") : (L.De ? "✖ SMB TCP 445 nicht erreichbar" : "✖ SMB TCP 445 unreachable"));
        string name = await _discovery.ResolveNameAsync(_server);
        lines.Add(L.De ? $"ℹ Gerätename: {name}" : $"ℹ Device name: {name}");
        if (smb)
        {
            try
            {
                var shares = _shares.ListShares(_server);
                lines.Add(L.De ? $"✔ {shares.Count} sichtbare Freigabe(n)" : $"✔ {shares.Count} visible share(s)");
                if (!string.IsNullOrWhiteSpace(_share))
                {
                    bool exists = shares.Contains(_share, StringComparer.OrdinalIgnoreCase);
                    lines.Add(exists ? (L.De ? $"✔ Freigabe '{_share}' sichtbar" : $"✔ Share '{_share}' visible") : (L.De ? $"⚠ Freigabe '{_share}' nicht in der sichtbaren Liste" : $"⚠ Share '{_share}' not in visible list"));
                }
                foreach (string share in shares) lines.Add($"   • {share}");
            }
            catch (Exception ex) { lines.Add(L.De ? $"⚠ Freigaben konnten nicht aufgelistet werden: {ex.Message}" : $"⚠ Shares could not be listed: {ex.Message}"); }
        }
        lines.Add(string.Empty);
        lines.Add(L.De ? "Hinweis: Gast-/Anonymous-SMB kann von Windows-Sicherheitsrichtlinien blockiert werden." : "Note: Guest/anonymous SMB may be blocked by Windows security policy.");
        _output.Lines = lines.ToArray();
    }
}
