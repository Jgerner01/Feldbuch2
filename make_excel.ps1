# GPS-Messung in KOR-Datei umwandeln
$gpsDir = "D:\NextCloud\jgerner\04 Praktikum Vermessung"
$gpsSub = Get-ChildItem $gpsDir -Directory | Where-Object { $_.Name -like "*bung 3*" }
$gpsFolder = $gpsSub[0].FullName
$gpsFile = Join-Path $gpsFolder "GPS-Messung.txt"
$korFile = Join-Path $gpsFolder "gps-messung.kor"

$lines = Get-Content $gpsFile

# Punkt-Sektion finden - nach "Point" Zeile suchen
$startIdx = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Trim() -eq 'Point') {
        $startIdx = $i + 2  # Header-Zeilen ueberspringen
        break
    }
}

if ($startIdx -lt 0) {
    Write-Host "FEHLER: Point-Sektion nicht gefunden!" -ForegroundColor Red
    exit 1
}

# Datenzeilen parsen
$rawPoints = @()
for ($i = $startIdx; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Trim() -eq '') { break }

    # Datenzeilen haben: PointName  Easting  Northing  Height  Lat  Lon ...
    # PointName beginnt entweder mit T\d oder urn:
    if ($line -match '^(T\d+|urn:[^\s]+)\s+(\d[\d.]+)\s+(\d[\d.]+)\s+(\d[\d.]+)') {
        $rawPoints += @{
            Name = $matches[1]
            Easting = [double]$matches[2]
            Northing = [double]$matches[3]
            Height = [double]$matches[4]
        }
    }
}

Write-Host "Rohe Punkte: $($rawPoints.Count)" -ForegroundColor Cyan

# Duplikate entfernen (gleicher Name -> ersten nehmen) und kurze Namen umbenennen
$nameCounter = 1
$usedNames = @{}
$points = @()

foreach ($p in $rawPoints) {
    $name = $p.Name

    # Pruefe ob Name > 5 Zeichen
    if ($name.Length -gt 5) {
        # URN-Namen durch GPS1, GPS2, ... ersetzen
        if ($usedNames.ContainsKey($name)) {
            continue  # Duplikat ueberspringen
        }
        $name = "GPS$nameCounter"
        $nameCounter++
    } else {
        # Kurze Namen (T2, T3, ...) behalten
        if ($usedNames.ContainsKey($name)) {
            continue  # Duplikat ueberspringen
        }
    }

    $usedNames[$name] = $true
    $points += @{
        Name = $name
        X = $p.Easting
        Y = $p.Northing
        Z = $p.Height
    }
}

Write-Host "Eindeutige Punkte: $($points.Count)" -ForegroundColor Yellow

# KOR-Datei schreiben
$header = @"
# METADATA
Projekt: Campus THD UTM32
Sensor: GPS/GNSS RTK
Bearbeiter:
Datum: $(Get-Date -Format 'yyyy-MM-dd')
---
# DATENTYP
Koordinaten
# DATEN
PunktNr; X; Y; Z; Code
"@

$korLines = @($header)
$nr = 1
foreach ($p in $points) {
    $x = "{0:F3}" -f $p.X
    $y = "{0:F3}" -f $p.Y
    $z = "{0:F3}" -f $p.Z
    $korLines += "$nr; $x; $y; $z; GPS"
    $nr++
}

$korLines -join "`r`n" | Out-File -FilePath $korFile -Encoding UTF8

Write-Host "`nKOR-Datei erstellt: $korFile" -ForegroundColor Green
Write-Host "Punkte: $($points.Count)" -ForegroundColor Green

# Ausgabe der Punkte
Write-Host "`nErgebnis:" -ForegroundColor Cyan
foreach ($p in $points) {
    Write-Host ("  {0,-6} X={1,12} Y={2,12} Z={3,9}" -f $p.Name, ("{0:F3}" -f $p.X), ("{0:F3}" -f $p.Y), ("{0:F3}" -f $p.Z))
}
