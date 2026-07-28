namespace NetworkShareAssistant.Core;

public static class NetworkTarget
{
    public static string NormalizeHost(string value)
    {
        string host = (value ?? string.Empty)
            .Trim()
            .Trim('\\', '/');

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException(
                "FQDN / Server / IP darf nicht leer sein.",
                nameof(value));
        }

        return host;
    }

    public static string NormalizeShare(string value)
    {
        string share = (value ?? string.Empty)
            .Trim()
            .Trim('\\', '/');

        if (string.IsNullOrWhiteSpace(share))
        {
            throw new ArgumentException(
                "Freigabe darf nicht leer sein.",
                nameof(value));
        }

        return share;
    }

    public static string BuildUnc(string host, string share)
    {
        string normalizedHost = NormalizeHost(host);
        string normalizedShare = NormalizeShare(share);

        // Correct UNC syntax:
        // \\server\share
        return $@"\\{normalizedHost}\{normalizedShare}";
    }
}
