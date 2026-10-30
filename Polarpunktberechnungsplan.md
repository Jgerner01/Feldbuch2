# Polarpunktberechnung – Detailplan

Stand: 2026-10-30 | Status: **Planungsdokument – noch nichts codiert** (Entwurf 4, Änderungen gegenüber den Vorentwürfen siehe Kap. 0; **alle Fragen beantwortet** – wartet auf Freigabe zum Codieren)

Zugehörig: `Gewichtungsanalyse.md` (Vor-/Nachteile und Umsetzung der „Gewichtung bei nahen Zielen")

## Umsetzungsstand (Version 1.9.8, noch nicht committet)

Umgesetzt wie geplant: Hauptmenü, `dat.csv`-Erweiterung mit Migration, `PolarpunktRechner`, Maßstab und Gewichtung als Rechenparameter,
Exzentrizitätsfelder im DXF-Viewer (nur Neupunkt-Modus), Maske `FormPolarpunkte`, Speichern nach `Neupunkte.json` (`Quelle = Polar`),
KOR-CSV-Button, Orientierung aus einem Anschlussziel, Tests (82 grün) und README.

**Abweichungen vom Plan:**

- **Protokoll:** Statt einer RTF-Vorlage (`Polarpunkte_Protokoll.xml`, `PolarpunkteProtokoll.cs`) schreibt „Speichern“ Zeilen (Kategorie `POLAR`) ins
  Tagesprotokoll (`ProtokollManager`, nur bei aktiver Option „Protokoll“). Einen eigenen Button „Protokoll“ in der Maske gibt es nicht.
- **Gewichtungswirkung:** Die Gewichtung halbiert den Einfluss eines nahen Ziels etwa; die im Plan genannte Verbesserung auf < 1 cc war zu optimistisch
  (siehe `Gewichtungsanalyse.md`, Kap. 3).
- **Importpunkte nach dem KOR-CSV-Schreiben** werden nicht automatisch neu geladen (die Datei kann im DXF-Viewer wie jede KOR-CSV geladen werden).
- **Mehrfachmessungen gleicher Punktnummer:** In der Maske getrennte Zeilen; beim Speichern nach `Neupunkte.json` gilt weiterhin „gleiche Punktnummer
  wird ersetzt“ (die letzte Zeile gewinnt).
- **Dialog „Rechenparameter“:** zweispaltig (neue Gruppen rechts), Titel „Rechenparameter – Stationierung und Polarpunkte“, linke Maßstabsgruppe heißt
  „Maßstab (Stationierung)“.
- Die Maske wurde nur über Rauchtests und Bildschirmfotos geprüft, **nicht** am echten Gerät und nicht mit einem echten Projekt.

---

## 0  Änderungen gegenüber den Vorentwürfen

| Thema | Entscheidung | Entwurf |
|---|---|---|
| Vorzeichen der Exzentrizitäten | wie beschrieben bestätigt (Kap. 6.3) | 2 |
| Maßstab | neuer Rechenparameter mit **drei Varianten**: 1 (Standard) / berechnet / vorgegeben (von Hand, später per UTM-Funktion); Hinweis, sobald ≠ 1 (Kap. 6.5, 7.4) | 2 |
| Speichern bei wenig Anschlusspunkten | Speichern ist auch mit **einem** Anschlussziel möglich (bei bekanntem Standpunkt); bei **weniger als drei** erscheint eine Information (Kap. 7.3, 8.3) | 2, in 3 bestätigt |
| Gültigkeitsregel „≥ 3 grüne Quer- und Längsabweichungen" | entfällt als **Sperre**, bleibt als Information (Kap. 8.3) | 2, in 3 bestätigt |
| Maßstab und Exzentrizitäten | der Maßstab wirkt **nicht** auf die Exzentrizitäten (Kap. 6.2) | 3 |
| Große Exzentrizität | Information, wenn `|eL|` oder `|eQ|` größer als **2 m** (Kap. 6.6) | 3 |
| KOR-CSV | zusätzlicher Button „Als KOR-CSV speichern" in der Maske (Kap. 8.4) | 3 |
| Nahe Anschlussziele | neuer Rechenparameter „**Gewichtung bei nahen Zielen**" (Standard: aktiv) mit Gewichtsberechnung bei der Orientierung (Kap. 7.5, Analyse in `Gewichtungsanalyse.md`) | 3 |
| Wirkungsbereich der Gewichtung | **nur im Polarmodul**; Freie Stationierung und DXF-Viewer bleiben unverändert | 4 |
| Standardwerte Gewichtung | Zielzentrierung 2,0 mm, Standpunktzentrierung 0,0 mm | 4 |
| Ampel `v Winkel [cc]` | **feste Grenze** (nicht entfernungsabhängig) | 4 |
| Zusatzanzeigen zur Gewichtung | **nur** die Spalte „Gewicht"; kein Nahziel-Hinweis, keine effektive Redundanz | 4 |
| Höhe | bekommt **kein** Gewicht, auch nicht später; Höhenberechnung bleibt wie bisher | 4 |
| KOR-CSV mit vorhandener Datei | Punkte gleicher Nummer werden ersetzt, andere bleiben erhalten | 4 |

---

## 1  Ziel

Neues Modul **„Polarpunkte"**: Aus den polaren Messungen einer `*-dat.csv` werden Neupunktkoordinaten
(R, H, Höhe) berechnet. Dabei gilt:

- Der **Richtungswinkel** (Orientierung des Standpunkts) kommt aus der Freien Stationierung bzw. wird aus den
  Anschlusspunkt-Messungen der `dat.csv` neu bestimmt.
- Die Messungen, die in den Richtungswinkel eingehen, werden in einer **Maske mit Ampel** ausgewählt
  (einzelne Werte an-/abschaltbar, wie bei der Freien Stationierung).
- Jede Messung kann eine **Längs-** und eine **Querexzentrizität** tragen, die in die Lageberechnung eingeht.
- Die **Höhenberechnung** richtet sich nach dem Rechenparameter „3D-Berechnung (mit Höhen)".
- Der **Maßstab** der Polarstrecken ist in den Rechenparametern wählbar (Standard: 1).

---

## 2  Hauptmenü (Form1)

Aktuelle Anordnung im Abschnitt FELDARBEIT:

```
[ DXF-Viewer / Aufnahme                          ]
[ Freie Stationierung ] [ Tachymeter             ]
[ Testmessungen  GeoCOM                          ]
```

Neue Anordnung:

```
[ DXF-Viewer / Aufnahme                          ]
[ Freie Stationierung ] [ Polarpunkte            ]
[ Tachymeter          ] [ Test GeoCOM            ]
```

| Button | Text | Position | Größe | Farbe | Aktion |
|---|---|---|---|---|---|
| `btnPolarpunkte` (neu) | „Polarpunkte" | rechts neben „Freie Stationierung" (Platz des bisherigen „Tachymeter") | Halbbreite (208 × 42) | grau (`GrauButton`, wie Freie Stationierung) | öffnet `FormPolarpunkte` |
| `btnTachymeterKommunikation` | „Tachymeter" (unverändert) | Zeile 3, links | Halbbreite | grün (unverändert) | unverändert |
| `btnTestmessungen` | **„Test GeoCOM"** (bisher „Testmessungen  GeoCOM") | Zeile 3, rechts | Halbbreite | orange (unverändert) | unverändert |

Geändert werden nur `Form1.Designer.cs` (Positionen/Größen/Text, neuer Button samt Controls-Liste) und
`Form1.cs` (Click-Handler). Die Gesamthöhe des Hauptfensters bleibt gleich (gleiche Zeilenzahl).

---

## 3  Wohin werden Messdaten bei „Neupunkt" + „Messen" gespeichert? (Prüfergebnis)

Ablauf in `FormDxfViewer.VerarbeiteMiessung()` (Neupunkt-Modus). Pro Messung entstehen **drei** Ablagen:

| Ablage | Datei | Inhalt | Anmerkung |
|---|---|---|---|
| 1. Messdaten | `{Projekt}-dat.csv` (`MessdatenCSV.SchreibeNeupunktmessung`) | PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel | **Das ist die gewünschte `*dat.csv`** ✔ |
| 2. Neupunkte | `{Projekt}-Neupunkte.json` (`NeupunkteManager`) | Rohdaten + berechnete Koordinaten | wird nur geschrieben, wenn eine Station/ein lokales System vorliegt; **gleiche PunktNr wird ersetzt** |
| 3. Tagesprotokoll | `Data-protocol-{yyyy-MM-dd}.csv` (`MessdatenProtokoll`) | alle Messdaten inkl. EDM-Modus, Prismenkonstante, Neigung usw. | lückenlos, append-only |

**Befunde, die für das Polarmodul wichtig sind:**

1. In der `dat.csv` stehen **Neupunkt- und Anschlusspunkt-Messungen im selben Format** untereinander
   (`SchreibeStationierungsmessung` schreibt dieselben 7 Spalten). Es gibt keine Kennzeichnung, welche Zeile
   ein Anschlusspunkt und welche ein Neupunkt ist.
2. Die **StandpunktNr steht nur im Dateikopf** (Wert beim Anlegen der Datei) bzw. in `# STATIONIERUNG:`-Kommentaren,
   die erst nach einer Stationierungsberechnung geschrieben werden. Eine Neupunktzeile weiß nicht, zu welchem
   Standpunkt sie gehört, sobald in derselben Datei der Standpunkt gewechselt wird.
3. `SicherstelleDatei()` prüft nur die erste Zeile (`# METADATA`). Ändert man die Spaltenzeile (Kap. 4), würden alte
   Dateien Zeilen mit mehr Werten als Spalten bekommen → **Migration alter Dateien nötig** (Kap. 4.3).
4. Die Rohdaten in `Neupunkte.json` kennen die Exzentrizitäten ebenfalls noch nicht (Kap. 4.4).

---

## 4  Datenformat `dat.csv` (Erweiterung)

### 4.1  Neue Spalten

Spaltenzeile bisher:

```
PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel
```

Neu (hinten angehängt, damit alte Leser/Dateien kompatibel bleiben):

```
PunktNr; HZ_gon; V_gon; Strecke_m; Zielhoehe_m; Code; Zeitstempel; ExzLaengs_m; ExzQuer_m; StandpunktNr; Typ
```

| Spalte | Format | Bedeutung |
|---|---|---|
| `ExzLaengs_m` | `F3`, Standard `0.000` | Längsexzentrizität (in/gegen Messrichtung), siehe Kap. 6 |
| `ExzQuer_m` | `F3`, Standard `0.000` | Querexzentrizität (senkrecht zur Messrichtung), siehe Kap. 6 |
| `StandpunktNr` | Text | Standpunkt, von dem aus gemessen wurde |
| `Typ` | `AP` / `NP` | Anschlusspunkt oder Neupunkt |

Die beiden Exzentrizitätsspalten sind die geforderten „zwei weiteren Spalten". `StandpunktNr` und `Typ` sind
mein Zusatzvorschlag, weil ohne sie die Trennung Anschlusspunkt/Neupunkt und die Standpunktzuordnung nur
geraten werden kann (Befunde 1 und 2).

### 4.2  Lesen alter Dateien

- Zeilen mit nur 7 Werten: `ExzLaengs = ExzQuer = 0`.
- Ohne `Typ`-Spalte: Heuristik – PunktNr kommt in den Koordinaten (`-kor.csv` / Importpunkte / Feldbuchpunkte)
  vor → Anschlusspunkt, sonst Neupunkt.
- Ohne `StandpunktNr`-Spalte: Standpunkt aus `# STATIONIERUNG:`-Kommentar davor, sonst aus dem Dateikopf.

### 4.3  Schreiben / Migration

- `MessdatenCSV.SchreibeNeupunktmessung` und `SchreibeStationierungsmessung` schreiben die neuen Spalten.
- `SicherstelleDatei()` erkennt eine alte Spaltenzeile und **rüstet sie auf** (Spaltenzeile ersetzen, alte
  Datenzeilen mit `0.000; 0.000; ; ` auffüllen) – vorher Sicherungskopie `…_backup_….csv` wie bisher.
- `CsvDatenDatei.StandardSpaltenDat` und `Muster-dat.csv` werden angepasst; der Daten-Editor muss die zusätzlichen
  Spalten verkraften (beim Codieren prüfen).

### 4.4  `Neupunkte.json`

`NeupunktRohdaten` und `NeupunktErgebnis` bekommen `ExzLaengs_m` und `ExzQuer_m` (Standard 0, Deserialisierung
alter Dateien bleibt möglich). `NeupunktErgebnis` speichert zusätzlich den **verwendeten Maßstab**
(`VerwendeterMassstab`, Standard 1) und die Herkunft (`Quelle`: leer = DXF-Viewer, `Polar` = Polarmodul, Kap. 7.5). `NeupunktRechner.Berechnen()` wendet Exzentrizität und Maßstab an (Kap. 6),
damit der DXF-Viewer und das neue Modul **dieselbe** Rechenroutine benutzen.

---

## 5  Wo werden die Exzentrizitäten eingegeben? (Vorschlag)

**Empfehlung: an zwei Stellen, mit je klarer Aufgabe.**

### A  Bei der Messung im DXF-Viewer (Hauptweg)

Die Eingabeleiste (von rechts: `Code – Nr – ZH – Messen`) bekommt zwei kleine Felder direkt neben der Zielhöhe:

```
… Code [____]  Nr [____]  ZH [0.000]  eL [0.000]  eQ [0.000]  [Messen] …
```

- `eL` = Längsexzentrizität, `eQ` = Querexzentrizität, in m, Tooltip erklärt Vorzeichen (Kap. 6.3).
- **Nur im Modus „Neupunkt" sichtbar** (im Stationierungsmodus ausgeblendet → Leiste bleibt schmal, kein Rückfall
  in das „abgeschnittene Felder"-Problem).
- Nach jeder gültigen Messung werden beide Felder auf `0.000` zurückgesetzt (Exzentrizität gilt meist nur für
  einen Punkt; vergessene Werte würden sonst stillschweigend den nächsten Punkt verfälschen).
- Die Werte werden mit der Messung in die `dat.csv` (Spalten 8/9) und in `Neupunkte.json` geschrieben.
- Die Koordinatenanzeige im Viewer (R/H im Status) enthält die Exzentrizität bereits.

### B  Nachträglich in der Polarpunkt-Maske (Korrektur)

In der Tabelle der Polarpunkte (Kap. 8) sind `Exz längs` und `Exz quer` **editierbare Spalten**. Änderung →
sofortige Neuberechnung → Speichern schreibt die korrigierten Werte in die `dat.csv` zurück. So lassen sich
vergessene oder falsch eingegebene Exzentrizitäten ohne Neumessung nachtragen.

### Verworfene Alternativen

- *Nur im Messdaten-Dialog (`FormMessdatenEingabe`)*: Der Dialog gehört zum Anschlusspunkt-Ablauf, nicht zu
  Neupunkten.
- *Eigener Dialog nach jeder Messung*: bremst den Messablauf.

---

## 6  Rechenverfahren Polarpunkt

### 6.1  Eingangsdaten

Standpunkt `R_S, H_S, Höhe_S` und Richtungswinkel-Orientierung `o` (gon) aus der Station (Kap. 7);
Messung `Hz, V, S` (Schrägstrecke), `ZH`, Instrumentenhöhe `IH`, Exzentrizitäten `eL, eQ`,
Maßstab `m` (Kap. 6.5).

### 6.2  Formeln

```
t   = (Hz + o) mod 400                    orientierter Richtungswinkel (Messrichtung) [gon]
d   = S · sin(V)                          Horizontalstrecke zum gemessenen Reflektor [m]

R   = R_S + m · d · sin(t) + eL · sin(t) + eQ · cos(t)
H   = H_S + m · d · cos(t) − eQ · sin(t) + eL · cos(t)
```

Das entspricht: erst Punkt P' am Reflektor, dann Verschiebung um `eL` entlang der Messrichtung und um `eQ`
senkrecht dazu nach rechts (Blickrichtung Standpunkt → Ziel). Der Maßstab `m` wirkt **nur auf die gemessene
Horizontalstrecke `d`**, **nicht** auf die Exzentrizitäten (Entscheidung Entwurf 3). Mit `m = 1` (Standard)
entfällt er. Die Exzentrizitäten wirken **nur in der Lage**.

### 6.3  Vorzeichenkonvention (bestätigt)

| Größe | positiv | negativ |
|---|---|---|
| Längsexzentrizität `eL` | Zielpunkt liegt **weiter vom Standpunkt weg** als der Reflektor | Zielpunkt liegt näher am Standpunkt |
| Querexzentrizität `eQ` | Zielpunkt liegt **rechts** der Messrichtung (Blick vom Standpunkt zum Ziel) | links |

Rechenbeispiel zur Kontrolle (Standpunkt 0/0, `o` = 0, `m` = 1): `Hz` = 100 gon (Richtung Ost), `V` = 100 gon, `S` = 50 m.

| eL | eQ | R | H |
|---|---|---|---|
| 0 | 0 | 50.000 | 0.000 |
| +1.000 | 0 | 51.000 | 0.000 |
| 0 | +1.000 | 50.000 | −1.000 (rechts von Ost = Süd) |

### 6.4  Höhe (aus „Rechenparameter")

- `RechenparameterManager.Params.Berechnung3D = false` → **reine 2D-Berechnung**, Höhenspalte leer/„–".
- `Berechnung3D = true` **und** Standpunkthöhe vorhanden (Station mit Höhe ≠ 0, oder lokales System):
  `Höhe = Höhe_S + S · cos(V) + IH − ZH`
- `Berechnung3D = true`, aber Station ohne Höhe → 2D mit Hinweis in der Statuszeile („Station ohne Höhe").
- Die Höhe wird durch `eL`/`eQ` und durch den Maßstab **nicht** verändert (der Maßstab ist ein Lage-Maßstab).

Gegenüber heute ändert sich: Die 3D-Entscheidung hängt künftig am **aktuellen** Rechenparameter und nicht nur am
Merker, der bei der Stationierungsberechnung im Ergebnis gespeichert wurde.

### 6.5  Maßstab (neuer Rechenparameter)

Der bestehende Parameter „Freier Maßstab" steuert nur die **Ausgleichung der Stationierung** (m frei oder m = 1
fixiert). Er bleibt unverändert. Neu ist ein davon getrennter Parameter, der festlegt, **welcher Maßstab bei der
Polarberechnung angewendet wird**:

| Modus | Wert von `m` | Hinweis |
|---|---|---|
| **1 – ohne Maßstab** (Standard) | `m = 1,000000` | kein Hinweis |
| **Berechnet** | `m` aus der Stationierung (Helmert/Ausgleichung des Standpunkts) | Hinweis, sobald `m ≠ 1` |
| **Vorgegeben** | fester Wert, von Hand eingegeben oder per Funktion berechnet (später UTM) | Hinweis, sobald `m ≠ 1` |

**Dialog „Rechenparameter"**: neue Gruppe „Maßstab (Polarpunkte)":

```
┌ Maßstab (Polarpunkte) ──────────────────────────────────────────┐
│ (•) 1  (ohne Maßstab)                                           │
│ ( ) berechneter Maßstab aus der Stationierung                   │
│ ( ) vorgegebener Maßstab:  [ 0.999600 ]  [ Aus UTM berechnen… ] │
└─────────────────────────────────────────────────────────────────┘
```

- Der Button „Aus UTM berechnen…" ist **vorbereitet, aber in der ersten Ausbaustufe deaktiviert** (Hinweistext
  „folgt"). Er füllt später das Feld `m`. Die Eingabe von Hand bleibt immer möglich. Gespeichert wird zusätzlich
  die Herkunft (`manuell` / `UTM`), damit sie im Protokoll steht.
- Plausibilitätsprüfung des Eingabewerts (benannte Konstanten, z. B. `0,9 < m < 1,1`), Dezimalkomma und -punkt.
- Modus „berechnet" ohne verfügbaren Maßstab (z. B. Stationierung mit festem `m = 1`, lokales System, 1 Anschlusspunkt
  ohne Strecke): Rückfall auf `m = 1` **mit** Hinweis „Maßstab nicht berechenbar – m = 1 verwendet".
