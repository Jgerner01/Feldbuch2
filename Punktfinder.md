# Automatische Punkterkennung – Detailplan

Stand: 2026-04-24 | Status: Planungsdokument (alle offenen Fragen geklärt)

---

## 1  Ziel und Konzept

Die Punktsuche dient der **Teilautomatisierung der Erstellung von
`Anschlusspunkte.csv`** in der Freien Stationierung. Es gibt zwei Betriebsmodi:

### Handbetrieb
- Benutzer klickt in der DXF-Graphik auf einen Punkt.
- Gleichzeitig liegt vom Tachymeter eine aktuelle Messung vor (Hz, V, D).
- Diese Kombination (DXF-PunktNr + Messung) wird direkt in `Anschlusspunkte.csv` geschrieben.
- Keine Stationsberechnung nötig, kein Schwellenwert.

### Auto-Match
- Erfordert eine **brauchbare** vorläufige Stationsberechnung:
  - `grün` (< 3 mm s₀): Auto-Match verfügbar.
  - `gelb` (3–10 mm s₀): Auto-Match verfügbar.
  - `rot` (10–20 mm s₀): Auto-Match verfügbar.
  - `tiefrot` (> 20 mm s₀ oder Redundanz = 0): **Auto-Match gesperrt**.
- Sobald eine brauchbare Station vorhanden ist und der Modus aktiviert ist,
  wird jede neue Tachymetermessung automatisch in der DXF-Graphik gesucht.
- Treffer werden (je nach Konfidenz) automatisch oder nach Bestätigung in die CSV übernommen.

Beide Modi schreiben immer **vollständige Datensätze** (PunktNr, R, H, Höhe, Hz, V, D, Zielh.)
in `Anschlusspunkte.csv`. 2D-Only-Matching: die Höhe wird aus der Messung übernommen,
nicht aus dem DXF.

### Entschiedene Rahmenbedingungen

| Nr | Frage | Entscheidung |
|---|---|---|
| 1 | Auto-Übernahme? | Ja, wenn Abstand ≤ ½ × r_suche (nur E+N); sonst Bestätigung |
| 2 | Quellen | **Nur DXF-Graphik** – bereits gemessene Punkte werden ausgeschlossen |
| 3 | Qualitätsschwelle | `tiefrot` (s₀ > 20 mm oder kein Redundanz) → Auto-Match gesperrt |
| 4 | DXF-Suchkreis | Kreis + Kandidat sichtbar, wenn Bestätigung nötig |
| 5 | Distanz-Pflicht | Konfigurierbar; ohne Strecke immer Bestätigung |
| 6 | Protokoll | Eigenes Auto-Match-Protokoll (CSV, append-only) |
| 7 | PunktNr ohne DXF-Nummer | Aus DXF-Viewer-Zähler, inkrementiert |
| A | DXF-Viewer-Referenz | `static FormDxfViewer? AktiveInstanz` (gesetzt in `OnLoad`/`OnFormClosed`) |
| B | Maßstabskorrektur | `D_h_korr = D_h / massstab` wenn `freierMassstab = true` |
| C | „Bereits gemessen" | HashSet der PunktNr-Einträge im Grid (nur PunktNr-Vergleich) |

---

## 2  Algorithmus

### 2.1  Mit Streckenmessung (Vollmessung: Hz + V + D)

