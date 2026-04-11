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

## Autor

Johann Gerner – © 2026

# Anleitung: Bluetooth-Kopplung und COM-Port-Einrichtung (Windows 11)

Diese Anleitung beschreibt die Kopplung eines Bluetooth-Geräts mit der PIN `0000` und die anschließende manuelle Zuweisung einer ausgehenden COM-Schnittstelle.

## 1. Bluetooth-Gerät koppeln

1. Öffnen Sie die **Einstellungen** (`Win + I`).
2. Navigieren Sie zu **Bluetooth & Geräte**.
3. Stellen Sie sicher, dass der Bluetooth-Schalter auf **Ein** steht.
4. Klicken Sie auf **Gerät hinzufügen** -> **Bluetooth**.
5. Wählen Sie Ihr Gerät aus der Liste aus.
6. Geben Sie bei der PIN-Abfrage eine gültige Pin ein und bestätigen Sie mit **Verbinden**. Leica benutzt z.B. **0000**. Oft wird auch die **1234** benutzt. Ggf. beim Gerätehersteller nachfragen.

## 2. Ausgehende COM-Schnittstelle konfigurieren

Da Windows 11 die COM-Ports tiefer im Menü versteckt, folgen Sie diesen Schritten:

1. Scrollen Sie unter **Bluetooth & Geräte** nach unten und klicken Sie auf **Geräte**.
2. Scrollen Sie ganz nach unten zum Bereich "Verwandte Einstellungen" und klicken Sie auf **Weitere Bluetooth-Einstellungen**.
3. Ein neues Fenster öffnet sich. Wechseln Sie zum Reiter **COM-Anschlüsse**.
4. Klicken Sie auf die Schaltfläche **Hinzufügen...**.
5. Wählen Sie die Option **Ausgehend (der Computer initiiert die Verbindung)**.
6. Wählen Sie Ihr Gerät aus dem Dropdown-Menü aus und bestätigen Sie mit **OK**.
7. Notieren Sie sich den zugewiesenen Port (z. B. `COM3`), der nun in der Liste erscheint.

---

