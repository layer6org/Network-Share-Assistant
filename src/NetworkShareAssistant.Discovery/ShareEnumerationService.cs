using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NetworkShareAssistant.Discovery;

public sealed class ShareEnumerationService
{
    private const int NerrSuccess = 0;
    private const int MaxPreferredLength = -1;
    private const uint StypeDiskTree = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHARE_INFO_1
    {
        public string shi1_netname;
        public uint shi1_type;
        public string shi1_remark;
    }

    [DllImport(
        "Netapi32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern int NetShareEnum(
        string servername,
        int level,
        out IntPtr bufptr,
        int prefmaxlen,
        out int entriesread,
        out int totalentries,
        ref int resume_handle);

    [DllImport("Netapi32.dll")]
    private static extern int NetApiBufferFree(
        IntPtr buffer);

    public IReadOnlyList<string> ListShares(string server)
    {
        string normalized =
            server.Trim().TrimStart('\\');

        string serverName = $@"\\{normalized}";
        int resume = 0;

        int result = NetShareEnum(
            serverName,
            1,
            out IntPtr buffer,
            MaxPreferredLength,
            out int read,
            out _,
            ref resume);

        if (result != NerrSuccess)
        {
            throw new Win32Exception(
                result,
                $"Freigaben auf {normalized} konnten nicht gelesen werden.");
        }

        var shares = new List<string>();

        try
        {
            int size = Marshal.SizeOf<SHARE_INFO_1>();

            for (int i = 0; i < read; i++)
            {
                IntPtr item =
                    IntPtr.Add(buffer, i * size);

                var info =
                    Marshal.PtrToStructure<SHARE_INFO_1>(item);

                bool diskShare =
                    (info.shi1_type & 0xFFFF) == StypeDiskTree;

                if (diskShare &&
                    !string.IsNullOrWhiteSpace(info.shi1_netname) &&
                    !info.shi1_netname.EndsWith(
                        "$",
                        StringComparison.Ordinal))
                {
                    shares.Add(info.shi1_netname);
                }
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                NetApiBufferFree(buffer);
        }

        return shares
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