- Speicherung in der bestehenden Rechenparameter-Datei (`Freie-Stationierung.xml`), neue Elemente mit Standardwerten
  → alte Dateien bleiben lesbar und laufen mit `m = 1`:
  `PolarMassstabModus` (`Eins`/`Berechnet`/`Vorgegeben`), `PolarMassstabWert` (Standard `1.000000`),
  `PolarMassstabQuelle`.
- Der Parameter wirkt zentral in `NeupunktRechner` und damit **auch im DXF-Viewer**. Mit dem Standard `m = 1` bleiben
  alle bisherigen Ergebnisse unverändert (Regressionsschutz).
- Der Maßstab wirkt nur auf die gemessene Horizontalstrecke, nicht auf Exzentrizitäten und nicht auf die Höhe.
- Die Richtung der Maßstabswirkung (Koordinatenstrecke = `m` · gemessene Strecke) wird beim Codieren gegen
  `FreieStationierungRechner` geprüft: Mit Modus „berechnet" müssen die Anschlusspunkte die Streckenresiduen der
  Stationierung reproduzieren (Test, Kap. 11).
- **Hinweisausgabe, sobald `m ≠ 1`** (Toleranz benannte Konstante):
  1. gelber Hinweisbalken in der Polarpunkt-Maske: „Berechnung mit Maßstab m = 0.999600 (vorgegeben)",
  2. Zeile im Protokoll (Kap. 9) und im Tagesprotokoll-Kommentar,
  3. im DXF-Viewer Zusatz in der Statuszeile nach der Neupunktmessung (z. B. „… · m = 0.999600").
- Ausblick UTM-Funktion (nicht Teil dieser Ausbaustufe): Streckenreduktion `k ≈ k0 · (1 + (E − 500 000)² / (2 · R²))`
  mit `k0 = 0,9996` und `R ≈ 6 381 km`, `E` = Abstand zum Mittelmeridian; Eingabe: Rechtswert des Standpunkts
  (Näherung vor Umsetzung noch zu prüfen, ggf. mit Höhenreduktion).

### 6.6  Information bei großer Exzentrizität

Ist `|eL| > 2 m` oder `|eQ| > 2 m` (benannte Konstante `EXZ_INFO_GRENZE_M = 2.0`), wird eine **Information** ausgegeben –
die Berechnung wird nicht gesperrt:

- im DXF-Viewer bei der Messung: Zusatz in der Statuszeile („⚠ Exzentrizität eL = 2.350 m > 2 m"),
- in der Maske: betroffene Zelle gelb hinterlegt, Zeile im Hinweisbereich („Punkt 7: Exzentrizität > 2 m"),
- im Protokoll.

Bei Eingabe außerhalb der Grenze im Viewer-Feld wird das Feld gelb markiert (Tooltip mit Hinweis), die Messung bleibt möglich.

---

## 7  Richtungswinkel / Orientierung

### 7.1  Quelle (zwei Wege, in der Maske umschaltbar)

| Quelle | Herkunft | Wann sinnvoll |
|---|---|---|
| **a) Gespeicherte Stationierung** (Standard) | `Station-{SP}.json` bzw. `ProjektdatenManager` „Freie Stationierung" (`Orientierung [gon]`, R, H, Höhe, Maßstab, s0) | Stationierung wurde bereits berechnet (Freie Stationierung oder DXF-Viewer) |
| **b) Neu aus der `dat.csv`** | Anschlusspunkt-Zeilen (`Typ = AP`) des gewählten Standpunkts; Koordinaten der Anschlusspunkte aus Import-/Feldbuchpunkten (wie `SucheAnschlusspunkt`) | Messung liegt nur als Datei vor, oder Auswahl der Beobachtungen soll geändert werden |

Bei **b)** läuft dieselbe Ausgleichung wie bisher: `FreieStationierungRechner.Berechnen()` mit
`aktivRichtung[]`, `aktivStrecke[]`, `aktivHoehe[]`. Es gibt **keine neue Ausgleichung**.
Das Ergebnis liefert R, H, Höhe, **Orientierung `o`**, Maßstab, s0, Residuen.

