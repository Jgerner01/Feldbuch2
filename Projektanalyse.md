# Projektanalyse Feldbuch2

Ursprüngliche Analyse: 31. Oktober 2026. Untersucht wurden insbesondere das WinForms-Hauptprojekt, Rechenkern, Projektwechsel, JSON-/CSV-Persistenz, serielle Kommunikation und vorhandene Tests. Keine Prüfung mit realem Tachymeter und keine vollständige fachliche Zertifizierung der Vermessungsalgorithmen.

## Bearbeitungsstand vom 1. November 2026

Die unten dokumentierten konkreten Fehler wurden im Anwendungscode bearbeitet:

- Gemeinsame JSON-Optionen erlauben das konsistente Speichern und Laden nicht bestimmter Residuen.
- Projektwechsel setzen Stationszustand und Schreibpfad zurück; ein Projektwechsel ohne Standpunkt übernimmt keine alte Station.
- Ladefehler sperren die betroffene Datei gegen Überschreiben. Fehlende Pflichtlisten und ungültige Einträge gelten ebenfalls als Ladefehler. Nach Wiederherstellung und erneutem Laden wird die Sperre aufgehoben.
- Ein gemeinsamer Speicherdienst ersetzt JSON-, CSV-, XML- und Textdateien atomar. Auto-Backup sichert bei aktivierter Option die vorherige Version im Unterordner `Backup`. Speicherfehler werden weitergereicht und in der Anwendung angezeigt.
- CSV-Verarbeitung unterstützt Anführungszeichen, Kommas und mehrzeilige Felder; zusammengehörige Stationsparameter können gemeinsam geschrieben werden.
- Ein einziger serieller Portleser verteilt Rohdaten und gepufferte Antwortzeilen. Asynchrone Anfragen werden untereinander serialisiert; Handler werden unabhängig vom veränderlichen Portfeld entfernt. Health-Checks überlappen nicht mehr.
- Höhenfehler erzeugen eine explizite Warnung zum 2D-Rückfall. Die Ergebnis- und Polarpunktmasken zeigen diesen Zustand an.
- Die gemeinsame Stationsprüfung berücksichtigt Konvergenz und endliche Ergebnisse. Die aktive freie Station benötigt mindestens zwei verschiedene Anschlusspunkte.
- Beschreibbare Einstellungen und Fehlerprotokolle verwenden beim Programmstart das Benutzerverzeichnis. Eine separate Desktop-Solution und ein GitHub-Workflow vermeiden den MAUI-Workloadbedarf für den Desktop-Build.
- Formular-Smoke-Tests haben eine begrenzte Wartezeit und unterdrücken modale WinForms-Fehlerdialoge. Die drei Fenstergrößentests liefen außerhalb der Sandbox erfolgreich; der ursprüngliche GDI+-Fehler ließ sich damit auf die eingeschränkte Testumgebung eingrenzen.

Die nachfolgenden Befunde beschreiben den ursprünglichen Zustand. Die umfassende Aufteilung großer Formularklassen und die Ablösung sämtlicher statischer Manager bleiben längerfristige Architekturverbesserungen. Eine reale Geräteprüfung steht weiterhin aus.

Validierung der Umsetzung: Desktop-Release-Build ohne Warnungen und Fehler; vollständiger Release-Testlauf mit 119 erfolgreichen Tests, ohne übersprungene Tests. Die ergänzten Regressionstests prüfen unter anderem Stationswechsel, JSON-Rundlauf, Erhalt defekter Dateien, Freigabe nach Wiederherstellung, CSV-Sonderzeichen, echte Backup-Inhalte und Dateisperren bei Schreibfehlern.

## Einschätzung

Das Projekt bietet einen großen Funktionsumfang und bereits sinnvolle Referenztests für Berechnungen. Die größten belegten Risiken betreffen Datenverlust und die Übernahme eines alten Stationszustands in ein anderes Projekt. Diese Themen sollten vor neuen Funktionen behandelt werden.

## Priorisierte Befunde

### 1. Hoch: Stationierungen mit NaN lassen sich nicht speichern

Fundstellen: `Feldbuch/FreieStationierung.cs:61`, `:275–298`, `Feldbuch/StationsdatenManager.cs:239–248`.

Nicht bestimmte Residuen und Richtungsgewichte werden als `double.NaN` modelliert. Das tritt regulär auf, beispielsweise bei einer Zweipunktstationierung oder bei einer 2D-Berechnung. Die verwendeten Standardoptionen von `JsonSerializer` können diese Werte nicht serialisieren. Der Fehler wird nur geloggt; der Aufrufer erhält keinen Fehlerstatus.

Gezielt reproduziert: Eine berechnete Zweipunktstationierung löst beim Serialisieren eine `ArgumentException` aus.

