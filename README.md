# Feldbuch

**Geodätisches Feldbuch** – eine Windows-Anwendung für die Vermessung im Feld mit Tachymeteranbindung, freier Stationierung, DXF-Viewer und mehr.

## Features

- **Tachymeter-Kommunikation** – Verbindung über COM-Port oder Bluetooth (inkl. Modellauswahl, z.B. Leica TCR307)
- **Freie Stationierung** – automatische Berechnung mit Ausweisung von Längs- und Querabweichungen
- **DXF-Import & -Export** – Import von Koordinaten aus DXF-Dateien, Export von Messpunkten
- **GSI-Import** – Einlesen von GSI-Messdateien
- **Konvertierung** – Koordinatenkonvertierung und Formatumwandlung
- **DXF-Viewer** – Grafische Darstellung mit Zoom (Mausrad) und Overlay-Funktion
- **Protokollgenerator** – Automatische RTF-Protokolle für Freie Stationierungen
- **Projektverwaltung** – Mehrere Projekte, Projektdaten und Rechenparameter verwalten
- **Fehlerprotokoll** – Stille Fehlerbehandlung mit `ErrorLogger`

## Technologie

- **Plattform:** Windows (.NET 10, WinForms)
- **Sprache:** C#
- **Zielrahmen:** `net10.0-windows`
- **Abhängigkeiten:** `System.IO.Ports`, `System.Management`

## Version

Aktuelle Version: **v1.4.1**

## Autor

Johann Gerner – © 2026

## Build & Start

1. Repository klonen:
   ```
   git clone https://github.com/Jgerner01/Feldbuch1.git
   cd Feldbuch1
   ```
2. Solution öffnen: `Feldbuch Universal.slnx` (Visual Studio 2022+)
3. Projekt bauen und starten (F5)

> Voraussetzung: .NET 10 SDK und Windows

## Projektstruktur

```
Feldbuch/           # Hauptprojekt (WinForms)
  Form1.cs          # Hauptformular
  FreieStationierung.cs
  TachymeterVerbindung.cs
  DxfParser.cs / DxfCanvas.cs
  GsiParser.cs
  RtfProtokollGenerator.cs
  ErrorLogger.cs
  icons/            # PNG-Icons
tools/              # Hilfsprogramme (z.B. ExportIcons)
```