```
Eingabe:  E₀, N₀, θ₀ (vorläufige Station)   – aus StationierungsErgebnis
          Hz [gon], V [gon], D [m]           – neue TachymeterMessung
          n                                  – Anzahl bereits bekannter Punkte
          massstab                           – aus StationierungsErgebnis (1.0 wenn nicht frei)

1. Horizontalstrecke (mit Maßstabskorrektur wenn freierMassstab):
   D_h = D × sin(V × π / 200) / massstab

2. Gitterrichtung:
   α = (θ₀ + Hz) mod 400         [gon]

3. Vorhergesagte Position:
   E_pred = E₀ + D_h × sin(α × π / 200)
   N_pred = N₀ + D_h × cos(α × π / 200)

4. Suchradius:
   σ_θ_cc  = (n == 2) ? 15 : 5                     [cc]
   σ_θ_rad = σ_θ_cc × π / 2_000_000
   σ_stat  = max(s0_mm / 1000, 0.005)               [m]
   σ_quer  = D_h × σ_θ_rad                          [m]
   σ_ges   = sqrt(σ_stat² + σ_quer²)
   r_suche = clamp(k × σ_ges,  r_min,  r_max)
             k=3,  r_min=0.10 m,  r_max=2.00 m

5. DXF-Kandidaten (aus _eintraege, ausgenommen bereits gemessene):
   Treffer = { p ∈ DXF | sqrt((p.R−E_pred)² + (p.H−N_pred)²) ≤ r_suche }
   Sortiert nach Abstand aufsteigend.

6. Entscheidung:
   a) 0 Treffer       → "Kein Punkt gefunden" · Messung verwerfen oder manuell
   b) 1 Treffer, Abstand ≤ r_suche/2  → AUTO-MATCH (kein Dialog)
   c) 1 Treffer, Abstand > r_suche/2  → DXF öffnen, Kreis + Kandidat, Rückfrage
   d) ≥2 Treffer      → Auswahl-Dialog, alle Kandidaten in DXF markiert
```

### 2.2  Ohne Streckenmessung (nur Hz + V, wenn Distanz-Pflicht deaktiviert)

Ohne Strecke ist Auto-Match **immer verboten** (zu unsicher). Nur Bestätigung.

```
Eingabe:  E₀, N₀, θ₀   + Hz [gon]

1. Gitterrichtung:
   α = (θ₀ + Hz) mod 400         [gon]

2. Winkeltoleranz (konfigurierbar):
   δα = 15 cc = 0.015 gon  (≈ 5 mm Querabweichung bei 50 m)

3. Kandidaten:
   Für jeden DXF-Punkt p (nicht bereits gemessen):
     α_p = atan2(p.R − E₀, p.H − N₀) × 200/π     [gon]
     Δα  = |normiere(α − α_p)|                     [gon]
     Treffer wenn Δα ≤ δα
   Sortiert nach Distanz zu Station aufsteigend.

4. Entscheidung: IMMER Bestätigung, nie Auto-Match.
   a) 0 Treffer  → Warnung
   b) ≥1 Treffer → Auswahl-Dialog + DXF-Visualisierung (Richtungsstrahl statt Kreis)
```

### 2.3  PunktNr-Vergabe

1. DXF-Punkt hat eine erkennbare PunktNr → diese übernehmen.
2. DXF-Punkt hat keine PunktNr → letzten bekannten Zählerstand des DXF-Viewers
   (`FormDxfViewer.AktiveInstanz?.NaechstePunktNummer()`) holen und inkrementieren.
3. Kein DXF-Viewer aktiv → Sequenzielle Fallback-Nummer `"AP-{n}"`.

In allen Fällen fließt der Datensatz vollständig in `Anschlusspunkte.csv`.

---

## 3  Neue Klassen

### 3.1  `PunktFinder.cs`