Bei **a)** werden die gespeicherten Messungen des Standpunkts zusammen mit dem Ergebnis geladen; die Maske zeigt
dann Residuen und Aktivierung genauso an und erlaubt eine Neuberechnung (identisch zu `FormErgebnis`).

### 7.2  Lokales System

Kein DXF/keine Anschlusspunkte: Standpunkt 0/0/0, `o` = 0 gon, `m` = 1; die Ampel-Tabelle entfällt, Hinweis
„Lokales System – keine Anschlusspunkte erforderlich". Speichern ist immer möglich.

### 7.3  Wenige Anschlusspunkte

Gezählt werden **Anschlusspunkte** (verschiedene Punktnummern mit mindestens einer aktiven Beobachtung).

| Anzahl | Verhalten |
|---|---|
| **≥ 3** | normale Ausgleichung (Redundanz r > 0), volle Ampel |
| **2** | Ähnlichkeitstransformation (Direktlösung, r = 0 oder wenig Redundanz) wie bisher; **Information** beim Speichern |
| **1** | (bestätigt) **Standpunkt muss bekannte Koordinaten haben** (Feldbuch-/Importpunkt mit der Standpunktnummer, „fester Standpunkt"). Orientierung aus der einen Richtung: `o = t(SP → AP) − Hz_AP`. Ampel: `v Winkel` und `v Quer` nicht bestimmbar („–", grau), `v Längs`/`v Höhe` nur, wenn Strecke/Höhe aktiv. Maßstab „berechnet" aus `Koordinatenstrecke / gemessene Strecke`, falls Strecke aktiv. **Information** beim Speichern. Dafür ist eine kleine neue Rechenfunktion in `PolarpunktRechner` nötig (`FreieStationierungRechner` verlangt mindestens 2 Punkte). |
| **0** | Berechnung nicht möglich (außer lokales System) |

