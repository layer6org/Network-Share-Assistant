using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetworkShareAssistant.Discovery;

public sealed class WakeOnLanService
{
    public async Task SendAsync(
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        byte[] mac = ParseMac(macAddress);
        byte[] packet = new byte[6 + 16 * 6];

        for (int i = 0; i < 6; i++)
            packet[i] = 0xFF;

        for (int i = 1; i <= 16; i++)
            Buffer.BlockCopy(mac, 0, packet, i * 6, 6);

        var broadcasts = GetBroadcastAddresses().Distinct().ToList();

        // Limited broadcast as fallback.
        broadcasts.Add(IPAddress.Broadcast);

        using var udp = new UdpClient();
        udp.EnableBroadcast = true;

        foreach (IPAddress broadcast in broadcasts.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();

            await udp.SendAsync(
                packet,
                new IPEndPoint(broadcast, 9),
                cancellationToken);
        }
    }

    private static byte[] ParseMac(string mac)
    {
        string normalized =
            mac.Replace(":", "")
               .Replace("-", "")
               .Trim();

        if (normalized.Length != 12)
            throw new ArgumentException("Ungültige MAC-Adresse.", nameof(mac));

        var bytes = new byte[6];

        for (int i = 0; i < 6; i++)
            bytes[i] = Convert.ToByte(
                normalized.Substring(i * 2, 2),
                16);

        return bytes;
    }

    private static IEnumerable<IPAddress> GetBroadcastAddresses()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (var ua in nic.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork ||
                    ua.IPv4Mask is null)
                    continue;

                byte[] ip = ua.Address.GetAddressBytes();
                byte[] mask = ua.IPv4Mask.GetAddressBytes();
                byte[] broadcast = new byte[4];

                for (int i = 0; i < 4; i++)
                    broadcast[i] = (byte)(ip[i] | ~mask[i]);

                yield return new IPAddress(broadcast);
            }
        }
    }
}
