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
- **Gemeinsame Messkonfiguration** *(seit v1.9.6)*: Prisma / Reflektorlos (kurz, lang), EDM-Modus und Prismenkonstante gelten für alle Messfenster, werden gespeichert und mit dem Gerät abgeglichen (Rücklesen und Vergleich; Warnung bei Abweichung nach einer Messung)
- **Signaltöne am Instrument** *(Leica GeoCOM, seit v1.9.6)*: ein Piepton bei erfolgreicher, dreifacher Alarmton bei fehlgeschlagener Messung; Test-Button im Fenster Testmessungen
- Zeitüberwachung der Messabläufe (kein dauerhaftes Hängen bei ausbleibender Antwort)

### Freie Stationierung
- Helmert-Direktlösung (2 Punkte) und vollständige Ausgleichung (≥ 3 Punkte)
- Einzelne Beobachtungen (Richtung / Strecke) aktivierbar / deaktivierbar
- Iterative Ausgleichung mit konfigurierbaren Fehlergrenzen
- Protokollausgabe (RTF) mit Residuen, s₀, Redundanz, Maßstab
- Live-Stationierungsberechnung im DXF-Viewer mit Signallampe (grün/gelb/rot)
- Residual-Overlay im DXF-Viewer

### Polarpunkte *(seit v1.9.8)*
- Berechnet Neupunkte aus den Messungen der `{Projekt}-dat.csv` (Hauptmenü → **Polarpunkte**)
- **Orientierung** aus der gespeicherten Station oder neu aus den Anschlusspunkten der dat.csv; Ampel und einzelne Beobachtungen (Hz / Strecke / Höhe) schaltbar wie in der Freien Stationierung; mit einem Anschlussziel bei bekanntem Standpunkt möglich; Information bei weniger als drei Anschlusspunkten
- **Exzentrizitäten**: Längs (positiv = weiter weg) und Quer (positiv = rechts der Messrichtung); Eingabe im DXF-Viewer (nur im Modus Neupunkt), nachträglich korrigierbar in der Maske; Information bei mehr als 2 m
- **Maßstab** der Polarstrecken (Rechenparameter): 1 (Standard), aus der Stationierung berechnet oder vorgegeben; Hinweis bei m ≠ 1; wirkt nicht auf Exzentrizitäten und Höhe
- **Gewichtung bei nahen Zielen** (Rechenparameter, Standard an): Zentrierfehler-Modell bei der Orientierung, Spalte „Gewicht“; wirkt nur im Polarmodul
- Höhe nach Rechenparameter „3D-Berechnung“
- Ausgabe: `Neupunkte.json` (Anzeige im DXF-Viewer) und per Button als `*-kor.csv`

### Punktfinder – automatische Punktidentifikation *(seit v1.9.3)*
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
- **Lokales System** *(seit v1.9.6)*: ohne DXF-Daten, Importpunkte und Stationierung steht der Standpunkt bei 0/0/0; rote gestrichelte Zielrichtung folgt dem Hz-Winkel, Neupunkte werden mit Punktnummer dargestellt
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

Aktuelle Version: **v1.9.8**

| Version | Inhalt |
|---|---|
| v1.9.8 | **Polarpunkte** (neue Maske): Orientierung aus gespeicherter Station oder neu aus der dat.csv mit Ampel und schaltbaren Beobachtungen, Längs-/Querexzentrizität je Punkt (Eingabe im DXF-Viewer, Korrektur in der Maske), Maßstab der Polarstrecken wählbar (1 / berechnet / vorgegeben), Gewichtung bei nahen Zielen (nur Polarmodul), Orientierung auch aus einem Anschlussziel bei bekanntem Standpunkt, Speichern nach Neupunkte.json und als KOR-CSV; dat.csv um Exzentrizität, Standpunkt und Typ (AP/NP) erweitert (alte Dateien werden aufgerüstet); Hauptmenü: Buttons „Polarpunkte“ und „Test GeoCOM“ |
| v1.9.7 | Freie Stationierung: Mehrfachmessungen gleicher Punkte, Zeile entfernen schließt Lücke, Standardabweichung 1-/2-/3-fach (Rechenparameter), Speichern erst bei gültiger Berechnung (≥ 3 grüne Quer-/Längsabweichungen); Messdaten-Dialog grau bis gültige Messung; Zielstrahl-Fix bei starkem Zoom, Zoom max. 1:1; Zielrichtung fragt nur Hz ab und pausiert während der Messung |
| v1.9.6 | Lokales System im DXF-Viewer, gemeinsame Messkonfiguration mit Geräteabgleich, Signaltöne am Tachymeter, Programmicon, Neues Projekt |
| v1.9.5 | DXF-Export/-Viewer erweitert, PunktFinder verbessert, Aufnahmetest-Dokumentation |
| v1.9.4 | Freie Stationierung, DXF-Viewer und Anschlusspunkt verbessert |
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