**Information bei weniger als drei Anschlusspunkten** (dauerhaft in der Maske sichtbar und beim Speichern):
„Nur n Anschlusspunkt(e): Orientierung nicht überbestimmt, Qualität kann nicht beurteilt werden (empfohlen: mindestens 3)."
Zusätzlich Eintrag im Protokoll.

### 7.4  Maßstab bei der Orientierung

Der gewählte Modus (Kap. 6.5) bestimmt, welches `m` aus der Station verwendet wird. Bei „Berechnet" wird das `m`
der **gerade in der Maske gerechneten** Stationierung benutzt (ändert sich also beim An-/Abschalten von Beobachtungen).

### 7.5  Gewichtung bei nahen Zielen (neuer Rechenparameter, Standard: aktiv)

**Problem:** Ein Anschlussziel nahe am Standpunkt hat wegen kleiner Lotfehler eine schlechte Richtung (2 mm Lagefehler =
127 cc bei 10 m) und verfälscht bei gleichem Gewicht die Orientierung und damit alle Polarpunkte.
**Befund im Code:** Heute geht jede Richtung mit demselben Gewicht in die Orientierung ein, weil sich die Strecke aus
Koeffizient und Gewicht herauskürzt; die Zentrierfehler `mSPZ`/`mZPZ` sind im Rechenkern vorhanden, aber auf 0 gesetzt.

