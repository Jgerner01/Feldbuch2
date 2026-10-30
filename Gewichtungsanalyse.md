# Gewichtung bei nahen Zielen – Analyse

Stand: 2026-10-30 | Status: **entschieden, nichts codiert** | gehört zu `Polarpunktberechnungsplan.md` (Kap. 7.5)

**Entscheidungen (Entwurf 4):** Die Gewichtung wirkt **nur im Polarmodul** (Freie Stationierung und DXF-Viewer unverändert). Standardwerte:
Zielzentrierung 2,0 mm, Standpunktzentrierung 0,0 mm. Die Ampelgrenze für `v Winkel [cc]` bleibt **fest**. Als Zusatzanzeige gibt es **nur** die
Spalte „Gewicht". Die **Höhe bekommt kein Gewicht**, auch später nicht. Die Kapitel 6.2, 6.3 und 7 sind entsprechend angepasst.

---

## 1  Problem

Liegt ein Anschlussziel sehr nahe am Standpunkt, wirkt sich schon ein kleiner **Lagefehler am Ziel** (Lotstab nicht exakt über dem Punkt,
Reflektor schief, Zentrierung am Dreifuß) als **großer Winkelfehler** aus:

```
Winkelfehler [rad] ≈ Lagefehler / Strecke        (1 rad = 636 620 cc)
```

Beispiel mit 2 mm Lagefehler:

| Strecke | Winkelfehler |
|---|---|
| 200 m | 6,4 cc |
| 100 m | 12,7 cc |
| 50 m | 25,5 cc |
| 20 m | 63,7 cc |
| 10 m | 127 cc |
| 5 m | 255 cc |
| 2 m | 637 cc |

Der Winkel-Messfehler des Tachymeters selbst (Standard im Programm: `mR = 9 cc`) tritt dagegen bei jeder Strecke gleich auf.
Ein nahes Ziel liefert also eine **schlechte Richtung** (aber eine genauso gute Strecke). Fließt es mit vollem Gewicht in die Orientierung ein,
verfälscht es den Richtungswinkel – und damit **alle** polar berechneten Punkte, vor allem die weit entfernten.

---

## 2  Was macht das Programm heute? (Befund im Code)

`FreieStationierungRechner.VollAusgleichung()` (nur bei ≥ 3 Punkten; `FreieStationierung.cs`, Zeilen ca. 390–415):

- Die Richtungsbeobachtung wird als **Querabweichung in Metern** angesetzt:
  Koeffizient der Orientierung `A = s`, Gewicht `p = m0² / (mR·s)²`.
- Der Beitrag zur Normalgleichung der Orientierung ist damit `A² · p = s² · m0² / (mR · s)² = m0² / mR²`.

**Ergebnis: Die Strecke kürzt sich heraus. Jede Richtung geht mit demselben Gewicht in die Orientierung ein – ein Ziel in 2 m Entfernung
genauso stark wie eines in 200 m.**

Zentrierfehler sind im Rechenmodell zwar vorgesehen (`InstrumentParameter.mSPZ_mm`, `mZPZ_mm`, gehen quadratisch in die Varianz ein),
stehen aber auf **0** und sind im Dialog „Rechenparameter" nicht einstellbar. Deshalb wirkt heute keine Nahzielgewichtung.

Weitere Stellen:

| Stelle | Verhalten heute | Folge |
|---|---|---|
| Helmert-Näherungslösung | Rechnung in Koordinaten (`ξ = dh·cos Hz`, `η = dh·sin Hz`) | gibt weit entfernten Punkten von Natur aus mehr Einfluss; dient nur als Startwert |
| 2 Punkte (`n = 2`) | Direktlösung ohne Redundanz | Gewichtung wirkungslos (r = 0) |
| Höhenmittel | Gewicht `1 / dh²` | nahe Ziele bekommen sehr hohes Höhengewicht (Zielhöhen-/Lotfehler nicht berücksichtigt) |
| Ampel `v Winkel [cc]` | feste Grenze (Standard 20 cc) | bei nahen Zielen sind große cc-Werte normal → Ampel meldet „rot", obwohl `v Quer` in mm „grün" ist |

---

## 3  Vorschlag: Zentrierfehler-Modell

Statt einer willkürlichen Gewichtsfunktion wird der **tatsächliche Fehleranteil** modelliert (so rechnen die üblichen Stationierungsprogramme):

