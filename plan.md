# Feldbuch Universal – Entwicklungsplan

**Stand:** 2026-04-11 | **Aktuelle Version:** v1.7.3 | **Plattform:** WinForms (.NET 10)

---

## 1. CLAUDE.md – Compliance-Prüfung (Ist-Stand)

| Anforderung | Status | Befund |
|---|---|---|
| Code/UI auf Deutsch | ✅ OK | Alle Labels, Messages, Kommentare sind deutsch |
| Protokollierung bei OK-Übernahme | ⚠️ Teilweise | `ProtokollManager` loggt, aber kein expliziter OK-Trigger vor dem Schreiben |
| XML-Protokolldateien | ✅ OK | `FreieStationierung_Protokoll.xml` mit RTF-Generator vorhanden |
| Einheitliches Datenverzeichnis | ✅ OK | Alle Daten im `ProjektManager.ProjektVerzeichnis` |
| Standard-CSV-Format (Muster) | ✅ OK | `CsvDatenDatei.cs` hält METADATA-Header + `---` + DATENTYP-Struktur ein |
| Dateinamen-Konventionen | ✅ OK | Deutsche Klassen/Methoden-Namen durchgehend |

**Offene Punkte:**
- `FormFreieStationierung`: OK-Bestätigung vor Protokollschreibung nicht atomisch – erst in v2.0 adressieren
- `FormDxfViewer`: Kein Protokolleintrag wenn Messung verworfen wird (Absicht?)
- MAUI-Projekt (`Feldbuch Universal`) ist noch leeres Skeleton

---

## 2. Strategische Architektur-Entscheidung: WinForms vs. MAUI

### Ist WinForms ein Hindernis für iOS/Android?

**Ja – WinForms ist ausschließlich Windows.**

WinForms-spezifische Teile:
- `DxfCanvas` erbt von `System.Windows.Forms.Panel`
- Alle 14 Forms erben von `Form`
- Touch-Handling über `WM_TOUCH` (Win32 API)
- `System.IO.Ports` (seriell) – auf Mobile nicht direkt verfügbar
- `System.Drawing` – Teile nicht MAUI-kompatibel

**Lösungsweg: Schichtenarchitektur (empfohlen)**

```
┌─────────────────────────────────────────────────────────┐
│              Feldbuch.Shared  (Class Library)           │
│  ┌───────────────────────────────────────────────────┐  │
│  │  Business Logic (plattformunabhängig)             │  │
│  │  FreieStationierung, NeupunktRechner,             │  │
│  │  CsvDatenDatei, GsiParser, DxfParser,             │  │
│  │  Alle Manager-Klassen, Alle Modelle               │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
         │                          │
         ▼                          ▼
┌─────────────────┐      ┌─────────────────────────┐
│ Feldbuch        │      │ Feldbuch Universal       │
│ (WinForms)      │      │ (.NET MAUI)              │
│ Windows Desktop │      │ Android / iOS / Win      │
│ Voller Umfang   │      │ Touch-First UI           │
│ Serial / BT     │      │ BT LE / USB OTG          │
└─────────────────┘      └─────────────────────────┘
```

### Zeitplan für Schichtenarchitektur

| Phase | Aufwand | Ergebnis |
|---|---|---|
| Shared Library anlegen | 2-4h | `Feldbuch.Shared.csproj` als .NET Standard 2.1 |
| Business-Logic migrieren | 8-16h | Alle Manager/Parser/Rechner in Shared |
| Interfaces abstrahieren | 4-8h | `IFileSystem`, `ISerialPort`, `IBluetoothService` |
| WinForms referenziert Shared | 2-4h | Keine funktionalen Änderungen |
| MAUI UI aufbauen | 40-80h | Erste mobile Version (Android-Fokus) |

---

## 3. Internationalisierung (i18n) – Mehrsprachigkeit

### Unterstützte Zielsprachen (Priorität)

1. **Deutsch** (Muttersprache, Default) – bereits 100%
2. **Englisch** – internationales Geodäsie-Publikum
3. **Französisch** – Schweiz, Frankreich, Belgien
4. **Italienisch** – Südtirol, Schweiz, Italien
5. *Optional:* Spanisch, Polnisch, Tschechisch

### Technische Umsetzung

**Ansatz: .NET Resource Files (.resx)**

```
Feldbuch.Shared/
  Resources/
    Strings.de.resx   ← Default (German)
    Strings.en.resx
    Strings.fr.resx
    Strings.it.resx
```

