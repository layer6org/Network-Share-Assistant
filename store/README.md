# Microsoft Store Packaging

Die Store-Verteilung verwendet native MSIX-Pakete für x64 und ARM64.
Der Universal-Bootstrapper wird nicht in das Store-Paket aufgenommen.

Die endgültigen Store-Identitätswerte werden nach Reservierung der App im
Microsoft Partner Center in das Manifest übernommen:

- Package/Identity/Name
- Package/Identity/Publisher

Die Vorlage liegt unter:

`store\Package.appxmanifest.template.xml`
