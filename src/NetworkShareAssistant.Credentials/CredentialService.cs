using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace NetworkShareAssistant.Credentials;

public sealed class CredentialService
{
    private const uint CredTypeGeneric = 1;
    private const uint CredTypeDomainPassword = 2;
    private const uint CredPersistEnterprise = 3;
    private const uint CredTiServerFormatUnknown = 1;

    private const int ErrorNotFound = 1168;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string? TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL_TARGET_INFORMATION
    {
        public string? TargetName;
        public string? NetbiosServerName;
        public string? DnsServerName;
        public string? NetbiosDomainName;
        public string? DnsDomainName;
        public string? DnsTreeName;
        public string? PackageName;
        public uint Flags;
        public uint CredTypeCount;
        public IntPtr CredTypes;
    }

    [DllImport(
        "advapi32.dll",
        EntryPoint = "CredWriteW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern bool CredWrite(
        ref CREDENTIAL credential,
        uint flags);

    [DllImport(
        "advapi32.dll",
        EntryPoint = "CredWriteDomainCredentialsW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern bool CredWriteDomainCredentials(
        ref CREDENTIAL_TARGET_INFORMATION targetInfo,
        ref CREDENTIAL credential,
        uint flags);

    [DllImport(
        "advapi32.dll",
        EntryPoint = "CredDeleteW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern bool CredDelete(
        string target,
        uint type,
        uint flags);

    [DllImport(
        "advapi32.dll",
        EntryPoint = "CredEnumerateW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern bool CredEnumerate(
        string? filter,
        uint flags,
        out uint count,
        out IntPtr credentials);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(
        IntPtr buffer);

    public CredentialWriteResult SaveForAuthenticatedTarget(
        string target,
        string username,
        string password)
    {
        string normalizedTarget =
            NormalizeTarget(target);

        byte[] bytes =
            Encoding.Unicode.GetBytes(password);

        IntPtr blob =
            Marshal.AllocCoTaskMem(
                Math.Max(1, bytes.Length));

        try
        {
            if (bytes.Length > 0)
            {
                Marshal.Copy(
                    bytes,
                    0,
                    blob,
                    bytes.Length);
            }

            var credential =
                new CREDENTIAL
                {
                    Type = CredTypeDomainPassword,
                    TargetName = normalizedTarget,
                    UserName = username,
                    CredentialBlob = blob,
                    CredentialBlobSize =
                        (uint)bytes.Length,
                    Persist = CredPersistEnterprise,
                    Comment = "Network-Share-Assistant"
                };

            // Always store the credential under exactly the host name the
            // user entered (the same host used in the UNC path). Windows'
            // cached target information (CredGetTargetInfoW) may pick a
            // different name (FQDN vs. NetBIOS), which made the stored name
            // inconsistent and broke the subsequent lookup.
            var directTarget =
                new CREDENTIAL_TARGET_INFORMATION
                {
                    TargetName = normalizedTarget,
                    DnsServerName = normalizedTarget,
                    Flags = CredTiServerFormatUnknown
                };

            if (CredWriteDomainCredentials(
                    ref directTarget,
                    ref credential,
                    0))
            {
                DeleteType(
                    normalizedTarget,
                    CredTypeGeneric,
                    ignoreNotFound: true);

                return new CredentialWriteResult(
                    normalizedTarget,
                    "CredWriteDomainCredentialsW / direct target",
                    true);
            }

            int domainError =
                Marshal.GetLastWin32Error();

            // Last-resort compatibility path matching cmdkey /add semantics as
            // closely as possible without exposing the password on a command line.
            if (CredWrite(
                    ref credential,
                    0))
            {
                DeleteType(
                    normalizedTarget,
                    CredTypeGeneric,
                    ignoreNotFound: true);

                return new CredentialWriteResult(
                    normalizedTarget,
                    $"CredWriteW fallback after domain error {domainError}",
                    true);
            }

            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Windows-Anmeldeinformationen für '{normalizedTarget}' konnten nicht gespeichert werden.");
        }
        finally
        {
            if (blob != IntPtr.Zero)
            {
                for (int i = 0; i < bytes.Length; i++)
                    Marshal.WriteByte(blob, i, 0);

                Marshal.FreeCoTaskMem(blob);
            }

            Array.Clear(
                bytes,
                0,
                bytes.Length);
        }
    }

    public void Delete(string target)
    {
        string normalizedTarget =
            NormalizeTarget(target);

        DeleteType(
            normalizedTarget,
            CredTypeDomainPassword,
            ignoreNotFound: true);

        DeleteType(
            normalizedTarget,
            CredTypeGeneric,
            ignoreNotFound: true);
    }

    public bool ExistsDomainPassword(
        string target,
        string? username = null)
    {
        string normalizedTarget =
            NormalizeTarget(target);

        foreach (CredentialEntry entry in ListStored())
        {
            if (!entry.Target.Equals(
                    normalizedTarget,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (username is null ||
                entry.Username.Equals(
                    username,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<CredentialEntry> ListStored()
    {
        var entries =
            new Dictionary<string, CredentialEntry>(
                StringComparer.OrdinalIgnoreCase);

        if (!CredEnumerate(
                null,
                0,
                out uint count,
                out IntPtr buffer))
        {
            int error =
                Marshal.GetLastWin32Error();

            if (error == ErrorNotFound)
                return Array.Empty<CredentialEntry>();

            throw new Win32Exception(error);
        }

        try
        {
            for (int i = 0;
                 i < count;
                 i++)
            {
                IntPtr itemPtr =
                    Marshal.ReadIntPtr(
                        buffer,
                        i * IntPtr.Size);

                var credential =
                    Marshal.PtrToStructure<CREDENTIAL>(
                        itemPtr);

                if ((credential.Type !=
                        CredTypeDomainPassword &&
                     credential.Type !=
                        CredTypeGeneric) ||
                    string.IsNullOrWhiteSpace(
                        credential.TargetName))
                {
                    continue;
                }

                string key =
                    credential.TargetName!;

                if (!entries.ContainsKey(key) ||
                    credential.Type ==
                        CredTypeDomainPassword)
                {
                    entries[key] =
                        new CredentialEntry(
                            key,
                            credential.UserName ??
                            string.Empty);
                }
            }
        }
        finally
        {
            CredFree(buffer);
        }

        return entries.Values
            .OrderBy(
                x => x.Target,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void DeleteType(
        string target,
        uint type,
        bool ignoreNotFound)
    {
        if (CredDelete(
                target,
                type,
                0))
        {
            return;
        }

        int error =
            Marshal.GetLastWin32Error();

        if (ignoreNotFound &&
            error == ErrorNotFound)
        {
            return;
        }

        throw new Win32Exception(error);
    }

    private static string NormalizeTarget(
        string target)
    {
        string value =
            target.Trim().Trim('\\');

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Credential target must not be empty.",
                nameof(target));
        }

        return value;
    }
}

public sealed record CredentialWriteResult(
    string Target,
    string Method,
    bool Success);
