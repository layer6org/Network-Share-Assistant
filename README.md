# Network-Share-Assistant

**Network-Share-Assistant** ist ein kleines Windows-Programm, das den Umgang mit Netzlaufwerken und Geräten im lokalen Netzwerk vereinfacht.

**Network-Share-Assistant** is a small Windows application that makes it easier to use network drives and find devices on your local network.

---

# Deutsch

## Was kann Network-Share-Assistant?

Network-Share-Assistant hilft dabei, freigegebene Ordner auf einem NAS, Server oder anderen SMB-Gerät als Laufwerk in Windows einzubinden. Viele Aufgaben, für die sonst Windows-Menüs oder Kommandozeilenbefehle nötig wären, können direkt im Programm erledigt werden.

### Netzlaufwerke verbinden

Du kannst eine Netzwerkfreigabe wie ein normales Laufwerk in Windows einbinden.

Dazu werden im Wesentlichen nur benötigt:

- Name oder IP-Adresse des Servers bzw. NAS
- Name der Freigabe
- gewünschter Laufwerksbuchstabe
- falls erforderlich Benutzername und Passwort

Verbindungen ohne Benutzername oder Passwort werden ebenfalls unterstützt, sofern die Freigabe dies erlaubt.

Das Laufwerk kann entweder nur für die aktuelle Nutzung verbunden oder bei jeder Windows-Anmeldung automatisch wiederhergestellt werden.

### Zugangsdaten sicher in Windows speichern

Wenn eine Freigabe Zugangsdaten benötigt, kann Network-Share-Assistant diese im Windows-Anmeldeinformationsmanager speichern.

Dadurch kann Windows ein dauerhaft verbundenes Netzlaufwerk nach einer erneuten Anmeldung oder einem Neustart wieder mit den gespeicherten Zugangsdaten verwenden.

### Verbundene Netzlaufwerke verwalten

Network-Share-Assistant zeigt vorhandene Netzlaufwerke an. Du kannst einzelne, mehrere oder alle ausgewählten Verbindungen trennen.

Außerdem wird angezeigt, ob Windows ein Netzlaufwerk bei der Anmeldung wiederherstellen soll.

### Netzlaufwerke umbenennen

Ein verbundenes Laufwerk kann einen verständlichen Namen erhalten, zum Beispiel:

`Daten (WDCloud-134)`

Dieser Name wird auch im Windows Explorer verwendet.

### Geräte und Netzwerkspeicher finden

Network-Share-Assistant kann das lokale Netzwerk nach Geräten mit einer SMB-Dateifreigabe durchsuchen.

Gefundene Geräte können anhand von Informationen wie IP-Adresse, Gerätename und – soweit verfügbar – MAC-Adresse und potentiellem Hersteller leichter erkannt werden.

Es wird ausschließlich SMB über TCP-Port 445 verwendet.

### MAC-Adresse und Hersteller erkennen

Für bekannte MAC-Adressen versucht Network-Share-Assistant, den möglichen Hersteller des Netzwerkgeräts anzuzeigen.

Das kann beispielsweise helfen, ein NAS, einen Router, einen Server oder ein anderes Gerät in einer größeren Liste zu erkennen.

Die Herstellerangabe ist eine Orientierung. Bei virtuellen, lokal vergebenen oder zufällig erzeugten MAC-Adressen ist eine zuverlässige Herstellerbestimmung nicht immer möglich.

### ARP-Tabelle anzeigen

Die ARP-Ansicht zeigt bekannte Geräte mit tatsächlich aufgelöster MAC-Adresse.

Ungültige oder nicht aufgelöste Einträge wie `00:00:00:00:00:00` werden nicht angezeigt.

### Netzwerkinformationen anzeigen

Network-Share-Assistant kann wichtige Informationen über die aktuelle Netzwerkkonfiguration anzeigen, unter anderem:

- IPv4-Adresse
- IPv6-Adresse
- Subnetzmaske
- Standardgateway
- DNS-Server

### DNS-Abfrage durchführen

Mit der integrierten `nslookup`-Funktion kann geprüft werden, welcher Name zu einer IP-Adresse gehört oder welche Adresse einem Namen zugeordnet ist.

### Wake-on-LAN

Unterstützte Geräte können per Wake-on-LAN aufgeweckt werden, sofern Wake-on-LAN am Zielgerät und im Netzwerk eingerichtet ist.

### Windows-Anmeldeinformationen öffnen und verwalten

Der Windows-Anmeldeinformationsmanager kann direkt aus Network-Share-Assistant geöffnet werden. Gespeicherte Zugangsdaten für Netzwerkgeräte lassen sich dadurch leichter überprüfen und verwalten.