```
c²      = mSPZ² + mZPZ²                              Zentrierfehler Standpunkt / Ziel [mm]
σ_Rich² = (mR · s)² + c²        (in Metern, Querrichtung)
σ_Str²  = (msk + msv · s)² + c²                       (Strecke)
p       = m0² / σ²                                    Gewicht
```

Das ist **die bereits vorhandene Formel**; neu ist nur, dass `c` einen sinnvollen Wert bekommt. Für die Orientierung ergibt sich als wirksame
Richtungsunsicherheit

```
σ_o(s)² = mR² + (c / s)²
```

Beispiel mit `mR = 9 cc`, `c = 2 mm` (Zielzentrierung 2 mm, freie Stationierung ohne Standpunktzentrierung):

| Strecke | σ_o [cc] | relatives Gewicht (100 m = 1,00) | heute |
|---|---|---|---|
| 200 m | 11,0 | 2,01 | 1,00 |
| 100 m | 15,6 | 1,00 | 1,00 |
| 50 m | 27,0 | 0,33 | 1,00 |
| 20 m | 64,3 | 0,059 | 1,00 |
| 10 m | 127,6 | 0,015 | 1,00 |
| 5 m | 254,8 | 0,0037 | 1,00 |
| 2 m | 636,7 | 0,0006 | 1,00 |

**Wirkung (gemessen mit der umgesetzten Rechnung, nicht Näherung):** Ein nahes Ziel (5 m) mit 3 mm Lotfehler (= 382 cc Richtungsfehler),
dazu ferne Ziele in 100 m; Fehler gegenüber den wahren Werten:

| Geometrie | Orientierung ohne / mit Gewichtung | Standpunktlage ohne / mit Gewichtung |
|---|---|---|
| 3 ferne Ziele + 1 nahes | −1,1 cc / −0,5 cc | 3,0 mm / 1,3 mm |
| 2 ferne Ziele + 1 nahes (ungünstig) | −11,1 cc / −4,9 cc | 3,5 mm / 1,8 mm |

Die Gewichtung **halbiert** den Einfluss des nahen Ziels etwa; sie beseitigt ihn nicht. Der Grund: Die ursprünglich angenommene Wirkung
(≈ 96 cc ohne, < 1 cc mit Gewichtung) ergab sich aus einer Näherung **ohne** Kopplung mit den Standpunktkoordinaten und war zu groß. In der
Ausgleichung weicht ein großer Teil des Lotfehlers auf die Standpunktlage aus (Verschiebung quer zur Zielung); außerdem behält das
nahe Ziel auch mit Gewichtung ein Lagegewicht, weil sein Querfehler in Metern (σ ≈ 2 mm) mit dem der fernen Ziele vergleichbar ist.
Die Verbesserung ist am deutlichsten bei ungünstiger Geometrie mit wenigen fernen Zielen.

---

## 4  Vor- und Nachteile einer Gewichtung

### Vorteile

1. **Richtigere Orientierung.** Nahe Ziele verfälschen den Richtungswinkel nicht mehr; die Polarpunkte in größerer Entfernung werden genauer.
2. **Physikalisch begründet.** Die Gewichte folgen dem tatsächlichen Fehlerbudget (Messfehler + Zentrierfehler), nicht einer Faustregel.
3. **Weniger Fehlalarm in der Ampel** (bei angepasster Winkelgrenze, Kap. 5): Kleine Lotfehler an nahen Zielen erscheinen nicht mehr als grobe Winkelfehler.
4. **Realistischeres s0.** Die Standardabweichung der Gewichtseinheit bildet die tatsächliche Genauigkeit besser ab.
5. **Kaum Zusatzaufwand im Rechenkern.** Die Formeln existieren; es sind nur Parameter und ein Schalter nötig. „Aus" ergibt exakt das heutige Verhalten.
6. **Üblich.** Stochastische Modelle mit Zentrierfehlern sind in professioneller Stationierungssoftware Standard.

### Nachteile / Risiken

1. **Ergebnisse weichen von der ungewichteten Orientierung ab** (Orientierung, s0, Residuen), weil der Schalter standardmäßig **an** ist.
   Da die Gewichtung nur im Polarmodul wirkt, bleiben bisherige Stationierungen und Protokolle unverändert; Polar-Ergebnisse können aber von
   der Orientierung im DXF-Viewer abweichen → Protokolle müssen die Einstellung nennen.
