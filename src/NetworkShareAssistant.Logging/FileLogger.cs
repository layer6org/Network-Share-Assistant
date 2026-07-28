using NetworkShareAssistant.Common;

namespace NetworkShareAssistant.Logging;

public sealed class FileLogger
{
    private readonly object _sync = new();

    public void Info(string message) => Write("INFO", message);
    public void Warning(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            string file = Path.Combine(
                AppPaths.LogDirectory,
                $"{DateTime.Now:yyyy-MM-dd}.log");

            string line =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

            lock (_sync)
            {
                File.AppendAllText(file, line);
            }
        }
        catch
        {
            // Logging must never break user interaction.
        }
    }
}
