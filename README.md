# Feldbuch

**Geodätisches Feldbuch** – Windows-Anwendung für Vermessung im Feld mit Tachymeteranbindung, freier Stationierung, automatischer Punktidentifikation, DXF-Viewer und umfangreichen Berechnungsmodulen.

## Features

### Tachymeter-Kommunikation
- Verbindung über COM-Port oder Bluetooth
- Unterstützte Protokolle / Geräte:
  - **Leica GeoCOM** – TCR307, TS06, TS11, TPS1200-Reihe (38400 / 9600 Baud)
  - **Leica GSI Online** – TPS300-Reihe (9600 Baud, GSI-8 und GSI-16)
  - **Sokkia SDR** – SET-Reihe, SDR33-ASCII-Format
  - **Topcon GTS/GPT** – CR-Trigger-Protokoll
  - **GNSS NMEA 0183** – passiver Empfang
  - **Manuell** – direkte Koordinateneingabe
- Testmessungen-Fenster mit Rohdaten-Monitor
- Automatisches Health-Monitoring der Bluetooth-Verbindung

### Freie Stationierung
- Helmert-Direktlösung (2 Punkte) und vollständige Ausgleichung (≥ 3 Punkte)
- Einzelne Beobachtungen (Richtung / Strecke) aktivierbar / deaktivierbar
- Iterative Ausgleichung mit konfigurierbaren Fehlergrenzen
- Protokollausgabe (RTF) mit Residuen, s₀, Redundanz, Maßstab
- Live-Stationierungsberechnung im DXF-Viewer mit Signallampe (grün/gelb/rot)
- Residual-Overlay im DXF-Viewer

### Punktfinder – automatische Punktidentifikation *(neu in v1.9.3)*
- **Handbetrieb**: DXF-Punkt anklicken + aktuelle Tachymetermessung (automatisch vorausgefüllt) → `Anschlusspunkte.csv`
- **Auto-Match**: sobald eine brauchbare Stationierung vorliegt (s₀ ≤ 20 mm, Redundanz > 0), wird jede neue Messung automatisch einem DXF-Punkt zugeordnet
  - Positionsvorhersage aus Station + Messung (Hz, V, D) mit Maßstabskorrektur
  - Dynamischer Suchradius aus s₀, Punktanzahl und Strecke
  - Auto-Übernahme bei Abstand ≤ ½ Suchradius, sonst Bestätigungsdialog
  - Winkel-only-Modus (ohne Strecke) immer mit Bestätigung
  - Distanz-Pflicht konfigurierbar
- DXF-Overlay: Suchkreis (Cyan), Auto-Zone (Grün), Kandidaten (Orange) bei Bestätigung
- Protokoll aller Suchereignisse: `AutoMatch_{Standpunkt}.csv`
- JSON-Audit-Datei: `AutoMatch_Punkte.json` (Undo, Resume, Nachvollziehbarkeit)

### DXF-Viewer
- Zoom (Mausrad), Pan, Fit-to-View
- Snap auf DXF-Endpunkte
- DXF-Punkt-Marker mit Nummern aus persistentem Index
- Overlay: Standpunkte (rot), Neupunkte (grün), Import-Punkte, Residual-Marker
- Digitalisier-Modus (Koordinatenpicking in KOR-Datei)
- Layer-Sichtbarkeit einzeln ein-/ausblenden
- Katasterpunkte-Overlay

### Berechnungsmodule
- **Vorwärtsschnitt** – aus zwei Standpunkten und Richtungen
- **Rückwärtsschnitt** – aus drei bekannten Punkten
- **Bogenschnitt** – aus zwei bekannten Punkten und Strecken
- **Hochpunktherablegung** – vertikale Projektion
- **Koordinatentransformation** – Helmert (4-Parameter) und affin (6-Parameter)

### Absteckung
- **Punktabsteckung** – Soll-/Ist-Vergleich mit Kartenansicht und Einweiser
- **Achsabsteckung** – entlang einer definierten Trasse
- **Schnurgerüst** – Rechteckgebäude
- **Rasterabsteckung** – regelmäßiges Punktraster
- **Profilabsteckung** – Querprofile entlang einer Achse
- **Flächenteilung** – Aufteilung von Polygonflächen

### Import / Export
- **GSI-8 / GSI-16** – Import von Tachymeter-Messdateien
- **KOR / KOR-CSV** – Festbreitenformat und CSV-Metadaten-Header
- **DXF** – Koordinaten-Import aus DXF-Entities; Export von Messpunkten
- **CSV** – Anschlusspunkte, Testdaten, Muster

### Protokolle
- RTF-Protokolle für alle Berechnungsmodule
- Konfigurierbares XML-Protokoll-Layout pro Modul
- Auto-Match-Protokoll (CSV) mit vollständigem Suchergebnis-Kontext

### Projektverwaltung
- Mehrere Projekte mit eigenem Verzeichnis
- Projektdaten, Rechenparameter und Zoom-Zustand persistent
- Feldbuchpunkte (Standpunkte, Neupunkte) in JSON gespeichert
- Automatischer Neupunkt-Zähler

## Technologie

| | |
|---|---|
| **Plattform** | Windows (.NET 10, WinForms) |
| **Sprache** | C# |
| **Zielrahmen** | `net10.0-windows` |
| **Abhängigkeiten** | `System.IO.Ports`, `System.Management` |

## Version

Aktuelle Version: **v1.9.3**

| Version | Inhalt |
|---|---|
| v1.9.3 | Punktfinder – automatische Punktidentifikation für Freie Stationierung |
| v1.9.2 | Leica TPS300 GSI Online-Protokoll |
| v1.9.1 | Schnurgerüst neu, Koordinatentransformation Duplikat-Fix |
| v1.9.0 | Absteckung: Kartenpicking, universeller Einweiser, Achsabsteckung erweitert |
| v1.8.x | Absteckungsmodule, DXF-Digitalisierung, KOR-Import |

## Autor

Johann Gerner – © 2026

---

# Anleitung: Bluetooth-Kopplung und COM-Port-Einrichtung (Windows 11)

Diese Anleitung beschreibt die Kopplung eines Bluetooth-Geräts und die anschließende manuelle Zuweisung einer ausgehenden COM-Schnittstelle.

## 1. Bluetooth-Gerät koppeln

1. Öffnen Sie die **Einstellungen** (`Win + I`).
2. Navigieren Sie zu **Bluetooth & Geräte**.
3. Stellen Sie sicher, dass der Bluetooth-Schalter auf **Ein** steht.
4. Klicken Sie auf **Gerät hinzufügen** → **Bluetooth**.
5. Wählen Sie Ihr Gerät aus der Liste aus.
6. Geben Sie die PIN ein und bestätigen Sie mit **Verbinden**.  
   Leica: `0000` · andere Geräte oft: `1234` · ggf. beim Hersteller erfragen.

## 2. Ausgehende COM-Schnittstelle konfigurieren

1. Scrollen Sie unter **Bluetooth & Geräte** nach unten und klicken Sie auf **Geräte**.
2. Scrollen Sie zum Bereich „Verwandte Einstellungen" und klicken Sie auf **Weitere Bluetooth-Einstellungen**.
3. Wechseln Sie zum Reiter **COM-Anschlüsse**.
4. Klicken Sie auf **Hinzufügen...** → **Ausgehend**.
5. Wählen Sie Ihr Gerät und bestätigen Sie mit **OK**.
6. Notieren Sie den zugewiesenen Port (z. B. `COM3`).
