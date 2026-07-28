using System.Net;

namespace NetworkShareAssistant.Discovery;

internal sealed record Ipv4Network(
    IPAddress Address,
    IPAddress Mask)
{
    public IEnumerable<IPAddress> EnumerateHosts(int maxHosts = 4096)
    {
        byte[] ip = Address.GetAddressBytes();
        byte[] mask = Mask.GetAddressBytes();

        uint ipValue =
            ((uint)ip[0] << 24) |
            ((uint)ip[1] << 16) |
            ((uint)ip[2] << 8) |
            ip[3];

        uint maskValue =
            ((uint)mask[0] << 24) |
            ((uint)mask[1] << 16) |
            ((uint)mask[2] << 8) |
            mask[3];

        uint network = ipValue & maskValue;
        uint broadcast = network | ~maskValue;

        long hostCount = (long)broadcast - network - 1;
        if (hostCount <= 0)
            yield break;

        // Protect the GUI from accidentally scanning huge enterprise/VPN networks.
        if (hostCount > maxHosts)
            hostCount = maxHosts;

        uint current = network + 1;

        for (long i = 0; i < hostCount && current < broadcast; i++, current++)
        {
            byte[] bytes =
            {
                (byte)(current >> 24),
                (byte)(current >> 16),
                (byte)(current >> 8),
                (byte)current
            };

            yield return new IPAddress(bytes);
        }
    }
}
