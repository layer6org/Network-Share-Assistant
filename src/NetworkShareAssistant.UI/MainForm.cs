using System.Net;
using NetworkShareAssistant.Core;
using NetworkShareAssistant.Credentials;
using NetworkShareAssistant.Discovery;
using NetworkShareAssistant.Logging;
using NetworkShareAssistant.Network;

namespace NetworkShareAssistant.UI;

public sealed class MainForm : Form
{
    private readonly TextBox _server = new();
    private readonly ComboBox _share = new()
    {
        DropDownStyle = ComboBoxStyle.DropDown
    };
    private readonly TextBox _username = new();
    private readonly TextBox _password = new()
    {
        UseSystemPasswordChar = true
    };
    private readonly CheckBox _saveCredentials = new();
    private readonly CheckBox _persistent = new()
    {
        Checked = true
    };
    private readonly ComboBox _freeDrive = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly Label _status = new()
    {
        AutoSize = true
    };
    private readonly Label _credentialHint = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(105, 105, 110)
    };

    private readonly NetworkDriveService _network = new();
    private readonly CredentialService _credentials = new();
    private readonly ShareEnumerationService _shares = new();
    private readonly NeighborService _neighbors = new();
    private readonly MacVendorLookupService _macVendors = new();
    private readonly NetworkInterfaceService _networkInfo = new();
    private readonly DnsLookupService _dnsLookup = new();
    private readonly FileLogger _logger = new();

    private UiLanguage _language =
        UiText.DetectDefaultLanguage();

    private UiText T => new(_language);

    private Label? _title;
    private Label? _serverLabel;
    private Label? _shareLabel;
    private Label? _usernameLabel;
    private Label? _passwordLabel;
    private Label? _driveLabel;

    private Label? _managementTitle;
    private Label? _analysisTitle;

    private RoundedButton? _searchButton;
    private RoundedButton? _sharesButton;
    private RoundedButton? _connectedButton;
    private RoundedButton? _credentialsButton;
    private RoundedButton? _diagnosticsButton;
    private RoundedButton? _networkInfoButton;
    private RoundedButton? _arpButton;
    private RoundedButton? _nslookupButton;
    private RoundedButton? _connectButton;
    private RoundedButton? _disconnectButton;
    private RoundedButton? _languageButton;
    private RoundedButton? _aboutButton;

    public MainForm()
    {
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(880, 680);
        MinimumSize = new Size(700, 560);
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(246, 246, 248);

        try
        {
            Icon = new Icon(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "app.ico"));
        }
        catch
        {
        }

        L.Current = _language;

        BuildUi();
        ApplyLanguage();
        RefreshDriveLetters();

        _persistent.CheckedChanged += (_, _) =>
        {
            UpdateCredentialBehavior();
        };

        UpdateCredentialBehavior();

        Shown += (_, _) => FitToWorkingArea();
        DpiChanged += (_, _) => FitToWorkingArea();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = BackColor
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = new Padding(0)
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        stack.Controls.Add(
            BuildConnectionCard(),
            0,
            0);

        stack.Controls.Add(
            BuildGroupCard(
                out _managementTitle,
                () =>
                {
                    _searchButton = NewButton("");
                    _sharesButton = NewButton("");
                    _connectedButton = NewButton("");
                    _credentialsButton = NewButton("");

                    _searchButton.Click += (_, _) => OpenDiscovery();
                    _sharesButton.Click += (_, _) => LoadShares();
                    _connectedButton.Click += (_, _) => OpenMappedDrives();
                    _credentialsButton.Click += (_, _) =>
                        new CredentialsDialog(_credentials)
                            .ShowDialog(this);

                    return new Control[]
                    {
                        _searchButton,
                        _sharesButton,
                        _connectedButton,
                        _credentialsButton
                    };
                }),
            0,
            1);

        stack.Controls.Add(
            BuildGroupCard(
                out _analysisTitle,
                () =>
                {
                    _diagnosticsButton = NewButton("");
                    _networkInfoButton = NewButton("");
                    _arpButton = NewButton("");
                    _nslookupButton = NewButton("");

                    _diagnosticsButton.Click += (_, _) => OpenDiagnostics();
                    _networkInfoButton.Click += (_, _) => ShowNetworkInfo();
                    _arpButton.Click += (_, _) => ShowArpTable();
                    _nslookupButton.Click += async (_, _) =>
                        await DoNslookupAsync();

                    return new Control[]
                    {
                        _diagnosticsButton,
                        _networkInfoButton,
                        _arpButton,
                        _nslookupButton
                    };
                }),
            0,
            2);

        scroll.Controls.Add(stack);
        root.Controls.Add(scroll, 0, 1);

        _status.Margin = new Padding(2, 6, 0, 0);
        root.Controls.Add(_status, 0, 2);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        _title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 19F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 30, 32),
            Margin = new Padding(2, 5, 16, 8)
        };

        _languageButton = NewButton("");
        _aboutButton = NewButton("");

        _languageButton.Margin = new Padding(0, 7, 8, 8);
        _aboutButton.Margin = new Padding(0, 7, 8, 8);

        _languageButton.Click += (_, _) =>
        {
            _language =
                _language == UiLanguage.German
                    ? UiLanguage.English
                    : UiLanguage.German;

            L.Current = _language;
            ApplyLanguage();
        };

        _aboutButton.Click += (_, _) => ShowAbout();

        header.Controls.Add(_title);
        header.Controls.Add(_languageButton);
        header.Controls.Add(_aboutButton);

        return header;
    }

    private CardPanel BuildConnectionCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14)
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 8,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        grid.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 210));

        grid.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _serverLabel =
            AddField(grid, 0, _server);

        _shareLabel =
            AddField(grid, 1, _share);

        _usernameLabel =
            AddField(grid, 2, _username);

        _passwordLabel =
            AddField(grid, 3, _password);

        var options = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 2)
        };

        options.Controls.Add(_saveCredentials);
        options.Controls.Add(_persistent);

        grid.Controls.Add(
            new Label { AutoSize = true },
            0,
            4);

        grid.Controls.Add(
            options,
            1,
            4);

        _credentialHint.Margin =
            new Padding(0, 0, 0, 4);

        grid.Controls.Add(
            new Label { AutoSize = true },
            0,
            5);

        grid.Controls.Add(
            _credentialHint,
            1,
            5);

        _driveLabel =
            AddField(grid, 6, _freeDrive);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 0)
        };

        _connectButton = NewButton("");
        _disconnectButton = NewButton("");

        _connectButton.BackColor =
            Color.FromArgb(0, 122, 255);

        _connectButton.ForeColor =
            Color.White;

        _connectButton.Click += (_, _) =>
            ConnectDrive();

        _disconnectButton.Click += (_, _) =>
            OpenMappedDrives();

        actions.Controls.Add(_connectButton);
        actions.Controls.Add(_disconnectButton);

        grid.Controls.Add(
            new Label { AutoSize = true },
            0,
            7);

        grid.Controls.Add(
            actions,
            1,
            7);

        card.Controls.Add(grid);
        return card;
    }

    private CardPanel BuildGroupCard(
        out Label title,
        Func<Control[]> controlsFactory)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };

        title = new Label
        {
            AutoSize = true,
            Font = new Font(
                "Segoe UI",
                10F,
                FontStyle.Bold),
            ForeColor = Color.FromArgb(70, 70, 74),
            Margin = new Padding(0, 0, 0, 6)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Margin = new Padding(0)
        };

        foreach (Control control in controlsFactory())
            buttons.Controls.Add(control);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(buttons, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private static RoundedButton NewButton(
        string text) =>
        new()
        {
            Text = text
        };

    private static Label AddField(
        TableLayoutPanel panel,
        int row,
        Control control)
    {
        var label = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 10, 5)
        };

        control.Dock = DockStyle.Top;
        control.Margin =
            new Padding(0, 2, 0, 5);

        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(control, 1, row);

        return label;
    }

    private void ApplyLanguage()
    {
        Text = T.AppTitle;
        _title!.Text = T.AppTitle;

        _serverLabel!.Text = T.Server;
        _shareLabel!.Text = T.Share;
        _usernameLabel!.Text = T.Username;
        _passwordLabel!.Text = T.Password;
        _driveLabel!.Text = T.DriveLetter;

        _server.PlaceholderText =
            T.ServerExample;

        _username.PlaceholderText =
            _language == UiLanguage.German
                ? "optional – Benutzername"
                : "optional – username";

        _password.PlaceholderText =
            _language == UiLanguage.German
                ? "optional – Passwort"
                : "optional – password";

        _saveCredentials.Text =
            T.SaveCredentials;

        _persistent.Text =
            T.Persistent;

        _credentialHint.Text =
            T.PersistentCredentialHint;

        _connectButton!.Text =
            T.Connect;

        _disconnectButton!.Text =
            T.Disconnect;

        _managementTitle!.Text =
            T.GroupManagement;

        _analysisTitle!.Text =
            T.GroupAnalysis;

        _searchButton!.Text =
            T.Search;

        _sharesButton!.Text =
            T.Shares;

        _connectedButton!.Text =
            T.Connected;

        _credentialsButton!.Text =
            T.Credentials;

        _diagnosticsButton!.Text =
            T.Diagnostics;

        _networkInfoButton!.Text =
            T.NetworkInfo;

        _arpButton!.Text =
            T.Arp;

        _nslookupButton!.Text =
            T.Nslookup;

        _languageButton!.Text =
            T.LanguageSwitch;

        _aboutButton!.Text =
            T.About;
    }

    private void UpdateCredentialBehavior()
    {
        // Persistent mappings using explicit credentials must have a
        // retrievable credential at the next logon. Therefore reconnect
        // implicitly means "save credentials" whenever a username is used.
        if (_persistent.Checked &&
            !string.IsNullOrWhiteSpace(_username.Text))
        {
            _saveCredentials.Checked = true;
        }

        _credentialHint.Visible =
            _persistent.Checked;
    }

    private void RefreshDriveLetters()
    {
        var free =
            _network.GetFreeDriveLetters();

        _freeDrive.DataSource =
            free.ToList();

        if (free.Contains("Z"))
            _freeDrive.SelectedItem = "Z";

        _status.Text =
            _language == UiLanguage.German
                ? $"{free.Count} freie Laufwerksbuchstaben"
                : $"{free.Count} free drive letters";
    }

    private void ConnectDrive()
    {
        try
        {
            string host =
                NetworkTarget.NormalizeHost(
                    _server.Text);

            string share =
                NetworkTarget.NormalizeShare(
                    _share.Text);

            if (_freeDrive.SelectedItem is null)
            {
                throw new InvalidOperationException(
                    T.DriveLetter);
            }

            string? username =
                string.IsNullOrWhiteSpace(
                    _username.Text)
                    ? null
                    : _username.Text.Trim();

            string? password =
                string.IsNullOrEmpty(
                    _password.Text)
                    ? null
                    : _password.Text;

            bool mustStoreCredentials =
                _saveCredentials.Checked ||
                (_persistent.Checked &&
                 !string.IsNullOrWhiteSpace(
                     username));

            if (mustStoreCredentials &&
                string.IsNullOrWhiteSpace(
                    username))
            {
                throw new InvalidOperationException(
                    _language ==
                        UiLanguage.German
                        ? "Zum Speichern der Zugangsdaten muss ein Benutzername angegeben werden."
                        : "A username is required to save credentials.");
            }

            var profile =
                new NetworkDriveProfile(
                    host,
                    share,
                    _freeDrive.SelectedItem.ToString()!,
                    username,
                    mustStoreCredentials,
                    _persistent.Checked);

            // 1. Authenticate and create the persistent mapping first.
            // This lets Windows' authentication package learn/cache the exact
            // target metadata for the server.
            _network.Connect(
                profile,
                password);

            CredentialWriteResult? credentialResult =
                null;

            if (mustStoreCredentials)
            {
                // 2. Store the credential AFTER successful authentication,
                // using LSA target information when available.
                credentialResult =
                    _credentials.SaveForAuthenticatedTarget(
                        host,
                        username!,
                        password ?? string.Empty);

                if (!_credentials.ExistsDomainPassword(
                        host,
                        username))
                {
                    throw new InvalidOperationException(
                        _language ==
                            UiLanguage.German
                            ? "Die Windows-Anmeldeinformation wurde geschrieben, kann aber anschließend nicht wiedergefunden werden."
                            : "The Windows credential was written but could not be found afterwards.");
                }
            }

            // 3. For persistent mappings, simulate the next logon immediately:
            // disconnect the active session without deleting the profile, then
            // reconnect with NULL username/password.
            if (profile.Persistent &&
                mustStoreCredentials)
            {
                ReconnectTestResult test =
                    _network.TestStoredReconnect(
                        profile);

                if (!test.Success)
                {
                    throw new InvalidOperationException(
                        (_language ==
                            UiLanguage.German
                            ? "Reconnect-Selbsttest fehlgeschlagen"
                            : "Reconnect self-test failed") +
                        $": Win32 {test.Win32Error} — {test.Message}");
                }
            }

            string deviceName =
                ResolveDeviceName(host);

            _network.SetExplorerLabel(
                profile.DriveLetter,
                share,
                deviceName);

            _logger.Info(
                $"Connected {profile.DriveLetter}: -> {profile.UncPath}; " +
                $"persistent={profile.Persistent}; " +
                $"credentialStored={mustStoreCredentials}; " +
                $"credentialMethod={credentialResult?.Method ?? "<none>"}; " +
                $"reconnectSelfTest={(profile.Persistent && mustStoreCredentials ? "passed" : "not-required")}");

            _status.Text =
                profile.Persistent &&
                mustStoreCredentials
                    ? (_language ==
                        UiLanguage.German
                        ? "Verbunden. Reconnect-Selbsttest erfolgreich."
                        : "Connected. Reconnect self-test passed.")
                    : (_language ==
                        UiLanguage.German
                        ? "Netzlaufwerk verbunden."
                        : "Network drive connected.");

            RefreshDriveLetters();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void OpenMappedDrives()
    {
        using var dialog =
            new MappedDrivesDialog(
                _network,
                T);

        dialog.ShowDialog(this);

        RefreshDriveLetters();
    }

    private void OpenDiscovery()
    {
        using var dialog =
            new DiscoveryDialog();

        if (dialog.ShowDialog(this) ==
                DialogResult.OK &&
            dialog.SelectedDevice is not null)
        {
            _server.Text =
                dialog.SelectedDevice.Address;
        }
    }

    private void LoadShares()
    {
        try
        {
            var shares =
                _shares.ListShares(
                    NetworkTarget.NormalizeHost(
                        _server.Text));

            _share.Items.Clear();

            foreach (string item in shares)
                _share.Items.Add(item);

            if (_share.Items.Count > 0)
                _share.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void OpenDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(
                _server.Text))
            return;

        using var dialog =
            new DiagnosticsDialog(
                NetworkTarget.NormalizeHost(
                    _server.Text),
                _share.Text.Trim());

        dialog.ShowDialog(this);
    }

    private void ShowNetworkInfo()
    {
        var lines = new List<string>();

        foreach (var item in
                 _networkInfo.GetCurrent())
        {
            lines.Add(
                $"{item.InterfaceName} — {item.Family}");

            lines.Add(
                $"  Address: {item.Address}");

            lines.Add(
                $"  Mask/Prefix: {item.PrefixOrMask}");

            lines.Add(
                $"  Gateway: {item.Gateways}");

            lines.Add(
                $"  DNS: {item.DnsServers}");

            lines.Add(string.Empty);
        }

        new TextResultDialog(
            T.NetworkInfo,
            string.Join(
                Environment.NewLine,
                lines))
            .ShowDialog(this);
    }

    private void ShowArpTable()
    {
        var lines = new List<string>
        {
            _language == UiLanguage.German
                ? "IP-Adresse         MAC-Adresse         Potenzieller Hersteller"
                : "IP address         MAC address         Potential manufacturer",
            new string('-', 92)
        };

        var neighbors = _neighbors
            .GetIpv4Neighbors()
            .Where(x => NeighborService.IsUsableNeighborMac(x.Value))
            .OrderBy(x => ParseIpv4ForSort(x.Key))
            .ToList();

        foreach (var entry in neighbors)
        {
            MacVendorInfo vendor =
                _macVendors.Lookup(entry.Value);

            string manufacturer;

            if (vendor.IsLocallyAdministered)
            {
                manufacturer =
                    _language == UiLanguage.German
                        ? "Lokal administriert / randomisiert"
                        : "Locally administered / randomized";
            }
            else if (vendor.HasOrganization)
            {
                manufacturer =
                    vendor.Registry is null
                        ? vendor.Organization!
                        : $"{vendor.Organization} ({vendor.Registry})";
            }
            else
            {
                manufacturer =
                    _language == UiLanguage.German
                        ? "Unbekannt"
                        : "Unknown";
            }

            lines.Add(
                $"{entry.Key,-18} {entry.Value,-19} {manufacturer}");
        }

        if (neighbors.Count == 0)
        {
            lines.Add(
                _language == UiLanguage.German
                    ? "Keine aufgelösten ARP-Nachbarn gefunden."
                    : "No resolved ARP neighbors found.");
        }

        lines.Add(string.Empty);
        lines.Add(
            _language == UiLanguage.German
                ? "Nur Hosts mit tatsächlich aufgelöster individueller MAC-Adresse werden angezeigt. Null-, Broadcast-, Multicast- und unvollständige Einträge werden ausgeblendet."
                : "Only hosts with an actually resolved individual MAC address are shown. Zero, broadcast, multicast and incomplete entries are hidden.");

        new TextResultDialog(
            T.Arp,
            string.Join(Environment.NewLine, lines))
            .ShowDialog(this);
    }

    private async Task DoNslookupAsync()
    {
        using var prompt =
            new PromptDialog(
                T.Nslookup,
                _language == UiLanguage.German
                    ? "Hostname oder IP-Adresse:"
                    : "Hostname or IP address:",
                "192.168.1.10");

        if (prompt.ShowDialog(this) !=
            DialogResult.OK)
            return;

        try
        {
            string result =
                await _dnsLookup.LookupAsync(
                    prompt.Value);

            new TextResultDialog(
                T.Nslookup,
                result)
                .ShowDialog(this);
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private static uint ParseIpv4ForSort(
        string address)
    {
        if (!IPAddress.TryParse(
                address,
                out IPAddress? ip))
        {
            return uint.MaxValue;
        }

        byte[] bytes =
            ip.GetAddressBytes();

        if (bytes.Length != 4)
            return uint.MaxValue;

        return ((uint)bytes[0] << 24) |
               ((uint)bytes[1] << 16) |
               ((uint)bytes[2] << 8) |
               bytes[3];
    }

    private void ShowAbout()
    {
        using var form = new Form
        {
            Text = T.About,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(470, 250),
            MinimumSize = new Size(420, 220),
            Font = Font,
            AutoScaleMode = AutoScaleMode.Dpi,
            BackColor = Color.FromArgb(246, 246, 248)
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 4
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Network-Share-Assistant",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        var info = new Label
        {
            Text =
                $"Version 1.6.6{Environment.NewLine}" +
                "© Stephan Biedermann",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        var link = new LinkLabel
        {
            Text = "https://github.com/layer6org",
            AutoSize = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = new Padding(0, 0, 0, 12)
        };

        link.LinkClicked += (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/layer6org",
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    form,
                    ex.Message,
                    T.About,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        var ok = NewButton("OK");
        ok.Anchor = AnchorStyles.Right;
        ok.Click += (_, _) => form.Close();

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(info, 0, 1);
        root.Controls.Add(link, 0, 2);
        root.Controls.Add(ok, 0, 3);

        form.Controls.Add(root);
        form.ShowDialog(this);
    }

    private static string ResolveDeviceName(
        string server)
    {
        if (!IPAddress.TryParse(
                server,
                out IPAddress? ip))
            return server;

        try
        {
            return Dns.GetHostEntry(ip)
                .HostName
                .Split('.')[0];
        }
        catch
        {
            return server;
        }
    }

    private void FitToWorkingArea()
    {
        Rectangle work =
            Screen.FromControl(this)
                .WorkingArea;

        int width =
            Math.Min(
                880,
                Math.Max(
                    700,
                    (int)(work.Width * 0.94)));

        int height =
            Math.Min(
                680,
                Math.Max(
                    560,
                    (int)(work.Height * 0.94)));

        Size = new Size(
            width,
            height);

        CenterToScreen();
    }

    private void Fail(Exception ex)
    {
        _logger.Error(
            ex.ToString());

        _status.Text =
            ex.Message;

        MessageBox.Show(
            this,
            ex.Message,
            T.AppTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