```csharp
public record PunktFinderTreffer(
    string PunktNr,
    double R,
    double H,
    double Abstand_m,       // Abstand zu E_pred / N_pred
    bool   AutoMatch        // true wenn Abstand ≤ r_suche/2
);

public class PunktFinderKonfig
{
    public double MindestRadius_m     { get; set; } = 0.10;
    public double MaximalRadius_m     { get; set; } = 2.00;
    public double SicherheitsFaktor   { get; set; } = 3.0;
    public double SigmaTheta_2Pkt_cc  { get; set; } = 15.0;
    public double SigmaTheta_nPkt_cc  { get; set; } =  5.0;
    public double WinkelToleranz_cc   { get; set; } = 15.0;
    public bool   DistanzPflicht      { get; set; } = true;
}

public static class PunktFinder
{
    public static bool IstBrauchbar(double s0_mm, int redundanz)
        => redundanz > 0 && s0_mm <= 20.0;

    public static (double E, double N) BerechnePriorPosition(
        double stationE, double stationN, double orientierung_gon,
        double hz_gon,   double v_gon,   double schraegestrecke_m,
        double massstab = 1.0);

    public static double BerechneRadius(
        double s0_mm, int nPunkte, double horizontalstrecke_m,
        PunktFinderKonfig konfig);

    public static List<PunktFinderTreffer> SucheNachPosition(
        double e_pred, double n_pred, double r_suche,
        DxfPunktIndex dxfIndex,
        HashSet<string> bereitsGemessen);

    public static List<PunktFinderTreffer> SucheNachRichtung(
        double stationE, double stationN, double richtung_gon,
        DxfPunktIndex dxfIndex,
        HashSet<string> bereitsGemessen,
        PunktFinderKonfig konfig);
}
```

### 3.2  `TachymeterMessungsCache.cs`

```csharp
public static class TachymeterMessungsCache
{
    public static TachymeterMessung?         LetzteVollmessung { get; private set; }
    public static event Action<TachymeterMessung>? NeueVollmessung;

    public static void Initialisieren()   // einmal in Program.cs
    {
        TachymeterVerbindung.DatenEmpfangen += (_, roh) =>
        {
            var parser = TachymeterBefehlsgeberFactory
                             .ErzeugeParser(TachymeterVerbindung.Modell);
            foreach (var zeile in ZeilenAus(roh))
            {
                var m = parser.ParseZeile(zeile);
                if (m is { IstVollmessung: true })
                {
                    LetzteVollmessung = m;
                    NeueVollmessung?.Invoke(m);
                }
            }
        };
    }
}
```

### 3.3  `AutoMatchProtokoll.cs`

```csharp
public enum AutoMatchErgebnis { AutoMatch, Bestaetigt, Abgelehnt, KeinTreffer, MehrereTreffer }

public record AutoMatchEreignis(
    DateTime Zeitstempel,
    double StationE, double StationN, double StationH,
    double Orientierung_gon,
    double s0_mm, int nPunkte,
    double Hz_gon, double V_gon, double D_m,
    double E_pred, double N_pred, double Radius_m,
    int AnzahlTreffer,
    string GewaehlterPunkt,
    double AbstandGewählt_m,
    AutoMatchErgebnis Ergebnis
);

public static class AutoMatchProtokoll
{
    public static void Schreiben(AutoMatchEreignis e, string projektPfad);
    public static List<AutoMatchEreignis> Laden(string projektPfad);
    public static void ZeigeProtokoll(string projektPfad);
}
```

CSV-Format (`;`-getrennt, Header + je eine Zeile pro Ereignis):
```
Zeitstempel;StationE;StationN;StationH;θ₀_gon;s0_mm;nPkt;Hz_gon;V_gon;D_m;E_pred;N_pred;R_suche_m;nTreffer;GewähltPunkt;AbstandGew_m;Ergebnis
2026-04-24 10:23:11;5000.123;4000.456;345.78;127.3445;2.3;3;84.2100;99.8800;23.450;5018.234;4013.781;0.145;1;P301;0.032;AutoMatch
2026-04-24 10:24:03;5000.123;4000.456;345.78;127.3445;2.3;4;201.5600;100.1200;45.120;4962.001;4022.345;0.248;2;;-1;Abgelehnt
```

---

## 4  JSON-Datei für Auto-Match-Punkte – Konzept und Überlegungen

### Warum eine JSON-Datei?

Die `Anschlusspunkte.csv` ist die **einzige Wahrheitsquelle** für die
Stationierungsrechnung. Sie enthält manuell eingegebene, im Handbetrieb geklickte
und automatisch gematche Punkte – unterschiedslos. Das ist gut für die Berechnung,
aber schlecht für die Verwaltung:

