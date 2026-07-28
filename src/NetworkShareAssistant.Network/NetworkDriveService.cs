using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using NetworkShareAssistant.Core;

namespace NetworkShareAssistant.Network;

public sealed class NetworkDriveService
{
    private const int ResourceTypeDisk = 1;
    private const int ConnectUpdateProfile = 0x00000001;
    private const int ErrorMoreData = 234;
    private const int ErrorNotConnected = 2250;

    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfIdList = 0x0000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string? lpLocalName;
        public string? lpRemoteName;
        public string? lpComment;
        public string? lpProvider;
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(
        ref NETRESOURCE resource,
        string? password,
        string? username,
        int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(
        string name,
        int flags,
        bool force);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetGetConnection(
        string localName,
        StringBuilder remoteName,
        ref int length);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(
        uint wEventId,
        uint uFlags,
        IntPtr dwItem1,
        IntPtr dwItem2);

    public void Connect(NetworkDriveProfile profile, string? password)
    {
        string letter = Normalize(profile.DriveLetter);
        string local = $"{letter}:";
        string host = NetworkTarget.NormalizeHost(profile.Server);
        string share = NetworkTarget.NormalizeShare(profile.Share);
        string unc = NetworkTarget.BuildUnc(host, share);

        // Remove only a stale live mapping; do not remove the remembered profile.
        _ = WNetCancelConnection2(local, 0, true);

        var resource = new NETRESOURCE
        {
            dwType = ResourceTypeDisk,
            lpLocalName = local,
            lpRemoteName = unc
        };

        string? user = string.IsNullOrWhiteSpace(profile.Username)
            ? null
            : profile.Username;

        string? pwd = user is null && string.IsNullOrEmpty(password)
            ? null
            : password ?? string.Empty;

        int result = WNetAddConnection2(
            ref resource,
            pwd,
            user,
            profile.Persistent ? ConnectUpdateProfile : 0);

        if (result != 0)
        {
            throw new Win32Exception(
                result,
                $"Verbinden von {unc} fehlgeschlagen.");
        }

        if (profile.Persistent && !IsPersistent(letter))
        {
            throw new InvalidOperationException(
                $"Windows hat die Verbindung {letter}: hergestellt, aber nicht im Benutzerprofil gespeichert.");
        }
    }

    public ReconnectTestResult TestStoredReconnect(
        NetworkDriveProfile profile)
    {
        if (!profile.Persistent)
        {
            return new ReconnectTestResult(
                false,
                0,
                "Mapping is not persistent.");
        }

        string letter =
            Normalize(profile.DriveLetter);

        string local =
            $"{letter}:";

        string host =
            NetworkTarget.NormalizeHost(
                profile.Server);

        string share =
            NetworkTarget.NormalizeShare(
                profile.Share);

        string unc =
            NetworkTarget.BuildUnc(
                host,
                share);

        // Drop only the active connection. Keep HKCU\Network\<letter>
        // and the remembered profile intact.
        int disconnectResult =
            WNetCancelConnection2(
                local,
                0,
                true);

        if (disconnectResult != 0 &&
            disconnectResult != ErrorNotConnected)
        {
            return new ReconnectTestResult(
                false,
                disconnectResult,
                new Win32Exception(
                    disconnectResult)
                    .Message);
        }

        var resource =
            new NETRESOURCE
            {
                dwType = ResourceTypeDisk,
                lpLocalName = local,
                lpRemoteName = unc
            };

        // Critical: this is what logon restoration effectively needs:
        // no explicit username/password supplied by our process.
        int reconnectResult =
            WNetAddConnection2(
                ref resource,
                null,
                null,
                ConnectUpdateProfile);

        if (reconnectResult == 0)
        {
            return new ReconnectTestResult(
                true,
                0,
                "Windows restored the mapping using stored credentials.");
        }

        return new ReconnectTestResult(
            false,
            reconnectResult,
            new Win32Exception(
                reconnectResult)
                .Message);
    }

    public void Disconnect(
        string driveLetter,
        bool removePersistentProfile)
    {
        string local = $"{Normalize(driveLetter)}:";

        int result = WNetCancelConnection2(
            local,
            removePersistentProfile ? ConnectUpdateProfile : 0,
            true);

        if (result != 0 && result != ErrorNotConnected)
        {
            throw new Win32Exception(
                result,
                $"Trennen von {local} fehlgeschlagen.");
        }

        RemoveExplorerLabel(driveLetter);
        RefreshExplorer();
    }

    public IReadOnlyList<MappedDriveInfo> GetMappedNetworkDrives()
    {
        var result = new List<MappedDriveInfo>();

        for (char c = 'A'; c <= 'Z'; c++)
        {
            string letter = c.ToString();
            bool persistent = IsPersistent(letter);
            string? remote = TryGetRemotePath($"{c}:");

            if (string.IsNullOrWhiteSpace(remote) && persistent)
                remote = GetNetworkRegistryValue(letter, "RemotePath");

            if (string.IsNullOrWhiteSpace(remote))
                continue;

            string label =
                GetExplorerLabel(letter, remote)
                ?? GetNetworkRegistryValue(letter, "_LabelFromReg")
                ?? remote;

            result.Add(new MappedDriveInfo(
                letter,
                remote,
                label,
                persistent));
        }

        return result;
    }

    public IReadOnlyList<string> GetFreeDriveLetters()
    {
        var mapped = GetMappedNetworkDrives()
            .Select(x => char.Parse(x.DriveLetter))
            .ToHashSet();

        var local = DriveInfo.GetDrives()
            .Select(x => char.ToUpperInvariant(x.Name[0]))
            .ToHashSet();

        return Enumerable.Range('D', 'Z' - 'D' + 1)
            .Select(x => (char)x)
            .Where(c => !mapped.Contains(c) && !local.Contains(c))
            .Select(c => c.ToString())
            .ToList();
    }

    public void SetExplorerLabel(
        string driveLetter,
        string share,
        string deviceName)
    {
        SetExplorerCustomLabel(
            driveLetter,
            $"{share} ({deviceName})");
    }

    public void SetExplorerCustomLabel(
        string driveLetter,
        string label)
    {
        string letter = Normalize(driveLetter);
        string? remote =
            TryGetRemotePath($"{letter}:")
            ?? GetNetworkRegistryValue(letter, "RemotePath");

        string cleanLabel = label.Trim();

        // Keep compatibility with previous builds.
        using (var networkKey = Registry.CurrentUser.CreateSubKey(
                   $@"Network\{letter}",
                   writable: true))
        {
            networkKey?.SetValue(
                "_LabelFromReg",
                cleanLabel,
                RegistryValueKind.String);
        }

        // Explorer stores mapped-share presentation data in MountPoints2.
        if (!string.IsNullOrWhiteSpace(remote))
        {
            string mountPoint = ToMountPoints2Key(remote);

            using var mountKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\{mountPoint}",
                writable: true);

            mountKey?.SetValue(
                "_LabelFromReg",
                cleanLabel,
                RegistryValueKind.String);
        }

        RefreshExplorer();
    }

