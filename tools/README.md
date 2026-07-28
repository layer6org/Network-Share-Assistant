# Maintenance tools

The MAC manufacturer database is versioned with the project and is embedded
into the Discovery assembly. Release builds do not contact IEEE.

Manual update:

```powershell
.\tools\update-oui.ps1
```

If IEEE rejects the request, the updater preserves the existing local snapshot
and exits successfully.