**Schritt 1: Alle Strings extrahieren**

Aktuell ~450 hardcodierte Strings verteilt auf:
- 14 Designer.cs-Dateien (UI-Labels, Tooltips)
- ~30 .cs-Dateien (MessageBox-Texte, Fehlermeldungen)
- Protokoll-Inhalte (ProtokollManager, RtfProtokollGenerator)

**Schritt 2: Zugriff im Code**

```csharp
// Statt:
MessageBox.Show("Keine Punkte gefunden.");
// Künftig:
MessageBox.Show(Strings.KeinePunkteGefunden);
```

**Schritt 3: Sprachauswahl**

- Einstellung in `Einstellungen.xml` (`<Sprache>de</Sprache>`)
- Ladevorgang: `Thread.CurrentThread.CurrentUICulture = new CultureInfo(sprache);`
- UI in `FormInfo` oder neuem `FormEinstellungen` wählbar

**Wichtige Besonderheiten:**

| Thema | Anforderung |
|---|---|
| Dezimaltrennzeichen | Immer `.` (InvariantCulture) für Koordinaten-CSV – bereits so implementiert |
| Einheiten | Gon/Grad wählbar (bereits teilweise vorhanden) |
| Protokoll-Sprache | Kann unabhängig von UI-Sprache sein (z.B. immer Deutsch für Behörden) |
| Datumsformat | ISO 8601 (yyyy-MM-dd) für Dateien, lokales Format für Anzeige |

---

## 4. Feature-Roadmap nach Priorität

### v1.8 – Qualitätssicherung & Protokoll (kurzfristig)

- [ ] **Protokoll-Vollständigkeit**: Expliziter OK-Trigger in `FormFreieStationierung` vor Protokollschreibung
- [ ] **Neupunkt-Protokoll**: Automatisches XML-Protokoll für jeden berechneten Neupunkt
- [ ] **Wiederherstellung**: Letzte Stationierung beim Neustart automatisch laden
- [ ] **Backup-Mechanismus**: Automatische Datensicherung beim Projektstart (ZIP)
- [ ] **Validierung**: Plausibilitätsprüfung Koordinaten (Ausreißer-Erkennung > 3σ)
- [ ] **Fehlerprotokoll-Viewer**: GUI zum Anzeigen von `Fehler.log`

### v1.9 – Erweiterter DXF-Viewer

