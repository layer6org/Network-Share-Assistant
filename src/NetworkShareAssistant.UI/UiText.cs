using System.Globalization;

namespace NetworkShareAssistant.UI;

internal enum UiLanguage
{
    German,
    English
}

internal sealed class UiText
{
    public UiLanguage Language { get; }

    public UiText(UiLanguage language) =>
        Language = language;

    public static UiLanguage DetectDefaultLanguage() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals(
            "de",
            StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.German
            : UiLanguage.English;

    private string P(string de, string en) =>
        Language == UiLanguage.German ? de : en;

    public string AppTitle => P("Netzwerkfreigabe-Assistent", "Network Share Assistant");
    public string Server => "FQDN / Server / IP";
    public string ServerExample => P(
        "z. B. nas.example.local, NAS01 oder 192.168.1.10",
        "e.g. nas.example.local, NAS01 or 192.168.1.10");
    public string Share => P("Freigabe", "Share");
    public string Username => P("Benutzername", "Username");
    public string Password => P("Passwort", "Password");
    public string SaveCredentials => P("Zugangsdaten speichern", "Save credentials");
    public string Persistent => P("Bei Anmeldung wiederverbinden", "Reconnect at sign-in");
    public string DriveLetter => P("Laufwerksbuchstaben auswählen", "Select drive letter");

    public string Connect => P("Netzlaufwerk verbinden", "Connect network drive");
    public string Disconnect => P("Netzlaufwerk trennen", "Disconnect network drive");

    public string GroupManagement => P("Verwaltung", "Management");
    public string GroupAnalysis => P("Analyse", "Analysis");

    public string Connected => P("Verbundene Netzlaufwerke", "Connected network drives");
    public string Search => P("Netzwerkspeicher suchen", "Search network storage");
    public string Shares => P("Freigaben laden", "Load shares");
    public string Diagnostics => P("Diagnose", "Diagnostics");
    public string Credentials => P("Zugangsdaten verwalten", "Manage credentials");
    public string NetworkInfo => P("Netzwerkinformationen", "Network information");
    public string Arp => P("ARP-Tabelle", "ARP table");
    public string Nslookup => "nslookup";

    public string Refresh => P("Aktualisieren", "Refresh");
    public string Rename => P("Umbenennen", "Rename");
    public string SelectAll => P("Alle auswählen", "Select all");
    public string DisconnectSelected => P("Ausgewählte trennen", "Disconnect selected");
    public string Reconnect => P("Wiederverbinden", "Reconnect");
    public string Yes => P("Ja", "Yes");
    public string No => P("Nein", "No");

    public string About => P("Über", "About");
    public string LanguageSwitch =>
        Language == UiLanguage.German ? "English" : "Deutsch";

    public string PersistentCredentialHint => P(
        "Bei Wiederverbinden werden angegebene Zugangsdaten automatisch gespeichert.",
        "When reconnect is enabled, supplied credentials are saved automatically.");
}
