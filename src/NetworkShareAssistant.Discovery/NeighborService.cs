using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NetworkShareAssistant.Discovery;

public sealed class NeighborService
{
    private static readonly Regex IpRegex = new(
        @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b",
        RegexOptions.Compiled);

    private static readonly Regex MacRegex = new(
        @"\b[0-9A-Fa-f]{2}(?:[:-][0-9A-Fa-f]{2}){5}\b",
        RegexOptions.Compiled);

    public Dictionary<string, string> GetIpv4Neighbors()
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string line in Run("arp.exe", "-a"))
        {
            Match ipMatch = IpRegex.Match(line);
            Match macMatch = MacRegex.Match(line);

            if (!ipMatch.Success || !macMatch.Success)
                continue;

            string ip = ipMatch.Value;
            string mac = NormalizeMac(macMatch.Value);

            if (!IPAddress.TryParse(ip, out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
            {
                continue;
            }

            if (!IsUsableNeighborMac(mac))
                continue;

            result[ip] = mac;
        }

        return result;
    }

    public static bool IsUsableNeighborMac(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        string normalized = NormalizeMac(macAddress);
        string hex = normalized.Replace(":", string.Empty);

        if (hex.Length != 12)
            return false;

        if (hex == "000000000000" ||
            hex == "FFFFFFFFFFFF")
        {
            return false;
        }

        if (!byte.TryParse(
                hex[..2],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte firstOctet))
        {
            return false;
        }

        // Multicast/group address: not an individual ARP host.
        if ((firstOctet & 0x01) != 0)
            return false;

        return true;
    }

    private static string NormalizeMac(string value)
    {
        string hex = new string(
            value
                .Where(Uri.IsHexDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());

        if (hex.Length != 12)
            return value.ToUpperInvariant();

        return string.Join(
            ":",
            Enumerable.Range(0, 6)
                .Select(i => hex.Substring(i * 2, 2)));
    }

    private static IEnumerable<string> Run(
        string fileName,
        string arguments)
    {
        Process? process = null;

        try
        {
            process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

            if (process is null)
                yield break;

            while (!process.StandardOutput.EndOfStream)
            {
                string? line = process.StandardOutput.ReadLine();
                if (line is not null)
                    yield return line;
            }

            process.WaitForExit(3000);
        }
        finally
        {
            process?.Dispose();
        }
    }
}
