using System.Net;

namespace NetworkShareAssistant.Discovery;

public sealed class DnsLookupService
{
    public async Task<string> LookupAsync(string query)
    {
        query = query.Trim();
        if (query.Length == 0) throw new ArgumentException("Query must not be empty.");

        IPHostEntry entry = IPAddress.TryParse(query, out var ip)
            ? await Dns.GetHostEntryAsync(ip)
            : await Dns.GetHostEntryAsync(query);

        var lines = new List<string> { $"Name: {entry.HostName}" };
        foreach (var alias in entry.Aliases) lines.Add($"Alias: {alias}");
        foreach (var address in entry.AddressList) lines.Add($"Address: {address}");
        return string.Join(Environment.NewLine, lines);
    }
}
