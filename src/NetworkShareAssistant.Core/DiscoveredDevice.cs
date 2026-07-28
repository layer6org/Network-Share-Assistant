namespace NetworkShareAssistant.Core;

public sealed record DiscoveredDevice(
    string Address,
    string DisplayName,
    string? MacAddress,
    string? Manufacturer,
    bool SmbAvailable)
{
    public override string ToString()
    {
        string name = string.Equals(
            Address,
            DisplayName,
            StringComparison.OrdinalIgnoreCase)
            ? Address
            : $"{DisplayName} ({Address})";

        var parts = new List<string> { name };

        if (!string.IsNullOrWhiteSpace(MacAddress))
            parts.Add(MacAddress);

        if (!string.IsNullOrWhiteSpace(Manufacturer))
            parts.Add(Manufacturer);

        return string.Join(" — ", parts);
    }
}
