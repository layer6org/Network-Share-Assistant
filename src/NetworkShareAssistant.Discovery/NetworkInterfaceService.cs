using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetworkShareAssistant.Discovery;

public sealed record NetworkAddressInfo(
    string InterfaceName, string Family, string Address,
    string PrefixOrMask, string Gateways, string DnsServers);

public sealed class NetworkInterfaceService
{
    public IReadOnlyList<NetworkAddressInfo> GetCurrent()
    {
        var rows = new List<NetworkAddressInfo>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                 n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var props = nic.GetIPProperties();
            string gateways = string.Join(", ",
                props.GatewayAddresses.Select(g => g.Address)
                    .Where(a => !IPAddress.IsLoopback(a)));
            string dns = string.Join(", ", props.DnsAddresses);

            foreach (var ua in props.UnicastAddresses)
            {
                if (IPAddress.IsLoopback(ua.Address)) continue;

                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    rows.Add(new(nic.Name, "IPv4", ua.Address.ToString(),
                        ua.IPv4Mask?.ToString() ?? "", gateways, dns));
                else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    rows.Add(new(nic.Name, "IPv6", ua.Address.ToString(),
                        $"/{ua.PrefixLength}", gateways, dns));
            }
        }

        return rows;
    }
}
