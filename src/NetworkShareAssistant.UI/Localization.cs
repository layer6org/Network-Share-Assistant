namespace NetworkShareAssistant.UI;

internal static class L
{
    public static UiLanguage Current { get; set; } = UiLanguage.German;
    public static bool De => Current == UiLanguage.German;
    public static string AppTitle => De ? "Netzwerkfreigabe-Assistent" : "Network Share Assistant";
    public static string Server => "FQDN / Server / IP:";
    public static string ServerExample => De ? "z. B. nas.example.local, NAS01 oder 10.0.1.111" : "e.g. nas.example.local, NAS01 or 10.0.1.111";
    public static string Share => De ? "Freigabe:" : "Share:";
    public static string User => De ? "Benutzername:" : "Username:";
    public static string Password => De ? "Passwort:" : "Password:";
    public static string SaveCredentials => De ? "Zugangsdaten speichern" : "Save credentials";
    public static string FreeDrive => De ? "Laufwerksbuchstaben auswählen:" : "Select drive letter:";
    public static string MappedDrive => De ? "Verbundenes Netzlaufwerk:" : "Connected network drive:";
    public static string Search => De ? "Netzwerkspeicher suchen" : "Search network storage";
    public static string LoadShares => De ? "Freigaben laden" : "Load shares";
    public static string Diagnostics => De ? "Diagnose" : "Diagnostics";
    public static string Credentials => De ? "Zugangsdaten verwalten" : "Manage credentials";
    public static string Refresh => De ? "Laufwerke aktualisieren" : "Refresh drives";
    public static string Connect => De ? "Verbinden" : "Connect";
    public static string Disconnect => De ? "Trennen" : "Disconnect";
    public static string About => De ? "Über" : "About";
    public static string LanguageSwitch => De ? "English" : "Deutsch";
    public static string Ready => De ? "Bereit" : "Ready";
}
