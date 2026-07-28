using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetworkShareAssistant.Core;

namespace NetworkShareAssistant.Discovery;

public sealed class DiscoveryService
{
    private readonly NeighborService _neighbors = new();
    private readonly MacVendorLookupService _vendors = new();

    public async Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Netzwerkadapter werden ermittelt...");

        var localAddresses = GetLocalAddresses()
            .Select(x => x.Address.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        // Neighbor / ARP cache first.
        var neighbors = _neighbors.GetIpv4Neighbors();

        foreach (string address in neighbors.Keys)
        {
            if (!localAddresses.Contains(address))
                candidates.Add(address);
        }

        // Then use the real IPv4 subnet masks.
        foreach (var network in GetLocalNetworks())
        {
            foreach (IPAddress host in network.EnumerateHosts())
            {
                string address = host.ToString();

                if (!localAddresses.Contains(address))
                    candidates.Add(address);
            }
        }

        progress?.Report(
            $"{candidates.Count} Adresse(n) werden auf SMB TCP 445 geprüft...");

        var found = new List<DiscoveredDevice>();
        using var semaphore = new SemaphoreSlim(96);

        int processed = 0;

        var tasks = candidates.Select(async address =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                bool smb = await IsPortOpenAsync(
                    address,
                    445,
                    TimeSpan.FromMilliseconds(700),
                    cancellationToken);

                if (!smb)
                    return;

                string name = await ResolveNameAsync(address);

                string? mac = neighbors.TryGetValue(
                    address,
                    out string? knownMac)
                    ? knownMac
                    : null;

                MacVendorInfo vendor =
                    _vendors.Lookup(mac);

                string? manufacturer =
                    vendor.IsLocallyAdministered
                        ? "Local / randomized MAC"
                        : vendor.IsMulticast
                            ? "Multicast MAC"
                            : vendor.Organization;

                lock (found)
                {
                    found.Add(new DiscoveredDevice(
                        address,
                        name,
                        mac,
                        manufacturer,
                        true));
                }
            }
            finally
            {
                int done = Interlocked.Increment(ref processed);

                if (done % 50 == 0)
                    progress?.Report(
                        $"{done}/{candidates.Count} geprüft...");

                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        progress?.Report(
            $"{found.Count} SMB-Gerät(e) gefunden.");

        return found
            .DistinctBy(x => x.Address)
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Address)
            .ToList();
    }

    public async Task<bool> IsPortOpenAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var client = new TcpClient();

        using var linked =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        linked.CancelAfter(timeout);

        try
        {
            await client.ConnectAsync(
                host,
                port,
                linked.Token);

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> ResolveNameAsync(string hostOrIp)
    {
        if (!IPAddress.TryParse(hostOrIp, out _))
            return hostOrIp;

        try
        {
            IPHostEntry entry =
                await Dns.GetHostEntryAsync(hostOrIp);

            return entry.HostName.Split('.')[0];
        }
        catch
        {
            return hostOrIp;
        }
    }

    private static IEnumerable<UnicastIPAddressInformation> GetLocalAddresses()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n =>
                n.GetIPProperties().UnicastAddresses)
            .Where(a =>
                a.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(a.Address) &&
                !a.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal));
    }

    private static IEnumerable<Ipv4Network> GetLocalNetworks()
    {
        foreach (var address in GetLocalAddresses())
        {
            if (address.IPv4Mask is null)
                continue;

            yield return new Ipv4Network(
                address.Address,
                address.IPv4Mask);
        }
    }
}