**Probleme ohne JSON:**
- Man kann nicht sehen, welche CSV-Zeilen durch Auto-Match entstanden sind.
- Ein falscher Auto-Match kann nicht gezielt rückgängig gemacht werden.
- Nach einem Neustart weiß die Anwendung nicht mehr, welche Punkte bereits
  gematcht wurden (wichtig für `bereitsGemessen`-Filterung).

**Vorteile der JSON-Datei:**
1. **Rückgängig machen**: JSON enthält den Original-Match-Datensatz; löscht man
   ihn dort, kann man die zugehörige CSV-Zeile gezielt entfernen.
2. **Resume-Sicherheit**: Beim Öffnen einer bestehenden Stationierung werden die
   bereits gematchten Punkte aus der JSON geladen → `bereitsGemessen` korrekt befüllt.
3. **Audit**: Vollständige Nachvollziehbarkeit (vorhergesagte Position, Suchradius,
   Ergebnis) ohne den Protokoll-Stream durchsuchen zu müssen.
4. **Kein Overhead im Rechenweg**: Die JSON wird nur für Verwaltung genutzt;
   die Rechnung liest weiterhin die CSV.

**Kein Ersatz für `Anschlusspunkte.csv`**: JSON und CSV haben verschiedene Rollen.
Die JSON beschreibt *woher* ein Punkt kam; die CSV liefert die Zahlen für die Rechnung.

### Vorgeschlagene Struktur

Dateiname: `{ProjektOrdner}\AutoMatch_Punkte.json`

```json
{
  "version": 1,
  "standpunktNr": "STP-1",
  "punkte": [
    {
      "zeitstempel": "2026-04-24T10:23:11",
      "punktNr": "P301",
      "r": 5018.234,
      "h": 4013.781,
      "hoehe": 345.12,
      "hz_gon": 84.2100,
      "v_gon": 99.8800,
      "d_m": 23.450,
      "e_pred": 5018.201,
      "n_pred": 4013.748,
      "suchradius_m": 0.145,
      "abstand_m": 0.032,
      "ergebnis": "AutoMatch",
      "quelle": "DXF"
    }
  ]
}
```

### Verhältnis JSON ↔ CSV ↔ Protokoll

| Datei | Inhalt | Zweck |
|---|---|---|
| `Anschlusspunkte.csv` | Alle Anschlusspunkte (manuell + Handbetrieb + Auto-Match) | Stationierungsrechnung |
| `AutoMatch_Punkte.json` | Nur Auto-Match-Punkte mit vollem Kontext | Verwaltung, Undo, Resume |
| `AutoMatch_{STP}.csv` | Protokoll aller Suchereignisse inkl. Fehlschläge | Audit-Trail |

---

## 5  DXF-Visualisierung

Wenn Bestätigung nötig (Fälle c und d aus Abschnitt 2.1):

1. **DXF-Viewer öffnen / in Vordergrund bringen** via `FormDxfViewer.AktiveInstanz`.
2. **Suchkreis** – gestrichelter Overlay-Kreis (Cyan) um `(E_pred, N_pred)` mit Radius `r_suche`.
3. **Inner-Kreis** – gestrichelt Grün, Radius `r_suche/2` (Auto-Zone-Grenze).
4. **Kandidat(en)** – gefundene DXF-Punkte in Orange (Kreuz + PunktNr).
5. **Station** – kleines Dreieck Magenta.
6. Beim Schließen des Bestätigungs-Dialogs: Overlay entfernen.

Ohne Strecke: **Richtungsstrahl** (α ± δα als Keil von Station) statt Kreis.

Neue Overlay-Entitäten (in `PunktFinderOverlayEntities.cs`):
- `PunktFinderKreisEntity` – gestrichelter Kreis um Punkt
- `PunktFinderRichtungsEntity` – Keil für Winkel-only-Modus

