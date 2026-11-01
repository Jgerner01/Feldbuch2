# Erzeugt das Download-Paket fuer ein GitHub-Release:
#   artifacts\Feldbuch-v<Version>-win-x64.zip  und  ...zip.sha256
# Der Updater installiert ein Release nur, wenn beide Dateien vorhanden sind.
# Nach dem Commit ausfuehren, damit die EXE den richtigen Commit-Hash enthaelt.
$ErrorActionPreference = 'Stop'
$wurzel = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $wurzel 'Feldbuch\Feldbuch.csproj'
$version = ([xml](Get-Content -LiteralPath $csproj -Raw -Encoding UTF8)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -v quiet --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish ist fehlgeschlagen.' }

$publish = Join-Path $wurzel 'Feldbuch\bin\Release\net10.0-windows\win-x64\publish'
$artifacts = Join-Path $wurzel 'artifacts'
$name = "Feldbuch-v$version"
$stage = Join-Path $artifacts $name
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# Nur bekannte Programmdateien, damit keine Daten aus dem Build-Ordner ins Release gelangen.
Copy-Item -LiteralPath (Join-Path $publish 'Feldbuch.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $publish 'Muster.csv') -Destination $stage
Copy-Item -LiteralPath (Join-Path $publish 'prismenkonstanten.csv') -Destination $stage
Copy-Item -Path (Join-Path $publish '*_Protokoll.xml') -Destination $stage
Copy-Item -LiteralPath (Join-Path $publish 'icons') -Destination $stage -Recurse

$zip = Join-Path $artifacts "$name-win-x64.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [IO.Compression.CompressionLevel]::Optimal, $true)

$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
$pruefsumme = "$zip.sha256"
[IO.File]::WriteAllText($pruefsumme, "$hash  $name-win-x64.zip`n")

Write-Output "Paket:      $zip"
Write-Output "Pruefsumme: $pruefsumme"
Write-Output "Release:    gh release create v$version `"$zip`" `"$pruefsumme`" --verify-tag --latest --title `"v$version`" --notes-file <Datei>"
