namespace NetworkShareAssistant.Core;

public sealed record NetworkDriveProfile(
    string Server,
    string Share,
    string DriveLetter,
    string? Username,
    bool SaveCredentials,
    bool Persistent)
{
    public string UncPath => NetworkTarget.BuildUnc(Server, Share);
}