**Festlegung:**

- Neue Gruppe „Gewichtung" im Dialog „Rechenparameter": Schalter **„Gewichtung bei nahen Zielen"** (Standard **an**) mit den
  Feldern „Zielzentrierung [mm]" (Standard 2,0) und „Standpunktzentrierung [mm]" (Standard 0,0; nur bei bekanntem Standpunkt wirksam).
- Aus (`c = 0`) ergibt exakt das bisherige Verhalten; an: Richtungsvarianz `σ² = (mR·s)² + mSPZ² + mZPZ²`, Gewicht `p = m0²/σ²`
  (Zentrierfehler-Modell). Wirksame Richtungsunsicherheit der Orientierung: `σ_o² = mR² + (c/s)²`.
- Wirkt bei ≥ 3 Anschlusszielen in der Ausgleichung (inkl. s0). Bei 2 Zielen und bei einem Ziel ändert sich nichts am Ergebnis.
- **Wirkungsbereich (entschieden): nur im Polarmodul.** `FreieStationierungRechner.Berechnen()` bekommt dafür einen optionalen
  Parameter für die Zentrierfehler (Standard 0 = bisheriges Verhalten). Nur `FormPolarpunkte` übergibt die Werte aus den Rechenparametern.
  Freie Stationierung, DXF-Viewer, Auto-Match und die gespeicherten Stationsdaten bleiben **unverändert**.
- Anzeige in der Maske: nur die Spalte **„Gewicht"** (relatives Richtungsgewicht in % des größten) und die Angabe der Einstellung
  in der Ergebniszeile. Die Ampelgrenze für `v Winkel [cc]` bleibt fest. Es gibt keinen Nahziel-Hinweis und keine effektive Redundanz.
  Bekannte Folge: ein korrekt abgewichtetes nahes Ziel kann in der cc-Ampel rot erscheinen, obwohl `v Quer [mm]` grün ist; die
  Spalte „Gewicht" macht den Grund sichtbar (siehe `Gewichtungsanalyse.md`, Nachteil 5).
- **Höhe:** bekommt kein Gewicht; die Höhenberechnung bleibt wie bisher (auch später keine Höhengewichtung).
- Speicherung in der Rechenparameter-Datei: `GewichtungNaheZiele`, `ZentrierfehlerZiel_mm`, `ZentrierfehlerStandpunkt_mm`
  (fehlend = Standardwerte). Protokoll und Maske nennen die Einstellung.
- **Folgerung für die Datenhaltung:** Die Polarmaske schreibt ihre gewichtete Orientierung **nicht** in die Stationsdaten
  (`Station-{SP}.json`, Projektdaten) zurück. Die Koordinaten in `Neupunkte.json` werden dagegen mit der Polar-Orientierung
  berechnet. Damit sie nicht beim nächsten Stationierungsergebnis im DXF-Viewer (`NeupunkteManager.BerechneNeu`) mit der
  ungewichteten Orientierung überschrieben werden, erhalten sie die Markierung `Quelle = Polar` und werden von `BerechneNeu`
  übersprungen (Festlegung, bitte bei Bedarf widersprechen).

---

## 8  Maske `FormPolarpunkte`

Eine Maske mit zwei Tabellen untereinander (analog zu `FormFreieStationierung`/`FormErgebnis`).