### Deutsch und Englisch

Die Oberfläche steht auf Deutsch und Englisch zur Verfügung.

Bei einem deutschen Windows wird Deutsch verwendet. Bei anderen Systemsprachen startet Network-Share-Assistant standardmäßig auf Englisch. Die Sprache kann direkt im Programm gewechselt werden.

---

# English

## What can Network-Share-Assistant do?

Network-Share-Assistant helps you connect shared folders on a NAS, server, or other SMB device as normal Windows drives. Tasks that would otherwise require Windows dialogs or command-line commands can be handled directly in the application.

### Connect network drives

You can map a network share as a normal Windows drive.

Usually you only need:

- server or NAS name / IP address
- share name
- preferred drive letter
- username and password, if required

Shares that do not require a username or password are supported as well.

A drive can be connected for the current session or configured to reconnect automatically when you sign in to Windows.

### Store credentials in Windows

If a share requires credentials, Network-Share-Assistant can store them in Windows Credential Manager.

This allows Windows to use the stored credentials when restoring a persistent network drive after signing in again or restarting the computer.

### Manage mapped drives

Network-Share-Assistant shows existing mapped network drives. You can disconnect one, several, or all selected drives.

It also indicates whether Windows is configured to restore a drive at sign-in.

### Rename network drives

A mapped drive can be given a more useful display name, for example:

`Data (WDCloud-134)`

The name is also shown in Windows Explorer.

### Find devices and network storage

Network-Share-Assistant can scan the local network for devices offering SMB file sharing.

Found devices can be easier to identify using their IP address, device name and, where available, MAC address and potential manufacturer.

Only SMB over TCP port 445 is used.

### Identify potential manufacturers

For known MAC addresses, Network-Share-Assistant tries to show the potential manufacturer of the network device.

This can help identify a NAS, router, server, or another device in a larger list.

Manufacturer information is intended as guidance. Virtual, locally administered, or randomized MAC addresses cannot always be mapped reliably.

### View the ARP table

The ARP view shows known devices for which a real MAC address has been resolved.

Invalid or unresolved entries such as `00:00:00:00:00:00` are hidden.

### View network information

Network-Share-Assistant can display important information about the current network configuration, including:

- IPv4 address
- IPv6 address
- subnet mask
- default gateway
- DNS servers

### DNS lookup

The integrated `nslookup` function can be used to check which name belongs to an IP address or which address belongs to a host name.

### Wake-on-LAN

Supported devices can be woken using Wake-on-LAN if Wake-on-LAN is enabled on the target device and supported by the network.

### Open and manage Windows credentials

Windows Credential Manager can be opened directly from Network-Share-Assistant, making it easier to review and manage stored credentials for network devices.

### German and English

The user interface is available in German and English.

German is selected automatically on German Windows installations. Other system languages default to English. The language can be changed directly in the application.

---

# Detaillierte Informationen / Detailed information

Dieser Abschnitt enthält technische Informationen für Administratoren, Entwickler und interessierte Anwender.

This section contains technical information for administrators, developers, and interested users.

## Plattform / Platform

- Windows
- .NET 10 (LTS)
- x64 and ARM64 application payloads
- single offline launcher/build artifact
- no online bootstrapper required

## Netzwerk / Networking

- SMB discovery uses TCP 445
- TCP 139 is deliberately not used
- IPv4 and IPv6 network information
- ARP/neighbour information for resolved IPv4 hosts
- DNS lookup via `nslookup`
- Wake-on-LAN support

## Netzlaufwerke / Network drives

Network drive operations use Windows networking APIs. Persistent mappings are stored in the Windows user profile so Windows can restore them at sign-in.

The host used for the UNC path and the credential target must refer to the same target, for example:

`\\192.168.1.10\Data`

with credential target:

`192.168.1.10`

## Zugangsdaten / Credentials

Credentials are stored using the native Windows Credential APIs and are intended to appear as Windows credentials rather than being passed permanently through command-line arguments.

Persistent authenticated mappings perform a reconnect self-test so the application can verify that Windows can reuse the stored credentials without receiving the password again from Network-Share-Assistant.

## MAC-Herstellerdaten / MAC vendor data

Manufacturer identification uses a local MAC prefix database embedded in the application.

The normal application and release build do not require an online lookup to identify vendors.

Locally administered, randomized, multicast, broadcast, zero, and incomplete MAC entries are handled separately or filtered where appropriate.

## Datenschutz / Privacy

Network-Share-Assistant does not need a cloud service for its core network-drive and local-network functions. Network information and stored Windows credentials remain on the local Windows system.

## Projekt / Project

© Stephan Biedermann

https://github.com/layer6org
