# Aufnahmetest – Motorisierter Tachymeter: Koordinatenaufnahme & Anfahren

**Modul**: DXF-Viewer  
**Protokoll**: Leica GeoCOM (TPS1200, motorisiert)  
**Status**: Plan – noch kein Code

---

## 1. Zielsetzung

Erweiterung des DXF-Viewers um eine Testfunktion für motorisierte Tachymeter mit zwei Kernfunktionen:

1. **Aufnahme**: Messung auslösen → lokale Koordinaten (Instrument = Ursprung 0/0) berechnen → in bearbeitbarer Tabelle speichern.
2. **Anfahren**: Koordinate aus der Tabelle auswählen → Tachymeter dreht automatisch auf diese Position.

---

## 2. Architektur-Entscheidung: Separates Formular

FormDxfViewer hat bereits 1861 Zeilen. Eine weitere Einbettung würde die Klasse weiter überfrachten. Empfohlen wird ein **eigenständiges Formular `FormAufnahmetest`**, das:

- aus dem DXF-Viewer über einen neuen Button geöffnet wird (nicht-modal, bleibt im Hintergrund offen),
- `TachymeterVerbindung.DatenEmpfangen` direkt abonniert,
- die gleiche GeoCOM-Zustandsmaschine wie FormDxfViewer verwendet.

---

## 3. Neue GeoCOM-RPCs (AUT-Subsystem)

Noch nicht im `GeoCOMParser` vorhanden. Müssen ergänzt werden:

| Konstante | RPC | Funktion |
|---|---|---|
| `RPC_AUT_MakePositioning` | 9027 | Tachymeter auf Hz/V drehen |
| `RPC_AUT_FineAdjust` | 9037 | ATR-Feineinstellung auf Prisma |
| `RPC_AUT_Search` | 9029 | Prismasuche in Suchfenster |
| `RPC_MOT_ReadLockStatus` | 6021 | Motor-Sperrstatus lesen |
| `RPC_MOT_StartController` | 6001 | Motorsteuerung aktivieren |
| `RPC_MOT_StopController` | 6002 | Motorsteuerung stoppen |

### AUT_MakePositioning (9027) – Befehlsformat

```
%R1Q,9027,0:{Hz_rad},{V_rad},{PosTol_m},{AngTol_rad},{bDualFace}\r\n
```

- `Hz_rad`: Sollwinkel horizontal [rad]
- `V_rad`: Sollwinkel vertikal [rad] (für horizontales Anzielen: π/2 = 1.5708)
- `PosTol_m`: Positionstoleranz [m], typisch `0.0001`
- `AngTol_rad`: Winkeltoleranz [rad], typisch `0.000154` (≈ 5 mgon)
- `bDualFace`: 0 = Einzel-Lage, 1 = Zwei-Lagen

Antwort: `%R1P,0,0:{RC}` — nur Returncode, keine weiteren Daten.  
Der RPC **blockiert** das Gerät bis die Position erreicht oder ein Fehler auftritt (typisch 3–15 s). Timeout im PC muss entsprechend groß sein (≥ 30 s).

---

## 4. Koordinatenberechnung

### 4.1 Aufnahme – polar → lokal kartesisch

Instrument steht im Ursprung (0/0). X = Ost, Y = Nord.

```
Hz_rad    = Hz_gon × π/200
V_rad     = V_gon  × π/200
D_horiz   = D_schraeg × sin(V_rad)        [= Horizontalstrecke]
Delta_Z   = D_schraeg × cos(V_rad)        [= Höhenunterschied]

X_lokal   = D_horiz × sin(Hz_rad)
Y_lokal   = D_horiz × cos(Hz_rad)
Z_lokal   = Delta_Z                        [optional, nur wenn V ≠ 100 gon]
```

`D_horiz` und `Delta_Z` werden von `GeoCOMParser.BerechneAbgeleiteteMasse()` bereits berechnet und in `TachymeterMessung` abgelegt → direkte Nutzung möglich.

### 4.2 Anfahren – lokal kartesisch → polar

```
Hz_rad   = atan2(X_lokal, Y_lokal)         [atan2 mit X und Y vertauscht, da Nord = 0°]
wenn Hz_rad < 0: Hz_rad += 2π              [Wrap auf [0, 2π)]
V_rad    = π/2                             [horizontal: Zenitwinkel 100 gon]
```

Der Nutzer kann `V_rad` auch manuell vorgeben oder aus der letzten Messung übernehmen.

---

## 5. Formular-Aufbau `FormAufnahmetest`

```
┌─ Aufnahmetest – Motorisierter Tachymeter ──────────────────────────────────┐
│  Verbindung: ● TPS1200 – COM3 – 38400 Bd                                   │
│                                                                             │
│  ┌─ Messung ───────────────────────────────┐  ┌─ Anfahren ───────────────┐ │
│  │  [Messen]   Reflektorlos □ Prisma ■     │  │  Zenitwinkel V: [100.000]│ │
│  │  Status: Hz=123.4567  V=99.3210  D=23.4│  │  ATR □   Suche □         │ │
│  │  Punktnummer: [____]  Code: [____]      │  │  [Anfahren]  [Stopp]     │ │
│  └────────────────────────────────────────┘  └──────────────────────────┘ │
│                                                                             │
│  ┌─ Koordinatenliste (lokal, Instrument = 0/0) ───────────────────────────┐│
│  │  Nr  │ PunktNr │  X [m]   │  Y [m]   │  Z [m]   │ D [m] │ Hz [gon] │ ││
│  │   1  │ P001    │  12.345  │  -8.901  │   0.412  │ 15.34 │ 123.457  │ ││
│  │   2  │ P002    │   …      │   …      │   …      │  …    │   …      │ ││
│  │  [+] [−] [Bearbeiten]          [CSV exportieren]  [DXF anzeigen]     ││
│  └────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│  [Schließen]                                                  [In DXF zeigen]│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. GeoCOM-Zustandsmaschine – Anfahren

Das Anfahren ist asynchron (Gerät dreht, antwortet erst nach Erreichen der Position):

```
Bereit
  → [Anfahren geklickt]
  → SendeBefehl AUT_MakePositioning  →  ZustandWartePositionierung
  → Timeout-Timer starten (30 s)
  → Antwort %R1P,0,0:{RC}
      RC=0   → OK  → MessungAbschliessen → optional: ATR-FineAdjust
      RC≠0   → Fehler anzeigen (Timeout, Hindernis, Motor gesperrt)
  → Bereit