Vorschlag: Nicht vorhandene Werte im Speicherformat als nullable Zahlen abbilden; alternativ eine ausdrücklich definierte und auf beiden Seiten konsistente NaN-Konvertierung verwenden. Speicherfehler an die Oberfläche zurückgeben. Regressionstest: Berechnen, speichern, neu laden und Ergebnis vergleichen, auch für 2D und deaktivierte Beobachtungen.

### 2. Hoch: Projektwechsel kann die vorherige Station weiterverwenden

Fundstellen: `Feldbuch/Form1.cs:119–128`, `Feldbuch/StationsdatenManager.cs:81–98`.

`LadeAktuellenStandpunkt()` verändert den aktiven Stationszustand nur, wenn das neue Projekt eine Standpunktnummer enthält. Andernfalls bleiben `_aktiv` und `_pfad` vom vorherigen Projekt erhalten. Dadurch kann die alte Station weiter als gültig erscheinen; spätere Schreiboperationen können weiterhin auf deren Datei zeigen.

Gezielt reproduziert: Nach dem Wechsel zu Projektdaten ohne Standpunkt blieb die Station `ALT` aktiv und `HatGueltigeStation` meldete `true`.

Vorschlag: Stationszustand und Dateipfad beim Projektwechsel immer zurücksetzen. Projektbezogene Manager langfristig über einen expliziten Projektkontext statt globale statische Zustände verwalten. Regressionstest A → leeres B → A.

### 3. Hoch: Ladefehler ermöglichen das Überschreiben vorhandener Daten

Fundstellen: `Feldbuch/FeldbuchpunkteManager.cs:71–93`, vergleichbar `NeupunkteManager.Laden` und `StationsdatenManager.Laden`.

Eine nicht lesbare JSON-Datei wird als leerer Datenbestand behandelt. Beim nächsten Hinzufügen wird dieser Bestand wieder auf dieselbe Datei geschrieben. Der Ladefehler und ein tatsächlich leeres Projekt sind für den Aufrufer nicht unterscheidbar.

Gezielt reproduziert: Eine syntaktisch defekte Punktdatei wurde nach `Initialize` und `AddOrUpdate` durch einen neuen Bestand ersetzt.

Vorschlag: Ladefehler als eigenen Zustand behandeln, Schreibzugriffe bis zur bewussten Wiederherstellung sperren und Originaldatei erhalten. Wiederherstellung aus einer Sicherung anbieten.

### 4. Hoch: Direkte Dateiersetzung und funktionslose Auto-Backup-Option

Fundstellen: `Feldbuch/FeldbuchpunkteManager.cs:91`, `NeupunkteManager.Speichern`, `StationsdatenManager.Speichern`, `Feldbuch/ProjektManager.cs:25–26`.

Zentrale Dateien werden direkt mit `File.WriteAllText` überschrieben. Ein Abbruch während des Schreibens kann eine unvollständige Datei hinterlassen. Die sichtbare Option „Auto-Backup“ wird gespeichert und geladen, besitzt aber im untersuchten Code keine Sicherungsfunktion; sie ist ausdrücklich als Platzhalter kommentiert.

Vorschlag: Gemeinsamen Speicherdienst einführen: temporäre Datei im Zielverzeichnis schreiben, anschließend atomar ersetzen und vorherige Version sichern. Erfolg erst nach erfolgreicher Speicherung melden. Backup-Option implementieren oder bis dahin klar deaktivieren.

### 5. Mittel: CSV-Speichern und -Laden sind nicht symmetrisch

Fundstelle: `Feldbuch/ProjektdatenManager.cs:78–83`, `:95–96`.

Der Schreiber setzt Werte mit Komma in Anführungszeichen, der Leser verwendet lediglich `Split(',')` und entfernt diese Anführungszeichen nicht. Anführungszeichen und Zeilenumbrüche werden ebenfalls nicht vollständig behandelt.

Gezielt reproduziert: Der Wert `A,B` wurde nach Speichern und Laden zum Wert `"A,B"`, einschließlich der Anführungszeichen.

Vorschlag: Einen konsistenten CSV-Parser und -Schreiber verwenden. Rundlauftests für Komma, Anführungszeichen, Zeilenumbruch und leere Werte ergänzen.

### 6. Mittel: Zwei Leser konkurrieren um serielle Antworten

Fundstellen: `Feldbuch/TachymeterVerbindung.cs:81–87`, `:204–225`, `:265`.

Der permanente Empfangshandler und der temporäre Handler von `GeoCOM_SendenUndAntwortAsync` lesen beide mit `ReadExisting()` denselben Port. Der erste Leser kann die Antwort bereits konsumieren. Außerdem wird eine Antwort nicht über mehrere Empfangsblöcke zusammengesetzt. Im `finally` wird das veränderliche Feld `_port` verwendet, obwohl ein Verbindungsabbruch es auf null setzen kann.