2. **Das Ergebnis hängt von den Annahmen ab.** Ein zu großer Zentrierfehler entwertet zu viele Ziele, ein zu kleiner wirkt nicht. Die Werte müssen
   dokumentiert und einstellbar sein.
3. **Weniger effektive Redundanz.** Ein stark abgewichtetes Ziel zählt formal als Beobachtung, trägt aber fast nichts bei. Eine Stationierung mit
   3 Punkten, von denen einer sehr nah liegt, ist faktisch eine Stationierung mit 2 Punkten (r = 0). Die Redundanzzahl allein täuscht dann Sicherheit vor.
4. **Wirkt nur bei ≥ 3 Punkten.** Bei 2 Punkten (r = 0) und bei einem Anschlussziel ändert sich nichts; dort bleibt ein nahes Ziel ein Risiko
   (nur ein Hinweis möglich).
5. **Ampel muss mitwandern.** Mit festen Grenzen (20 cc / 10 mm) widerspricht die Gewichtung der Ampel: Ein korrekt abgewichtetes nahes Ziel zeigt
   große cc-Residuen und wäre „rot". Ohne angepasste Grenze wird der Nutzer verleitet, genau diese Ziele abzuschalten oder ihnen zu misstrauen.
6. **Streckengewicht ändert sich mit.** Der Zentrierfehler geht (physikalisch richtig) auch in die Strecken-Varianz ein. Dadurch sinken alle
   Streckengewichte leicht (z. B. bei 100 m: 3,2 → 3,8 mm mit 2 mm Zielzentrierung) und das Verhältnis Richtung/Strecke verschiebt sich, was s0 beeinflusst.
