param(
    [string]$OutputPath = ".\src\NetworkShareAssistant.Discovery\Data\oui-vendors.txt"
)

$ErrorActionPreference = "Stop"

$Sources = @(
    @{ Registry = "MA-L"; Url = "https://standards-oui.ieee.org/oui/oui.csv" },
    @{ Registry = "MA-M"; Url = "https://standards-oui.ieee.org/oui28/mam.csv" },
    @{ Registry = "MA-S"; Url = "https://standards-oui.ieee.org/oui36/oui36.csv" }
)

$destination = [System.IO.Path]::GetFullPath($OutputPath)
$destinationDirectory = Split-Path -Parent $destination
New-Item -ItemType Directory -Force $destinationDirectory | Out-Null

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("NetworkShareAssistant-OUI-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force $tempRoot | Out-Null

$entries = @{}

try {
    foreach ($source in $Sources) {
        $csvPath = Join-Path $tempRoot ($source.Registry + ".csv")

        try {
            Write-Host "Downloading IEEE $($source.Registry): $($source.Url)"

            Invoke-WebRequest `
                -Uri $source.Url `
                -OutFile $csvPath `
                -Headers @{
                    "User-Agent" = "Network-Share-Assistant OUI Updater/1.6.3"
                    "Accept" = "text/csv,text/plain,*/*"
                } `
                -UseBasicParsing
        }
        catch {
            Write-Warning "IEEE $($source.Registry) download failed."
            Write-Warning $_.Exception.Message
            Write-Warning "Existing local MAC vendor snapshot is kept unchanged."
            exit 0
        }

        foreach ($row in (Import-Csv $csvPath)) {
            $assignment = [string]$row.Assignment
            $organization = [string]$row.'Organization Name'

            if ([string]::IsNullOrWhiteSpace($organization)) {
                $organization = [string]$row.Organization
            }

            if ([string]::IsNullOrWhiteSpace($assignment) -or
                [string]::IsNullOrWhiteSpace($organization)) {
                continue
            }

            $prefix = ($assignment -replace '[^0-9A-Fa-f]', '').ToUpperInvariant()

            if ($prefix.Length -notin @(6, 7, 9)) {
                continue
            }

            $organization = $organization.Replace("|", "/").Trim()
            $entries["$prefix|$($source.Registry)"] =
                "$prefix|$($source.Registry)|$organization"
        }
    }

    if ($entries.Count -eq 0) {
        Write-Warning "No assignments parsed. Existing snapshot remains unchanged."
        exit 0
    }

    $newFile = Join-Path $tempRoot "oui-vendors.txt"

    $lines = @(
        "# Network-Share-Assistant IEEE MAC vendor database"
        "# Snapshot generated from IEEE MA-L/MA-M/MA-S listings."
        "# Format: HEX_PREFIX|REGISTRY|ORGANIZATION"
    )

    $lines += $entries.Values |
        Sort-Object `
            @{ Expression = { ($_ -split '\|')[0].Length }; Descending = $true }, `
            @{ Expression = { ($_ -split '\|')[0] }; Descending = $false }

    [System.IO.File]::WriteAllLines(
        $newFile,
        $lines,
        [System.Text.UTF8Encoding]::new($false))

    Copy-Item $newFile $destination -Force

    Write-Host "Updated local vendor database: $($entries.Count) entries."
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
