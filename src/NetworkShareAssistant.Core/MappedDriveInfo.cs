namespace NetworkShareAssistant.Core;

public sealed record MappedDriveInfo(
    string DriveLetter,
    string RemotePath,
    string DisplayName,
    bool Persistent);