DXF-Viewer-Referenz:
```csharp
// In FormDxfViewer.cs:
public static FormDxfViewer? AktiveInstanz { get; private set; }
protected override void OnLoad(EventArgs e)    { AktiveInstanz = this; base.OnLoad(e); }
protected override void OnFormClosed(...)      { if (AktiveInstanz == this) AktiveInstanz = null; }
```

---

## 6  UI in `FormFreieStationierung`

### 6.1  Handbetrieb-Bereich

Immer verfügbar (kein Stationserfordernis):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Handbetrieb – Punkt übernehmen                                             │
│  DXF-Punkt anklicken  →  aktuelle Tachymetermessung →  Anschlusspunkte.csv │
│  [DXF-Viewer öffnen]   Letztes Signal: Hz 84.21 gon / D 23.45 m            │
└─────────────────────────────────────────────────────────────────────────────┘
```

- Klick in DXF → `FormDxfViewer` feuert Event `PunktAngeklickt(PunktNr, R, H)`.
- `FormFreieStationierung` hört das Event: kombiniert mit `TachymeterMessungsCache.LetzteVollmessung`.
- Wenn keine aktuelle Messung → Warnung "Keine Tachymetermessung verfügbar".

### 6.2  Auto-Match-Bereich

Nur aktivierbar wenn `IstBrauchbar(s0_mm, redundanz) == true`:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Auto-Punktsuche                                                            │
│  [◉ Aktiv]   Nächste Messung automatisch zuordnen                          │
│  [✓ Distanz-Pflicht]   Nur mit Streckenmessung                             │
│  Suchradius: min 0.10 m   max 2.00 m       [Protokoll anzeigen]           │
└─────────────────────────────────────────────────────────────────────────────┘
```

- Bei `tiefrot`-Station: Toggle gesperrt + Hinweis "Station zu ungenau (s₀ > 20 mm)".
- **Protokoll-Button**: öffnet `FormAutoMatchProtokoll`.

### 6.3  Rückmeldung im Grid

**Auto-Match** (kein Dialog):
- Zeile direkt befüllen.
- Zellhintergrund kurz Grün blinken (500 ms).
- Statuszeile: `"✓ AutoMatch: P301 (DXF, Abstand 0.032 m, Radius 0.145 m)"`

**Bestätigung nötig**:
- Zeile Gelb färben (temporär).
- Infobereich unter Tabelle:
  ```
  Vorschlag: "P301"   R=5018.234   H=4013.781   Abstand=0.089 m
  [Übernehmen ✓]  [Ablehnen ✗]  [Manuell eingeben]
  ```
- DXF-Viewer mit Overlay öffnen.

**Mehrere Treffer** – kleiner Auswahl-Dialog:
```
PunktNr | R        | H        | Abstand
P301    | 5018.234 | 4013.781 |  0.089
P304    | 5018.891 | 4013.102 |  0.341
[Auswählen]  [Alle ablehnen]
```

### 6.4  Protokoll-Ansicht (`FormAutoMatchProtokoll`)

DataGridView aller protokollierten Ereignisse:
- Spalten: Zeitstempel, Station, Messung, Vorhergesagt, Radius, Treffer, Gewählt, Abstand, Ergebnis
- Filter nach Ergebnis (AutoMatch / Bestaetigt / Abgelehnt / KeinTreffer)
- Export-Button

---

## 7  Datenfluss-Übersicht

