using System.Globalization;
using System.Reflection;

namespace NetworkShareAssistant.Discovery;

public sealed record MacVendorInfo(
    string? Organization,
    string? Registry,
    bool IsLocallyAdministered,
    bool IsMulticast)
{
    public bool HasOrganization =>
        !string.IsNullOrWhiteSpace(Organization);
}

public sealed class MacVendorLookupService
{
    private const string ResourceName =
        "NetworkShareAssistant.Discovery.oui-vendors.txt";

    private readonly Dictionary<string, (string Registry, string Organization)> _prefixes;

    public MacVendorLookupService()
    {
        _prefixes = LoadPrefixes();
    }

    public MacVendorInfo Lookup(string? macAddress)
    {
        if (!TryNormalizeMac(macAddress, out string hex, out byte firstOctet))
        {
            return new MacVendorInfo(
                null,
                null,
                false,
                false);
        }

        bool multicast = (firstOctet & 0x01) != 0;
        bool locallyAdministered = (firstOctet & 0x02) != 0;

        // For locally administered/randomized MAC addresses, an IEEE prefix
        // match is not a reliable manufacturer indicator.
        if (locallyAdministered || multicast)
        {
            return new MacVendorInfo(
                null,
                null,
                locallyAdministered,
                multicast);
        }

        // Most specific first: MA-S 36-bit, MA-M 28-bit, MA-L 24-bit.
        foreach (int prefixLength in new[] { 9, 7, 6 })
        {
            if (hex.Length < prefixLength)
                continue;

            string prefix = hex[..prefixLength];

            if (_prefixes.TryGetValue(
                    prefix,
                    out var value))
            {
                return new MacVendorInfo(
                    value.Organization,
                    value.Registry,
                    false,
                    false);
            }
        }

        return new MacVendorInfo(
            null,
            null,
            false,
            false);
    }

    private static Dictionary<string, (string Registry, string Organization)> LoadPrefixes()
    {
        var result = new Dictionary<string, (string, string)>(
            StringComparer.OrdinalIgnoreCase);

        Assembly assembly =
            typeof(MacVendorLookupService).Assembly;

        using Stream? stream =
            assembly.GetManifestResourceStream(ResourceName);

        if (stream is null)
            return result;

        using var reader =
            new StreamReader(stream);

        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();

            if (line.Length == 0 ||
                line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            string[] parts =
                line.Split('|', 3);

            if (parts.Length != 3)
                continue;

            string prefix =
                parts[0].Trim().ToUpperInvariant();

            string registry =
                parts[1].Trim();

            string organization =
                parts[2].Trim();

            if (prefix.Length is not (6 or 7 or 9) ||
                organization.Length == 0)
            {
                continue;
            }

            result[prefix] =
                (registry, organization);
        }

        return result;
    }

    private static bool TryNormalizeMac(
        string? value,
        out string hex,
        out byte firstOctet)
    {
        hex = string.Empty;
        firstOctet = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        hex = new string(
            value.Where(Uri.IsHexDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());

        if (hex.Length != 12)
            return false;

        if (!byte.TryParse(
                hex[..2],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out firstOctet))
        {
            return false;
        }

        return true;
    }
}
