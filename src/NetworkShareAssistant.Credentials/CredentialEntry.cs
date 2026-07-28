namespace NetworkShareAssistant.Credentials;

public sealed record CredentialEntry(
    string Target,
    string Username)
{
    public override string ToString() =>
        $"{Target} — {Username}";
}