```
┌ Polarpunkte ───────────────────────────────────────────────────────────────┐
│ Standpunkt [SP01 ▼]   IH [1.500]   Quelle: (•) Station gespeichert         │
│                                              ( ) Neu aus dat.csv           │
│ [Rechenparameter…]  [Neu laden]                                            │
│ ⚠ Berechnung mit Maßstab m = 0.999600 (vorgegeben)          (nur wenn m≠1) │
│ ⚠ Nur 2 Anschlusspunkte – Qualität nicht beurteilbar        (nur wenn <3)  │
│ ⚠ Punkt 7: Exzentrizität > 2 m                              (nur wenn >2 m)│
├ Orientierung / Anschlusspunkte ────────────────────────────────────────────┤
│ Punkt  Str.h  v Winkel  v Quer  [Hz]  v Längs  [Str]  v Höhe  [Hoe] Gewicht│
│ 1001   111.8     +3.2    +0.5    ☑     −1.1     ☑     +2.0    ☑     100 %  │
│ 1003     4.8    +90.0    +0.7    ☑      …                         1,2 %   │
│ R 1234.567  H 2345.678  Höhe 452.345  o 12.3456 gon  m 1.000000  s0 2.1 mm │
│ r = 3   Gewichtung nahe Ziele: ein (Zielzentrierung 2,0 mm)                │
├ Polarpunkte ───────────────────────────────────────────────────────────────┤
│ ☑ Punkt  Hz  V  S  ZH  Exz längs  Exz quer  │  R  H  Höhe  Code  Zeit      │
│ ☑ 1      306.0750  24.9140  103.438  1.600   0.000   0.000 │ …            │
├────────────────────────────────────────────────────────────────────────────┤
│ Status: 2D/3D · m · n AP   [Neu berechnen] [Speichern] [Als KOR-CSV speichern] [Protokoll] [Schließen] │
└────────────────────────────────────────────────────────────────────────────┘
```

### 8.1  Obere Tabelle (Orientierung) – Funktion wie `FormErgebnis`

- Spalten, Reihenfolge und Checkboxen **identisch** zur Freien Stationierung:
  `Punkt | Str.h | v Winkel [cc] | v Quer [mm] | Hz☑ | v Längs [mm] | Str☑ | v Höhe [mm] | Hoe☑`.
- Jede Checkbox schaltet die Beobachtung an/ab, löst sofort die Neuberechnung aus (`OnAktivierungGeaendert`).
- **Ampel**: wie `FormErgebnis.AmpelFarbe`, Grenzen aus `RechenparameterManager.Params`
  (`FehlergrenzCC_Winkel`, `FehlergrenzeMM_Strecke`, `FehlergrenzeMM_Hoehe`); grün `|v| ≤ Grenze − 1`,
  gelb `≤ Grenze + 5`, rot darüber; deaktivierte Werte grau mit „–".
- Höhen-Checkbox automatisch aus, wenn der Anschlusspunkt keine Höhe hat (wie bisher) oder
  `Berechnung3D = false`.
- Zusätzliche Spalte **„Gewicht"** (relatives Richtungsgewicht, Kap. 7.5); Ampelgrenzen bleiben fest, kein Nahziel-Hinweis,
  keine effektive Redundanz (Details: `Gewichtungsanalyse.md`).
- Ergebniszeile mit R, H, Höhe, **Orientierung**, **Maßstab**, s0 (mit Sigma-Faktor aus den Rechenparametern), Redundanz.
- Die Logik wird **nicht kopiert**: Ampelfärbung ist bereits `public static` (`FormErgebnis.IstGruen`) und wird
  wiederverwendet; die Tabellenerzeugung wird beim Codieren in ein gemeinsames Steuerelement (`ResiduenGrid`)
  ausgelagert, falls das ohne Risiko für `FormErgebnis` möglich ist, sonst bewusst als Kopie mit Verweis.

### 8.2  Untere Tabelle (Polarpunkte)

- Eine Zeile je `NP`-Messung des Standpunkts, mit Checkbox „berechnen" (Messung ausblenden, ohne sie zu löschen).
- **Editierbar**: `Exz längs`, `Exz quer` (Dezimalkomma/-punkt akzeptiert, Eingabe in m).
- Nur lesend: Punkt, Hz, V, S, ZH, Code, Zeit sowie berechnete R, H, Höhe.
- Zeilen, die nicht berechenbar sind (Strecke ≤ 0, keine Orientierung), grau mit Grund im Tooltip.
- Exzentrizitätszellen mit Betrag > 2 m werden gelb hinterlegt (Kap. 6.6).

### 8.3  Speichern (neue Regel)

- **Möglich**, sobald eine Orientierung berechenbar ist (Kap. 7.3) – also auch mit einem Anschlusspunkt und im
  lokalen System. Es gibt keine Sperre mehr durch die Ampel-Regel „≥ 3 grüne Quer- und Längswerte".
- **Informationen** (nicht blockierend, vor dem Schreiben als Meldung mit OK, dauerhaft in der Maske):
  1. **weniger als drei Anschlusspunkte** (Kap. 7.3),
  2. **Maßstab ≠ 1** (Kap. 6.5),
  3. nicht alle Residuen grün (Anzahl gelb/rot) – Qualitätshinweis statt Sperre,
  4. Exzentrizität > 2 m (Kap. 6.6),
- Speichern schreibt:
  1. Koordinaten nach `Neupunkte.json` (`NeupunkteManager.HinzufuegenOderErsetzen`, damit der DXF-Viewer sie zeigt),
  2. geänderte Exzentrizitäten zurück in die `dat.csv`,
  3. ein Protokoll (Kap. 9) inkl. aller Informationen aus Punkt 1–4, sofern „Protokoll" im Hauptmenü aktiv ist.

### 8.4  „Als KOR-CSV speichern"

Eigener Button neben „Speichern", unabhängig davon (nur aktiv, wenn Koordinaten berechnet sind).

- Datei `{Projekt}-kor.csv` im Projektverzeichnis (Dateidialog mit diesem Vorschlag, Überschreiben/Anhängen wird abgefragt),
  Format exakt nach `Muster-kor.csv`:
  ```
  # METADATA
  Projekt: <Projektname>
  Sensor: <Sensor>
  Bearbeiter: 
  Datum: yyyy-MM-dd
  ---
  # DATENTYP
  Koordinaten
  # DATEN
  PunktNr; X; Y; Z; Code
  7; 803213.063; 5400842.754; 452.345; 030
  ```
  `X` = Rechtswert R, `Y` = Hochwert H, `Z` = Höhe, Trennzeichen `"; "`, Dezimalpunkt, `F3`.