    public void RemoveExplorerLabel(string driveLetter)
    {
        string letter = Normalize(driveLetter);
        string? remote =
            TryGetRemotePath($"{letter}:")
            ?? GetNetworkRegistryValue(letter, "RemotePath");

        using (var networkKey = Registry.CurrentUser.OpenSubKey(
                   $@"Network\{letter}",
                   writable: true))
        {
            networkKey?.DeleteValue("_LabelFromReg", false);
        }

        if (!string.IsNullOrWhiteSpace(remote))
        {
            string mountPoint = ToMountPoints2Key(remote);

            using var mountKey = Registry.CurrentUser.OpenSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\{mountPoint}",
                writable: true);

            mountKey?.DeleteValue("_LabelFromReg", false);
        }
    }

    private static string? GetExplorerLabel(
        string driveLetter,
        string remotePath)
    {
        string mountPoint = ToMountPoints2Key(remotePath);

        using var mountKey = Registry.CurrentUser.OpenSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\{mountPoint}",
            writable: false);

        return mountKey?.GetValue("_LabelFromReg") as string;
    }

    private static string ToMountPoints2Key(string uncPath)
    {
        string value = uncPath.Trim();

        while (value.StartsWith(@"\", StringComparison.Ordinal))
            value = value[1..];

        return "##" + value.Replace('\\', '#');
    }

    private static void RefreshExplorer()
    {
        SHChangeNotify(
            ShcneAssocChanged,
            ShcnfIdList,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    private static bool IsPersistent(string letter) =>
        !string.IsNullOrWhiteSpace(
            GetNetworkRegistryValue(letter, "RemotePath"));

    private static string? GetNetworkRegistryValue(
        string letter,
        string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            $@"Network\{Normalize(letter)}",
            writable: false);

        return key?.GetValue(name) as string;
    }

    private static string? TryGetRemotePath(string local)
    {
        int size = 512;
        var buffer = new StringBuilder(size);

        int result = WNetGetConnection(
            local,
            buffer,
            ref size);

        if (result == 0)
            return buffer.ToString();

        if (result == ErrorMoreData)
        {
            buffer = new StringBuilder(size);
            result = WNetGetConnection(
                local,
                buffer,
                ref size);

            if (result == 0)
                return buffer.ToString();
        }

        return null;
    }

    private static string Normalize(string value)
    {
        string normalized =
            value.Trim().TrimEnd(':').ToUpperInvariant();

        if (normalized.Length != 1 ||
            normalized[0] is < 'A' or > 'Z')
        {
            throw new ArgumentException(
                "Ungültiger Laufwerksbuchstabe.",
                nameof(value));
        }

        return normalized;
    }
}


public sealed record ReconnectTestResult(
    bool Success,
    int Win32Error,
    string Message);
