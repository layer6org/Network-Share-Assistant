using NetworkShareAssistant.Core;
using NetworkShareAssistant.Discovery;

internal static class Program
{
    private static int Main()
    {
        try
        {
            AssertEqual(
                @"\\10.0.1.111\Transfer",
                NetworkTarget.BuildUnc("10.0.1.111", "Transfer"),
                "IPv4 UNC");

            AssertEqual(
                @"\\nas.example.local\Daten",
                NetworkTarget.BuildUnc("nas.example.local", "Daten"),
                "FQDN UNC");

            AssertEqual(
                @"\\NAS01\Backup",
                NetworkTarget.BuildUnc(@"\\NAS01\\", @"\Backup\"),
                "Normalization");

            AssertEqual(
                "10.0.1.111",
                NetworkTarget.NormalizeHost(@"\\10.0.1.111\\"),
                "Credential target / host normalization");


            string host = NetworkTarget.NormalizeHost(@"\\192.168.1.10\\");
            string unc = NetworkTarget.BuildUnc(host, "Transfer");

            AssertEqual(
                "192.168.1.10",
                host,
                "Credential target exact host");

            AssertEqual(
                @"\\192.168.1.10\Transfer",
                unc,
                "Credential target equals UNC host");

            if (NeighborService.IsUsableNeighborMac("00:00:00:00:00:00"))
                throw new InvalidOperationException("Zero MAC must be rejected.");

            if (NeighborService.IsUsableNeighborMac("FF:FF:FF:FF:FF:FF"))
                throw new InvalidOperationException("Broadcast MAC must be rejected.");

            if (!NeighborService.IsUsableNeighborMac("70:A7:41:68:FE:78"))
                throw new InvalidOperationException("Valid unicast MAC must be accepted.");

            Console.WriteLine("NetworkShareAssistant regression tests: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("NetworkShareAssistant regression tests: FAIL");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void AssertEqual(
        string expected,
        string actual,
        string name)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', actual '{actual}'.");
        }
    }
}