- Bei 2D-Berechnung (Höhe nicht berechnet) bleibt `Z` leer.
- Es werden die in der unteren Tabelle angehakten, berechenbaren Polarpunkte geschrieben. Existiert die Datei bereits, werden Punkte
  gleicher Nummer ersetzt, andere bleiben erhalten (Rückfrage vor dem Schreiben).
- Hinweise (Maßstab ≠ 1, < 3 Anschlusspunkte, Exzentrizität > 2 m) werden im Protokoll vermerkt, nicht in die Datei geschrieben.
- Die Datei darf nicht mit einer bereits geöffneten Import-Datei kollidieren (Importpunkte werden nach dem Schreiben neu geladen).

> Hinweis: Die Freie Stationierung selbst (`FormFreieStationierung`, „Speichern erst nach gültiger Berechnung",
> v1.9.7) bleibt **unverändert**. Die gelockerte Regel gilt nur für das Polarmodul.

---

## 9  Technische Umsetzung

### 9.1  Neue Dateien

| Datei | Inhalt |
|---|---|
| `Feldbuch/PolarpunktRechner.cs` | reine Rechenlogik (Formeln Kap. 6, Orientierung aus 1 Anschlusspunkt), keine UI, **ohne** Abhängigkeit zu Managern → testbar |
| `Feldbuch/PolarMessdatenLeser.cs` | liest die `dat.csv` (alt und neu), liefert `PolarMessung`-Liste inkl. `Typ`, Standpunkt, Exzentrizität |
| `Feldbuch/FormPolarpunkte.cs` + `.Designer.cs` | Maske Kap. 8 |
| `Feldbuch/PolarpunkteProtokoll.cs` + `Polarpunkte_Protokoll.xml` | Protokoll analog zu den anderen Berechnungen (Eintrag als `Content` in der `.csproj`) |
| `Feldbuch/KorCsvSchreiber.cs` | schreibt/ergänzt `{Projekt}-kor.csv` im Format von `Muster-kor.csv` (Kap. 8.4) |
| `Feldbuch.Tests/PolarpunktRechnerTests.cs`, `…LeserTests.cs` | Tests (Kap. 11) |

### 9.2  Geänderte Dateien

| Datei | Änderung |
|---|---|
| `Form1.Designer.cs`, `Form1.cs` | Buttons Kap. 2, Click-Handler |
| `MessdatenCSV.cs` | neue Spalten schreiben, Migration alter Dateien, Lese-Hilfsfunktion |
| `CsvDatenDatei.cs`, `Muster-dat.csv` | Standardspalten und Beispiel |
| `NeupunktRechner.cs` | `NeupunktRohdaten`/`NeupunktErgebnis` + Exzentrizität + Maßstab; `Berechnen()` ruft `PolarpunktRechner` |
| `RechenparameterManager.cs` | neue Felder `PolarMassstabModus`, `PolarMassstabWert`, `PolarMassstabQuelle`, `GewichtungNaheZiele`, `ZentrierfehlerZiel_mm`, `ZentrierfehlerStandpunkt_mm` (Laden/Speichern/Standardwerte) |
| `FormRechenparameter.cs` + `.Designer.cs` | Gruppen „Maßstab (Polarpunkte)" und „Gewichtung", Dialog höher |
| `FreieStationierung.cs` | optionaler Parameter für Zentrierfehler (Standard 0 = unverändert), relatives Richtungsgewicht je Punkt im Ergebnis; **nur** `FormPolarpunkte` übergibt Werte, alle bisherigen Aufrufer bleiben unverändert |
| `NeupunkteManager.cs` | `BerechneNeu` überspringt Punkte mit `Quelle = Polar` (Kap. 7.5) |
| `FormDxfViewer.cs` + `.Designer.cs` | Felder `eL`/`eQ`, Layout der Eingabeleiste, Reset nach Messung, Maßstabs-Hinweis in der Statuszeile |
| `StationsdatenManager.cs` | ggf. Hilfsmethode zum Laden einer Station ohne den aktiven Stand zu überschreiben |
| `README.md`, `Feldbuch.csproj` | Beschreibung, Version (Patch +0.0.1, **v1.9.8**) |

### 9.3  Konventionen

Wie im restlichen Projekt: deutsche Kommentare, englische Bezeichner, Dateikopf mit Zweck/Hardware/Autor,
keine Magic Numbers (z. B. Grenzen der Ampel, `GON2RAD`, Maßstabs-Toleranz und -Plausibilitätsgrenzen,
Mindestzahl Anschlusspunkte = 3, Spaltenanzahl als benannte Konstanten).

---

## 10  Entscheidungsstand und offene Fragen

### 10.1  Entschieden

| Nr | Thema | Entscheidung |
|---|---|---|
| 3 | Vorzeichen `eL`/`eQ` | wie Kap. 6.3 |
| 5 | Speichern bei wenig Anschlusspunkten | möglich, Information bei < 3 Anschlusspunkten (Kap. 7.3/8.3) |
| 6 | Maßstab | drei Varianten im Rechenparameter, Standard 1, Hinweis bei ≠ 1 (Kap. 6.5) |
| – | Ein Anschlussziel | Polarberechnung möglich, wenn der Standpunkt bekannt ist (Kap. 7.3) |
| – | Ampel-Sperre beim Speichern | entfällt, nur Information (Kap. 8.3) |
| – | Maßstab auf Exzentrizitäten | nein (Kap. 6.2) |
| – | Exzentrizität > 2 m | Information (Kap. 6.6) |
| – | KOR-CSV | zusätzlicher Button (Kap. 8.4) |
| – | „Modus Berechnet" ohne Maßstab | Rückfall auf m = 1 mit Hinweis |
| – | Ausgabe | `Neupunkte.json` und KOR-CSV per Button |
| – | Gewichtung bei nahen Zielen | als Rechenparameter, Standard aktiv, Gewichtsberechnung bei der Orientierung (Kap. 7.5) |
| – | Wirkungsbereich Gewichtung | nur Polarmodul; Freie Stationierung/DXF-Viewer unverändert |
| – | Standardwerte Zentrierung | Ziel 2,0 mm, Standpunkt 0,0 mm |
| – | Ampel Winkel / Zusatzanzeigen | feste Grenze; nur Spalte „Gewicht" |
| – | Höhe | kein Gewicht, auch später nicht |
| – | KOR-CSV bei vorhandener Datei | gleiche Punktnummern ersetzen, andere erhalten |