Dieser Befund stammt aus der Codeprüfung; kein Hardwaretest. Die asynchrone Methode wird innerhalb der Verbindungsüberwachung verwendet. Ein aktiver Aufruf zum Start dieser Überwachung wurde im untersuchten Anwendungscode nicht gefunden, daher ist die aktuelle Auswirkung auf reguläre Messungen nicht belegt.

Vorschlag: Ein einziger Empfangsleser mit Zeilenpuffer und Antwortverteilung; Befehle serialisieren und Abbruch/Verbindungswechsel explizit behandeln. Mit simulierten fragmentierten und verspäteten Antworten prüfen.

### 7. Mittel: Höhenfehler führen ohne ausgefüllte Warnung zum 2D-Ergebnis

Fundstelle: `Feldbuch/FreieStationierung.cs:234–265`, `:314`.

Bei zu großen Höhenresiduen werden `ist3D=false` und `hMittel=0` gesetzt. Die Variable `warnHoehe` bleibt leer und wird so als `WarnungHoehe` zurückgegeben. Der Berechnungsstatus zeigt zwar 2D, erklärt dem Aufrufer aber nicht den Grund des Rückfalls.

Vorschlag: Ursache und betroffene Beobachtungen als strukturierten Qualitätsstatus zurückgeben und vor einer Übernahme sichtbar machen. Regressionstest mit widersprüchlichen Höhen.

### 8. Mittel: Gültigkeitsprüfung berücksichtigt die Konvergenz nicht

Fundstelle: `Feldbuch/StationsdatenManager.cs:43–44`.

`HatGueltigeStation` prüft lediglich auf ein Ergebnis und mindestens zwei Messungen. Der Rechenkern kann auch ein Ergebnis mit `Konvergiert=false` liefern. Diese Eigenschaft wird hier nicht berücksichtigt, obwohl die Gültigkeitsabfrage im DXF-Viewer die Verwendung einer Station steuert.

Vorschlag: Gemeinsame Qualitätsprüfung für Konvergenz, endliche Koordinaten, Geometrie und anwendungsabhängige Fehlergrenzen einführen. Fehlende Redundanz gesondert ausweisen; zwei Messungen sind nicht automatisch zwei unterschiedliche Anschlusspunkte.

## Weitere Verbesserungen

- Einstellungen und beschreibbare Nutzerdaten von Programmressourcen trennen. `AppPfade` legt Einstellungen neben die EXE; bei einer Installation in einem geschützten Verzeichnis sind Schreibfehler möglich.
- `ProjektdatenManager.SetValue` schreibt nach jedem Einzelwert die gesamte wachsende Historie neu. Stationsdaten als zusammengehörigen Satz aktualisieren und einmal speichern.
- Große Formularklassen schrittweise aufteilen, insbesondere `FormDxfViewer.cs` mit rund 105 KB. Berechnung, Speicherung und Gerätekommunikation aus den Formularen lösen.
- Desktop-Build und Tests unabhängig vom MAUI-Gerüst bauen. Die Solution enthält zusätzlich ein Projekt mit Android-/iOS-/MacCatalyst-Zielen und entsprechendem Workloadbedarf.
- Kontinuierliche Builds und Regressionstests ergänzen. Im geprüften Repository wurden keine YAML-Workflows gefunden.

## Prüfung und Grenzen

- Hauptprojekt und Testprojekt erfolgreich kompiliert.
- 87 Tests außerhalb von `FormPolarpunkteSmokeTests` erfolgreich ausgeführt.
- Der vollständige Testlauf lieferte kein Abschlussresultat und wurde abgebrochen. Der separate Formular-Testlauf wurde nach fünf erfolgreichen Tests wegen 30 Sekunden Inaktivität automatisch beendet. Zu diesem Zeitpunkt lief `FormPolarpunkteSmokeTests.Maske_LaesstSichBeiJederFensterhoeheAnzeigen`. Die genaue Ursache des Hängers ist noch nicht geklärt; ein fehlerhafter Rechenkern folgt daraus nicht.
- Vier zusätzliche gezielte Ausführungen bestätigten die Fehler bei NaN-Serialisierung, Stationswechsel, Überschreiben nach Ladefehler und CSV-Rundlauf. Der Prüfcode liegt außerhalb des Repositories in `../.feldbuch-review` und arbeitet mit temporären Testdaten.
- Keine Aussage über gemessene Genauigkeit am realen Instrument, vollständige Testabdeckung oder die Abwesenheit weiterer Sicherheitsprobleme.

## Empfohlene Reihenfolge

1. Projektwechsel und NaN-Speicherung korrigieren, mit Regressionstests absichern.
2. Datenverlustschutz, echte Backups und sichtbare Speicherfehler umsetzen.
3. CSV-Rundlauf und einheitliche Stationsqualität korrigieren.
4. Formular-Testhänger beseitigen und automatische Builds einrichten.
5. Serielle Kommunikation vereinheitlichen und große Formularklassen schrittweise entflechten.
