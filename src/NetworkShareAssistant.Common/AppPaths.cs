namespace NetworkShareAssistant.Common;

public static class AppPaths
{
    public static string AppDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Netzwerkfreigabe-Assistent");

    public static string LogDirectory =>
        Path.Combine(AppDataDirectory, "Logs");
}