### 10.2  Gelten als vereinbart, solange du nichts anderes sagst (Empfehlung aus Entwurf 1)

| Nr | Thema | Festlegung |
|---|---|---|
| 1 | Eingabeort Exzentrizität | Viewer-Leiste (A) plus Korrektur in der Maske (B) |
| 2 | Zusatzspalten `StandpunktNr`, `Typ` in der `dat.csv` | ja |
| 4 | Exzentrizität bei Anschlusspunkten | nur für Polarpunkte, bei Anschlusspunkten gesperrt/ignoriert |
| 8 | Höhenexzentrizität | keine eigene Spalte (durch Zielhöhe abgedeckt) |
| 9 | Einzelmessungen ausschließen | Checkbox „berechnen" in der unteren Tabelle |
| 10 | Mehrfachmessungen gleicher PunktNr | getrennte Zeilen, kein automatischer Mittelwert |
| 11 | Farbe „Polarpunkte" | grau |

### 10.3  Offen

Keine inhaltlichen Fragen mehr. Zur Prüfung vor der Freigabe nur die in Kap. 7.5 genannte Festlegung
(`Quelle = Polar`: Polar-Punkte werden von `BerechneNeu` im DXF-Viewer nicht überschrieben).

---

## 11  Test- und Abnahmeplan

**Automatische Tests (xunit, `Feldbuch.Tests`):**

- `PolarpunktRechner`: Beispiele aus Kap. 6.3 (eL=+1, eQ=+1, kombiniert, mit `o` ≠ 0, Hz-Überlauf > 400 gon).
- Maßstab: `m = 1` identisch zum bisherigen Ergebnis (Regression); `m = 0,9996` skaliert nur die gemessene Horizontalstrecke,
  **nicht** Exzentrizitäten und Höhe; Modus „berechnet" reproduziert die Streckenresiduen der Stationierung; Rückfall auf 1.
- Exzentrizität: Information bei `|eL|` oder `|eQ|` > 2 m (Grenzfall genau 2,000 m: keine Information).
- Gewichtung: Schalter **aus** → Ergebnisse identisch zu v1.9.7; **an** → Beispiel aus `Gewichtungsanalyse.md` Kap. 3
  (2 × 100 m + 1 × 5 m mit 3 mm Lotfehler: Orientierungsfehler mit Gewichtung höchstens 60 % des Fehlers ohne Gewichtung); Gewicht fällt monoton mit kleiner Strecke;
  n = 2 und 1 Anschlussziel unverändert; alte Rechenparameter-Datei ohne neue Elemente → Standardwerte.
  **Regression:** `FreieStationierungRechner` ohne den neuen Parameter liefert exakt die bisherigen Ergebnisse (auch bei aktivem
  Schalter in den Rechenparametern); Höhenergebnis unverändert.
- `NeupunkteManager.BerechneNeu` lässt Punkte mit `Quelle = Polar` unverändert.
- KOR-CSV: Format identisch zu `Muster-kor.csv` (Header, Trennzeichen `"; "`, `F3`), Ersetzen gleicher Punktnummern,
  2D ohne Höhe, wieder einlesbar mit `ImportPunkteManager.LeseCsv`.
- Höhe: 2D bei `Berechnung3D = false`; 3D mit `IH`/`ZH`; lokales System.
- Orientierung aus 1 Anschlusspunkt (Kap. 7.3), aus 2 und aus ≥ 3 Punkten; Anzahl-Information.
- Rechenparameter: alte XML ohne neue Elemente → `m = 1`; Speichern/Laden der drei Modi.
- Leser: alte 7-Spalten-Datei (Exz = 0, Heuristik für `Typ`), neue 11-Spalten-Datei, Kommentarzeilen `# STATIONIERUNG`.
- Migration: alte Datei → Sicherungskopie + aufgerüstete Spaltenzeile + aufgefüllte Datenzeilen.
- `NeupunktRechner` unverändert für Exz = 0 und `m = 1` (Regressionsschutz).

**Manuelle Prüfung (am Gerät/Testdaten):**

1. `Muster-dat.csv` laden → Maske zeigt Ampel wie die Freie Stationierung.
2. Einzelne Beobachtung abschalten → Orientierung/Ampel ändern sich sofort.
3. Exzentrizität eintragen → Koordinaten ändern sich entsprechend Kap. 6.3.
4. Maßstab auf „vorgegeben 0.9996" stellen → gelber Hinweis erscheint, Koordinaten ändern sich, Hinweis im Protokoll.
   Exzentrizität 2,5 m eintragen → Information erscheint (Viewer, Maske, Protokoll).
   Nahes Ziel (< 10 m) bei aktiver Gewichtung → Gewicht in der Spalte klein, Orientierung ändert sich beim Abschalten kaum.
   „Als KOR-CSV speichern" → Datei lässt sich im DXF-Viewer wieder als Punktdatei laden.
5. Mit 2 bzw. 1 Anschlusspunkt speichern → Information erscheint, Speichern funktioniert.
6. Speichern → Punkte im DXF-Viewer sichtbar, `dat.csv` enthält die Exzentrizitäten.
7. Lokales System ohne DXF: Messung mit eL/eQ → Koordinaten stimmen mit Handrechnung.

---

## 12  Umsetzungsreihenfolge (nach Freigabe)

1. `PolarpunktRechner` (inkl. Maßstab, Orientierung aus 1 Punkt) + Tests.
2. `RechenparameterManager`/`FormRechenparameter`: Maßstabsgruppe und Gruppe „Gewichtung" + Tests.
2a. `FreieStationierungRechner`: optionaler Zentrierfehler-Parameter, Gewicht je Punkt + Tests (ohne Parameter = bisheriges Ergebnis).
3. `MessdatenCSV`/`CsvDatenDatei`: neue Spalten, Leser, Migration + Tests.
4. `NeupunktRohdaten`/`NeupunktRechner` anbinden (Regressionstests grün).
5. DXF-Viewer: Felder `eL`/`eQ`, Maßstabs-Hinweis.
6. `FormPolarpunkte` (obere Tabelle mit Gewicht/Ampel → untere Tabelle → Hinweise → Speichern → KOR-CSV → Protokoll).
7. Hauptmenü (Buttons umordnen).
8. README, Version 1.9.8, Build, Tests, Commit/Push erst auf deine Anweisung.