```

Wichtig: Während `ZustandWartePositionierung` müssen alle anderen Buttons gesperrt werden (kein Messen, kein weiteres Anfahren).

---

## 7. Geplante Dateien

| Datei | Aktion | Inhalt |
|---|---|---|
| `FormAufnahmetest.cs` | neu | Hauptlogik: Messung, Koordinatenberechnung, Anfahren |
| `FormAufnahmetest.Designer.cs` | neu | WinForms-Designer-Code |
| `GeoCOMParser.cs` | erweitern | RPC-Konstanten für AUT/MOT-Subsystem, `ParseAntwort`-Zweige |
| `TachymeterMessung.cs` | prüfen | Ggf. `X_lokal`, `Y_lokal`, `Z_lokal` als Felder ergänzen |
| `FormDxfViewer.cs` | erweitern | Button „Aufnahmetest" zum Öffnen des Formulars |

---

## 8. Vorgeschlagene Erweiterungen

### 8.1 ATR-Modus (Automatic Target Recognition)
Checkbox im Formular. Wenn aktiv: nach `AUT_MakePositioning` automatisch `AUT_FineAdjust` senden (RPC 9037). Das Gerät zentriert dann präzise auf das Prisma.

```
%R1Q,9037,0:{dSrchHz_rad},{dSrchV_rad}
```
Suchfenster typisch ±0.05 rad (≈ ±3.2 gon).

### 8.2 Prismasuche vor FineAdjust
Optional: `AUT_Search` (9029) mit konfigurierbarem Suchfenster, bevor FineAdjust aufgerufen wird. Sinnvoll bei größeren Positionsungenauigkeiten oder nach längerem Stillstand.

### 8.3 Zwei-Lagen-Messung
Parameter `bDualFace=1` in `AUT_MakePositioning` aktiviert Zwei-Lagen. Erhöht Messgenauigkeit, dauert ~doppelt so lang. Als optionale Checkbox.

### 8.4 DXF-Overlay-Integration
Gemessene lokale Punkte können als Overlay-Entities in den DXF-Canvas von FormDxfViewer projiziert werden (ähnlich `Neupunkte-Overlay`). Erfordert eine Koordinatentransformation lokal → Weltkoordinaten, wenn eine Stationierung vorliegt.

### 8.5 Rückwärtiger Export / Import
- **Export**: Koordinatenliste als lokale `.kor`-CSV (Muster-kor.csv-Format) speichern.
- **Import**: Bestehende Koordinatenliste laden und daraus Anfahrpunkte generieren (Absteckung aus lokalen Koordinaten).

### 8.6 Toleranzüberwachung beim Anfahren
Nach erfolgtem Anfahren eine neue Messung auslösen und die Ist-Position mit der Soll-Position vergleichen. Abweichung > Schwellwert → Warnung anzeigen. Nützlich für Prüfzwecke.

### 8.7 Wiederholtes Anfahren (Batch-Modus)
Liste automatisch durchlaufen: Punkt 1 anfahren → messen → Punkt 2 anfahren → messen … → Ergebnisse exportieren. Geeignet für automatisierte Prüfmessungen.

---

## 9. Offene Fragen / Abzuklären

1. **Timeout-Wert**: Wie lange braucht der TPS1200 maximal zum Drehen (180° Drehung bei langsamer Geschwindigkeit)? Empfehlung: 30 s konfigurierbar.
2. **V-Winkel beim Anfahren**: Immer horizontal (100 gon) oder letzten gemessenen V-Winkel verwenden?
3. **Motorstatus prüfen**: Soll vor dem Anfahren `MOT_ReadLockStatus` (6021) abgefragt werden, um sicherzustellen, dass der Motor nicht gesperrt ist?
4. **Koordinatensystem**: Nur lokal (Instrument = 0/0) oder optional mit Transformation in Weltkoordinaten wenn Stationierung vorliegt?
5. **Persistenz**: Soll die Koordinatenliste zwischen Sitzungen gespeichert werden (CSV im Projektverzeichnis) oder nur im Arbeitsspeicher gehalten werden?

---

## 10. Abhängigkeiten & Risiken

| Thema | Bewertung |
|---|---|
| GeoCOM-Verbindung muss aktiv sein | Pflicht – Button graut aus wenn nicht verbunden |
| Tachymeter muss motorisiert sein | Kein programmatischer Check möglich; Fehlermeldung bei RC≠0 |
| AUT_MakePositioning blockiert das Gerät | Timeout-Handling und Abbrechen-Funktion zwingend erforderlich |
| Gleichzeitige Nutzung mit FormDxfViewer-Messung | Konfliktvermeidung: Messen im DXF-Viewer sperren während Anfahren läuft |
