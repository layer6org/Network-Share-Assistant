param(
    [ValidateSet("All", "Universal", "Native")]
    [string]$Target = "All"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Artifacts = Join-Path $Root "artifacts"
$PayloadDir = Join-Path $Root "src\NetworkShareAssistant.Bootstrapper\Payloads"

Remove-Item $Artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item $Artifacts -ItemType Directory -Force | Out-Null

Write-Host "Regression tests..."
dotnet run `
    --project "$Root\tests\NetworkShareAssistant.RegressionTests\NetworkShareAssistant.RegressionTests.csproj" `
    -c Release

if ($LASTEXITCODE -ne 0) {
    throw "Regression tests failed."
}

foreach ($runtime in @("win-x64", "win-arm64")) {
    $out = Join-Path $Artifacts $runtime

    dotnet publish `
        "$Root\src\NetworkShareAssistant.UI\NetworkShareAssistant.UI.csproj" `
        -c Release `
        -r $runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:PublishReadyToRun=false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $out

    if ($LASTEXITCODE -ne 0) {
        throw "Publish $runtime failed."
    }
}

if ($Target -eq "Native") {
    Write-Host "Native builds: $Artifacts"
    exit 0
}

Remove-Item $PayloadDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $PayloadDir -ItemType Directory -Force | Out-Null

foreach ($runtime in @("win-x64", "win-arm64")) {
    Compress-Archive `
        -Path "$Artifacts\$runtime\*" `
        -DestinationPath "$PayloadDir\$runtime.zip" `
        -CompressionLevel Optimal `
        -Force
}

$universal = Join-Path $Artifacts "universal"

dotnet publish `
    "$Root\src\NetworkShareAssistant.Bootstrapper\NetworkShareAssistant.Bootstrapper.csproj" `
    -c Release `
    -r win-x64 `
    -p:PublishAot=true `
    -p:IlcOptimizationPreference=Size `
    -p:StripSymbols=true `
    -o $universal

if ($LASTEXITCODE -ne 0) {
    throw "Universal NativeAOT build failed."
}

Copy-Item `
    "$universal\Network-Share-Assistant.exe" `
    "$Artifacts\Network-Share-Assistant.exe" `
    -Force

$size = (Get-Item "$Artifacts\Network-Share-Assistant.exe").Length / 1MB

Write-Host ""
Write-Host ("Universal EXE: {0:N1} MB" -f $size)
Write-Host "$Artifacts\Network-Share-Assistant.exe"