- [ ] **Layer-Manager**: Sichtbarkeit und Farbe aller DXF-Layer togglen
- [ ] **Schnittmenge-Berechnung**: Zwei Geraden → Schnittpunkt berechnen
- [ ] **Strecken-Messung**: Direkte Abstandsmessung im DXF-Viewer
- [ ] **Polygon-Fläche**: Fläche eines Polygons berechnen (Gauß'sche Flächenformel)
- [ ] **DXF-Vergleich**: Zwei DXF-Dateien überlagern, Differenzen farbig markieren
- [ ] **Export SVG/PDF**: Viewer-Ausschnitt als SVG oder PDF speichern
- [ ] **Koordinaten-Suche**: Direkteingabe Koordinaten → Sprung zur Position
- [ ] **Maßstab-Anzeige**: Dynamischer Maßstab in der Statusleiste

### v2.0 – Shared Library & MAUI-Grundgerüst (mittelfristig)

- [ ] **Feldbuch.Shared Projekt** anlegen (`net8.0` / `netstandard2.1`)
- [ ] Alle Manager/Parser/Rechner migrieren
- [ ] Interfaces für Plattform-Dienste:
  - `IFileSystemService` (Pfade, Lesen/Schreiben)
  - `IBluetoothService` (BLE und Classic)
  - `ISerialPortService` (USB-OTG auf Android)
  - `IDialogService` (plattformunabhängige Dialoge)
- [ ] MAUI-Projekt aufsetzen (Android-Fokus zuerst)
- [ ] Erstes MAUI-Feature: Neupunkt-Messung mit Tachymeter via Bluetooth

### v2.1 – Internationalisierung (mittelfristig)

- [ ] Resource-Files anlegen (de, en)
- [ ] Alle UI-Strings in FormDxfViewer extrahieren
- [ ] Alle UI-Strings in Form1, FormFreieStationierung extrahieren
- [ ] Sprachauswahl in Einstellungen
- [ ] Englische Übersetzung vollständig
- [ ] Französische Übersetzung (50%)

### v2.5 – Mobile (Android-Release) + Cloud-Sync

- [ ] MAUI Android UI vollständig:
  - Projektauswahl (lokaler Speicher / Cloud-Sync)
  - DXF-Viewer (SkiaSharp für cross-platform Rendering)
  - Freie Stationierung
  - Neupunktberechnung
  - Tachymeter-Verbindung (Bluetooth LE + Classic)
- [ ] Android-Berechtigungen: Bluetooth, Dateizugriff, Kamera
- [ ] Offline-First: Alles ohne Internet nutzbar

#### Cloud-Sync via Nextcloud (WebDAV)

**Ziel:** Projektdaten zwischen Büro-PC und Android-Gerät synchronisieren – selbst gehostet, keine Abhängigkeit von Drittdiensten.

**Architektur:**
```
Feldbuch.Shared/
  ICloudSyncService.cs        ← Interface (plattformunabhängig)
  NextcloudSyncService.cs     ← WebDAV-Implementierung (HttpClient)
  SyncManifest.cs             ← Metadaten für Konflikt-Erkennung
```

**Synchronisierte Dateien pro Projekt:**
```
Nextcloud/Feldbuch/
  {ProjektName}/
    Feldbuchpunkte.json
    Station-{Nr}.json
    {ProjektName}-Neupunkte.json
    ImportPunkte.json
    Projektdaten.csv
    Anschlusspunkte.csv
    Protokoll_YYYY-MM-DD.txt   (optional)
  {DXF-Dateien}/               (Büro → Feld, read-only im Feld)
  sync_manifest.json           (Timestamp + SHA256 pro Datei)
```

**Workflow:**
- Morgens im Feld: Projekt von Nextcloud laden (wenn neuere Version vorhanden)
- Messungen laufen offline-first auf Gerät
- Abends: "Auf Nextcloud speichern" – alle geänderten Dateien hochladen
- Konflikt-Erkennung via `sync_manifest.json` (Timestamp-Vergleich + SHA256)
- Bei Konflikt: Dialog mit Auswahl (Cloud/Lokal/Zusammenführen)
- JSON-Zusammenführung: Punkte nach `PunktNr` mergen, Duplikate erkennen

**Einstellungen (in FormEinstellungen / MAUI-Settings):**
```
Server-URL:    https://meine-nextcloud.de
Benutzer:      jgerner
App-Passwort:  ●●●●●●●●●●●●●●●●   (Nextcloud App-Passwort, nicht Hauptpasswort)
Ordner:        /Feldbuch/
```

**Implementierungs-Tasks:**
- [ ] `ICloudSyncService` Interface + `NextcloudSyncService` (WebDAV PUT/GET/MKCOL/PROPFIND)
- [ ] `SyncManifest`-Klasse mit SHA256-Hashing
- [ ] Einstellungen erweitern (URL, Benutzer, App-Passwort – verschlüsselt gespeichert)
- [ ] "Verbindung testen"-Funktion
- [ ] Upload/Download-Dialog in `FormProjekt` (WinForms) und MAUI-Projektseite
- [ ] Konflikt-Erkennung und Merge-Dialog
- [ ] DXF-Dateien separater Sync-Modus (nur Download, kein Upload)
- [ ] Android MAUI-Integration (gleiche Shared-Klasse wie Desktop)

**Geschätzter Aufwand:** ~15h (Interface + Service + UI-Dialoge)

### v3.0 – iOS & Cloud

- [ ] iOS-Unterstützung (iPad-fokussiert für Feldarbeit)
- [ ] iCloud / Google Drive Projekt-Sync
- [ ] GNSS-Integration (GPS auf Mobilgeräten)
- [ ] Foto-Dokumentation: Bilder zu Messpunkten hinzufügen
- [ ] REST API für Datenaustausch mit GIS-Systemen

---

## 5. Zusätzliche Funktionen (Ideenpool)

### Vermessungskern-Erweiterungen

| Funktion | Beschreibung | Priorität |
|---|---|---|
| **Bogenschnitt** | Koordinatenberechnung aus 2 Strecken | Hoch |
| **Einschneiden vorwärts** | Koordinaten aus 2 Richtungen | Hoch |
| **Helmert 2D/3D** | Vollständige Transformation | Mittel |
| **Polygonzug** | Offener/geschlossener Polygonzug mit Fehlerausgleich | Hoch |
| **Nivellement** | Höhenmessung Protokoll | Mittel |
| **GNSS-Import** | RINEX / NMEA-Daten importieren | Mittel |
| **Profilschnitt** | Längs- und Querprofil aus Messpunkten | Mittel |
| **Grundbuchvermessung** | Flächen, Grenzen, Teilungsberechnungen | Niedrig |

### Daten & Import/Export

| Funktion | Beschreibung | Priorität |
|---|---|---|
| **LandXML-Import** | ISO-Standard für Vermessungsdaten | Hoch |
| **GeoJSON-Export** | Direkt-Export für GIS (QGIS, ArcGIS) | Hoch |
| **Shape-File-Export** | ArcGIS / QGIS Shapefile | Mittel |
| **KMZ/KML-Export** | Google Earth kompatibel | Mittel |
| **PDF-Protokoll** | PDFsharp statt RTF | Mittel |
| **Excel-Export** | XLSX für Aufmaßblätter | Niedrig |
| **Totalstation andere Hersteller** | Sokkia, Trimble, Topcon – eigener Parser je Gerät (analog `GeoCOMParser.cs`) | Mittel |
| **GNSS-Empfänger** | NMEA 0183 Parser (`NmeaParser.cs`), Protokollintegration vorbereitet | Mittel |
| **Robotik-Tachymeter** | Automatische Zielsuche (ATR) | Hoch |

### UI & Usability

| Funktion | Beschreibung | Priorität |
|---|---|---|
| **Dark Mode** | System-Theme folgen | Mittel |
| **Einstellungs-Dialog** | Alle Optionen in einem FormEinstellungen | Hoch |
| **Tastaturkürzel-Übersicht** | Help-Dialog mit allen Shortcuts | Niedrig |
| **Rückgängig/Wiederholen** | Undo/Redo für Messpunkte | Mittel |
| **Kamera-Overlay** | Live-Kamerabild über DXF (AR) | Niedrig |
| **Druckvorschau** | DXF + Messdaten drucken | Mittel |

---

## 6. Technologie-Empfehlungen

### Cross-Platform DXF-Rendering (für MAUI)

**SkiaSharp** (empfohlen):
- Funktioniert auf Android, iOS, Windows, MacCatalyst
- Ersatz für `System.Drawing` / WinForms Panel
- `SKCanvas` statt `Graphics`
- Bestehender `DxfCanvas` → `SkiaDxfCanvas` portieren

```csharp
// WinForms:
protected override void OnPaint(PaintEventArgs e)
{
    e.Graphics.DrawLine(pen, x1, y1, x2, y2);
}

// MAUI mit SkiaSharp:
void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
{
    e.Surface.Canvas.DrawLine(x1, y1, x2, y2, paint);
}
```

### Bluetooth auf Mobile

| Plattform | Bibliothek | Protokoll |
|---|---|---|
| Android | `Plugin.BLE` (NuGet) | Bluetooth Classic (SPP) + BLE |
| iOS | `Plugin.BLE` (NuGet) | Nur BLE (iOS beschränkt Classic) |
| Windows | Bestehend `System.IO.Ports` | COM-Port via BT |

**Wichtig:** iOS unterstützt kein Bluetooth Classic (SPP). Tachymeter-Kommunikation auf iOS nur möglich mit:
- BLE-fähigen Tachymetern (neuere Leica-Modelle)
- WLAN-Adapter (GeoCOM über TCP/IP)

### Datei-System auf Mobile

```csharp
// Interface-Abstraktion (in Shared Library):
public interface IFileSystemService
{
    string AppDataDirectory { get; }
    string ProjectDirectory { get; set; }
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string content);
    bool FileExists(string path);
    string[] GetFiles(string directory, string pattern);
}

// Android-Implementierung:
public class AndroidFileSystemService : IFileSystemService
{
    public string AppDataDirectory => FileSystem.AppDataDirectory;
    // ...
}
```

---

## 6b. Architekturentscheidungen (festgelegt)

### Geräteprotokolle: direkt im Code, nicht externalisiert

**Entscheidung (2026-04-11):** Tachymeter- und GNSS-Steuerkommandos werden **nicht** in JSON/XML-Konfigurationsdateien ausgelagert.

**Begründung:**
- Die Messsequenz-Zustandsmaschine enthält Fehlerbehandlungslogik – nicht sinnvoll als Daten darstellbar
- Ein vollständiger Interpreter wäre komplexer als der Code selbst
- Nur reine Parameter wären externalisierbar; Aufwand übersteigt Nutzen

**Umsetzung:**
- Neues Gerät = neuer Parser (`SokkiaParser.cs`, `NmeaParser.cs`, etc.) analog `GeoCOMParser.cs`
- Registrierung in `TachymeterParserFactory`
- Messsequenz-Anpassung direkt in `FormDxfViewer.VerarbeiteZeile()`
- Jedes neue Gerät wird gemeinsam mit dem Benutzer eingebaut

---

## 7. Bekannte technische Schulden

| Bereich | Problem | Lösung |
|---|---|---|
| **Versionierung** | `.csproj` zeigt v1.4.1, Publish-Ordner hat v1.7.2 | `<Version>` im .csproj synchronisieren |
| **Kein Unit-Test-Projekt** | Berechnungen ungetestet | `Feldbuch.Tests` mit xUnit anlegen |
| **GeoCOM-Parser** | State-Machine komplex, schwer zu testen | Unit-Tests für alle bekannten Response-Muster |
| **FreieStationierung** | Sonderfall 2-Punkt ohne Tests | Mathematik-Tests für Grenzfälle |
| **ProtokollManager** | Nur Text-Protokoll, kein strukturiertes Log | JSON-basiertes Audit-Log ergänzen |
| **DxfParser** | Kein Support für SPLINE, HATCH, BLOCK | Erweiterung für komplexere DXF-Dateien |
| **Icon-Ressourcen** | `ExportIcons`-Tool separat | In Build-Prozess integrieren |

---

## 8. Migrations-Checkliste (WinForms → MAUI)

### Vorbereitungsarbeiten (vor MAUI-Migration)

```
[ ] Feldbuch.Shared Klassenbibliothek anlegen
[ ] Alle Modelle nach Shared verschieben
[ ] Alle Manager nach Shared verschieben
[ ] Alle Berechnungsklassen nach Shared verschieben
[ ] Alle Parser nach Shared verschieben
[ ] Interfaces für Platform-Services definieren
[ ] Unit-Tests für Shared-Klassen schreiben
[ ] WinForms-Projekt gegen Shared referenzieren
[ ] Build + alle Tests grün
```

### MAUI-Projekt aufbauen

```
[ ] MAUI-Projekt auf Shared referenzieren
[ ] Dependency Injection konfigurieren
[ ] Navigation (Shell/NavigationPage)
[ ] Projektauswahl-Seite
[ ] DXF-Viewer mit SkiaSharp
[ ] Freie-Stationierung-Seite
[ ] Neupunkt-Seite
[ ] Tachymeter-Verbindung (Plugin.BLE)
[ ] Android-Release bauen + testen
[ ] iOS-Build (Mac-Build-Agent benötigt)
```

---

## 9. Prioritäts-Matrix

```
         AUFWAND
         Gering        Mittel         Hoch
       ┌─────────────┬──────────────┬──────────────┐
HOCH   │ Protokoll-  │ Shared Lib   │ MAUI Mobile  │
       │ Vollständig │ anlegen      │ (v2.5)       │
       │ (v1.8)      │ (v2.0)       │              │
       ├─────────────┼──────────────┼──────────────┤
MITTEL │ Layer-Mgr   │ i18n (v2.1)  │ iOS (v3.0)   │
       │ DXF-Viewer  │ PDF-Protokoll│ Cloud-Sync   │
       │ (v1.9)      │              │              │
       ├─────────────┼──────────────┼──────────────┤
GERING │ Dark Mode   │ GeoJSON-     │ AR-Kamera    │
       │ Tastatur-   │ Export       │ Overlay      │
       │ Kürzel      │              │              │
       └─────────────┴──────────────┴──────────────┘
```

---

## 10. Nächste konkrete Schritte

1. **Sofort (v1.8):** `FormFreieStationierung` – OK-Button triggert explizit `ProtokollManager.Schreiben()`
2. **Kurzfristig:** `Feldbuch.Shared` Bibliothek erstellen, schrittweise migrieren
3. **Mittelfristig:** Resource-Files anlegen, Strings extrahieren (Englisch zuerst)
4. **Langfristig:** MAUI Android-App mit SkiaSharp-DXF-Viewer

---

*Dieser Plan ist ein lebendiges Dokument – Ergänzungen und Priorisierungsänderungen nach Bedarf.*
