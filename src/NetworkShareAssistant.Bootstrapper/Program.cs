using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace NetworkShareAssistant.Bootstrapper;

internal static class Program
{
    private const ushort ImageFileMachineAmd64 = 0x8664;
    private const ushort ImageFileMachineArm64 = 0xAA64;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(
        IntPtr process,
        out ushort processMachine,
        out ushort nativeMachine);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(
        IntPtr hWnd,
        string text,
        string caption,
        uint type);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string runtime = DetectRuntime();

            string destination = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Netzwerkfreigabe-Assistent",
                "Runtime",
                "1.6.7",
                runtime);

            string exe = Path.Combine(destination, "Network-Share-Assistant.exe");
            string marker = Path.Combine(destination, ".payload.sha256");

            using Stream payload = OpenPayload(runtime);
            string hash = Convert.ToHexString(SHA256.HashData(payload));

            // Re-extract whenever the embedded payload differs from what was
            // extracted before. Checking only for the EXE would keep running
            // an outdated payload after a rebuild with the same version, and
            // would never repair an interrupted extraction.
            if (!File.Exists(exe) ||
                !File.Exists(marker) ||
                !string.Equals(
                    File.ReadAllText(marker).Trim(),
                    hash,
                    StringComparison.OrdinalIgnoreCase))
            {
                payload.Position = 0;
                Extract(payload, destination);
                File.WriteAllText(marker, hash);
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = destination,
                UseShellExecute = true
            };

            foreach (string arg in args)
                psi.ArgumentList.Add(arg);

            Process.Start(psi);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBoxW(
                IntPtr.Zero,
                ex.Message,
                "Network-Share-Assistant",
                0x10);

            return 1;
        }
    }

    private static string DetectRuntime()
    {
        if (IsWow64Process2(
                GetCurrentProcess(),
                out ushort processMachine,
                out ushort nativeMachine))
        {
            if (nativeMachine == ImageFileMachineArm64)
                return "win-arm64";

            if (nativeMachine == ImageFileMachineAmd64)
                return "win-x64";

            if (processMachine == ImageFileMachineArm64)
                return "win-arm64";
        }

        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "win-arm64",
            Architecture.X64 => "win-x64",
            _ => throw new PlatformNotSupportedException(
                $"Unsupported Windows architecture: {RuntimeInformation.OSArchitecture}")
        };
    }

    private static Stream OpenPayload(string runtime)
    {
        string resourceName = $"NetworkShareAssistant.Payload.{runtime}.zip";
        Stream? stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

        if (stream is null)
            throw new InvalidOperationException($"Embedded payload missing: {runtime}");

        return stream;
    }

    private static void Extract(Stream stream, string destination)
    {
        string temp = Path.Combine(
            Path.GetTempPath(),
            "Network-Share-Assistant",
            Guid.NewGuid().ToString("N"));

        string zip = Path.Combine(temp, "payload.zip");
        string unpacked = Path.Combine(temp, "payload");

        Directory.CreateDirectory(temp);

        try
        {
            using (var output = File.Create(zip))
                stream.CopyTo(output);

            ZipFile.ExtractToDirectory(zip, unpacked);

            Directory.CreateDirectory(destination);

            foreach (string source in Directory.EnumerateFiles(
                         unpacked, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(unpacked, source);
                string target = Path.Combine(destination, relative);

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target, overwrite: true);
            }
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); }
            catch { }
        }
    }
}