```
Tachymeter
    │ DatenEmpfangen (roh)
    ▼
TachymeterMessungsCache          ← statisch, in Program.cs initialisiert
    │ NeueVollmessung (TachymeterMessung)
    ▼
FormFreieStationierung           ← abonniert wenn Auto-Modus aktiv
    │
    ├─ PunktFinder.IstBrauchbar(s0_mm, redundanz)   → tiefrot: abbrechen
    │
    ├─ PunktFinder.BerechnePriorPosition(station, hz, v, d, massstab)
    │       → E_pred, N_pred
    │
    ├─ PunktFinder.BerechneRadius(s0_mm, n, D_h, konfig)
    │       → r_suche
    │
    ├─ PunktFinder.SucheNachPosition(E_pred, N_pred, r_suche, dxfIndex, bereitsGemessen)
    │       → List<PunktFinderTreffer>
    │
    ├─ Entscheidungslogik (0 / 1-Auto / 1-Bestätigung / ≥2)
    │       → Grid befüllen  oder  Bestätigungs-UI
    │
    ├─ DXF-Overlay-Entities erzeugen (wenn Bestätigung nötig)
    │       via FormDxfViewer.AktiveInstanz
    │
    ├─ FormDxfViewer.SchreibeAnschlusspunkt(punkt)  → Anschlusspunkte.csv
    │
    ├─ AutoMatchPunkte.Speichern(punkt)              → AutoMatch_Punkte.json
    │
    └─ AutoMatchProtokoll.Schreiben(ereignis, pfad)  → AutoMatch_{STP}.csv


Handbetrieb:
FormDxfViewer
    │ PunktAngeklickt(PunktNr, R, H)
    ▼
FormFreieStationierung
    │ + TachymeterMessungsCache.LetzteVollmessung
    │
    └─ FormDxfViewer.SchreibeAnschlusspunkt(punkt)  → Anschlusspunkte.csv
```

---

## 8  Erweiterung `DxfPunktIndex`

```csharp
public List<PunktEintrag> SucheNahe(double r, double h, double radiusM)
{
    var result = new List<PunktEintrag>();
    double r2 = radiusM * radiusM;
    foreach (var e in _eintraege)
    {
        double dr = e.R - r, dh = e.H - h;
        if (dr * dr + dh * dh <= r2)
            result.Add(e);
    }
    return result;
}

public List<(PunktEintrag Punkt, double Distanz_m)> SucheNachRichtung(
    double stationR, double stationH,
    double richtung_gon, double toleranz_gon,
    double maxDistanz_m = 500.0)
{
    const double GON2RAD = Math.PI / 200.0;
    double alpha_rad = richtung_gon * GON2RAD;
    double tol_rad   = toleranz_gon * GON2RAD;
    var result = new List<(PunktEintrag, double)>();
    foreach (var e in _eintraege)
    {
        double dr   = e.R - stationR;
        double dh   = e.H - stationH;
        double dist = Math.Sqrt(dr * dr + dh * dh);
        if (dist < 0.5 || dist > maxDistanz_m) continue;
        double alpha_p = Math.Atan2(dr, dh);
        double diff = Math.Abs(NormRad(alpha_rad - alpha_p));
        if (diff <= tol_rad) result.Add((e, dist));
    }
    return result.OrderBy(x => x.Item2).ToList();

    static double NormRad(double a)
    {
        while (a >  Math.PI) a -= 2 * Math.PI;
        while (a < -Math.PI) a += 2 * Math.PI;
        return a;
    }
}
```

---

## 9  Implementierungsreihenfolge

| Phase | Inhalt | Dateien |
|---|---|---|
| **1** | Kern-Algorithmus (testbar ohne UI) | `PunktFinder.cs`, `DxfPunktIndex.SucheNahe()` |
| **2** | Tachymeter-Cache | `TachymeterMessungsCache.cs`, Init in `Program.cs` |
| **3** | JSON-Verwaltung | `AutoMatchPunkte.cs` (Lesen/Schreiben) |
| **4** | CSV-Protokoll | `AutoMatchProtokoll.cs`, `FormAutoMatchProtokoll.cs` |
| **5** | DXF-Viewer-Referenz + Event | `FormDxfViewer.cs`: `AktiveInstanz`, `PunktAngeklickt` |
| **6** | Overlay-Entities | `PunktFinderOverlayEntities.cs` |
| **7** | UI-Integration | `FormFreieStationierung.cs/.Designer.cs` |