7. **Mehr Parameter** im Dialog („Rechenparameter" wird voller) und mehr Testaufwand.

---

## 5  Alternativen im Vergleich

| Variante | Prinzip | Vorteile | Nachteile | Bewertung |
|---|---|---|---|---|
| **A  keine Gewichtung** (heute) | alle Richtungen gleichgewichtet | einfach, reproduzierbar | nahe Ziele verfälschen die Orientierung | ungeeignet für nahe Ziele |
| **B  Zentrierfehler-Modell** | `σ² = (mR·s)² + c²` | begründet, nutzt vorhandenen Code, stetig, ein Schalter | abhängig von `c`, ändert Ergebnisse | **empfohlen** |
| C  harte Mindestentfernung | Richtungen unter x m werden abgeschaltet | verständlich, sehr einfach | Sprung bei der Grenze, willkürlich, Daten gehen verloren | nur als ergänzender **Hinweis** |
| D  empirische Funktion | z. B. `p = s² / (s² + s_ref²)` | einfach, stetig | keine physikalische Begründung, `s_ref` schwer zu wählen | unnötig, wenn B vorhanden |
| E  robuste Schätzung (Huber/Danish) | Gewichte werden aus den Residuen iterativ gesenkt | erkennt Ausreißer aller Art | Ergebnis schwer nachvollziehbar, instabil bei wenigen Punkten (n < 6) | für kleine Netze ungeeignet; später evtl. |
| F  Ausreißertest (Data-Snooping) | normierte Verbesserungen prüfen | statistisch sauber | braucht Redundanz, komplex | später evtl. |

**Empfehlung: B** als Standard, **C** nur als Information („Ziel näher als x m"), E/F nicht jetzt.

---

## 6  Umsetzungsvorschlag

### 6.1  Rechenparameter („Gewichtung bei nahen Zielen")

Neue Gruppe im Dialog „Rechenparameter":

```
┌ Gewichtung ──────────────────────────────────────────────────┐
│ [x] Gewichtung bei nahen Zielen          (Standard: aktiv)   │
│     Zielzentrierung      [ 2.0 ] mm                          │
│     Standpunktzentrierung [ 0.0 ] mm  (nur bei bekanntem     │
│                                        Standpunkt wirksam)   │
└──────────────────────────────────────────────────────────────┘
```

| Parameter | XML-Element | Standard | Bedeutung |
|---|---|---|---|
| Schalter | `GewichtungNaheZiele` | `true` | an: `c` wird verwendet; aus: `c = 0` (= Verhalten wie heute) |
| Zielzentrierung | `ZentrierfehlerZiel_mm` | `2.0` | Lot-/Zentrierfehler am Ziel (`mZPZ`) |
| Standpunktzentrierung | `ZentrierfehlerStandpunkt_mm` | `0.0` | `mSPZ`; bei **freier** Stationierung bedeutungslos, bei bekanntem Standpunkt (1 Anschlussziel) relevant |

Alte Rechenparameter-Dateien ohne diese Elemente laufen mit den Standardwerten (= Gewichtung **aktiv**, siehe Nachteil 1).
Die bestehenden Instrumentenwerte (`mR = 9 cc`, `msk = 3 mm`, `msv = 2 ppm`) bleiben unverändert im Code.

### 6.2  Wirkungsbereich

| Bereich | Wirkung |
|---|---|
| `FreieStationierungRechner.Berechnen()` (n ≥ 3): Ausgleichung und s0 | **nur auf Anforderung des Polarmoduls**: optionaler Parameter für `mSPZ`/`mZPZ`, Standard 0 = bisheriges Verhalten |
| n = 2 Direktlösung, 1 Anschlussziel | keine Wirkung (r = 0 bzw. keine Ausgleichung) |
| Polarmodul (Orientierung aus Station oder `dat.csv`) | **ja** – übergibt die Zentrierfehler aus den Rechenparametern |
| DXF-Viewer (Live-Stationierung), Freie Stationierung, Auto-Match, gespeicherte Stationsdaten | **nein, unverändert** |
| Höhenmittel | **unverändert, kein Gewicht** (auch später nicht) |

Das Polarmodul schreibt seine gewichtete Orientierung nicht in die Stationsdaten zurück. Die damit berechneten Neupunkte werden mit
`Quelle = Polar` markiert, damit sie das Neuberechnen im DXF-Viewer nicht mit der ungewichteten Orientierung überschreibt.

### 6.3  Anzeige und Transparenz

1. Zusätzliche Spalte **„Gewicht"** in der Residuentabelle der Polarmaske (relatives Richtungsgewicht, % des größten).
2. Die Ergebniszeile nennt die Einstellung („Gewichtung nahe Ziele: ein, Zielzentrierung 2,0 mm"); ebenso das Protokoll.
3. Die Ampelgrenzen bleiben **fest** (20 cc / 10 mm). Es gibt keinen Nahziel-Hinweis und keine effektive Redundanz.
   Ein korrekt abgewichtetes nahes Ziel kann deshalb in der cc-Ampel rot erscheinen (Nachteil 5); die Spalte „Gewicht" erklärt den Grund.

### 6.4  Tests

- Ohne den neuen Parameter liefert `FreieStationierungRechner` exakt die Ergebnisse von v1.9.7 (Regression, `Muster-dat.csv`), auch wenn der Schalter
  in den Rechenparametern aktiv ist.
- Polarmodul, Schalter **aus** → Ergebnisse identisch zu v1.9.7.
- Polarmodul, Schalter **an**, alle Ziele weit entfernt → Orientierung praktisch unverändert.
- Konstruiertes Beispiel aus Kap. 3 (2 × 100 m + 1 × 5 m mit 3 mm Lotfehler): Orientierungsfehler mit Gewichtung höchstens 60 % des Fehlers ohne Gewichtung
  (umgesetzt in `PolarpunktRechnerTests`).
- Gewichte monoton in der Entfernung.
- n = 2 und n = 1: Ergebnis unverändert.
- Höhenergebnis mit und ohne Gewichtung identisch.
- `BerechneNeu` im DXF-Viewer lässt Punkte mit `Quelle = Polar` unverändert.

---

## 7  Entscheidungen (alle beantwortet)

| Nr | Frage | Entscheidung |
|---|---|---|
| 1 | Wirkungsbereich | nur Polarmodul (dort ist die Gefahr durch kurze Zielungen am größten) |
| 2 | Standardwerte | Zielzentrierung 2,0 mm, Standpunktzentrierung 0,0 mm |
| 3 | Ampel Winkel | feste Grenze |
| 4 | Zusatzanzeigen | nur die Spalte „Gewicht" |
| 5 | Höhengewicht | kein Gewicht, auch später nicht |
