namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// TachymeterSignal – Quittungstöne am Instrument nach einer Messung.
//
// Nur Leica GeoCOM (TPS1200-Reihe, laut GeoCOM-Handbuch Kap. 9 BMM):
//   BMM_BeepNormal (RPC 11003) – ein Piepton        → Messung erfolgreich
//   BMM_BeepAlarm  (RPC 11004) – dreifacher Piepton → Messung fehlgeschlagen
// Tonhöhe und Länge sind am Gerät fest und nicht einstellbar.
//
// Bei allen anderen Protokollen (GSI Online, Sokkia SDR, Topcon, GNSS, manuell)
// oder ohne Verbindung geschieht nichts.
//
// Die Antwort "%R1P,0,0:0" wird von den Messfenstern ignoriert, weil sie keine
// Messwerte enthält.
// Hardware: Leica-Tachymeter mit GeoCOM   Autor: Johann Gerner
// ──────────────────────────────────────────────────────────────────────────────
public static class TachymeterSignal
{
    /// <summary>Ein Piepton: Messung erfolgreich.</summary>
    public static void Erfolg() => Senden(GeoCOMParser.RPC_BMM_BeepNormal);

    /// <summary>Dreifacher Piepton (Alarm): Messung fehlgeschlagen.</summary>
    public static void Fehler() => Senden(GeoCOMParser.RPC_BMM_BeepAlarm);

    private static void Senden(int rpc)
    {
        if (!TachymeterVerbindung.IstVerbunden
            || !TachymeterBefehlsgeberFactory.IstGeoCOM(TachymeterVerbindung.Modell))
            return;

        try
        {
            TachymeterVerbindung.GeoCOM_Senden($"%R1Q,{rpc},0:");
        }
        catch (Exception ex)
        {
            // Ein fehlender Quittungston darf den Messablauf nicht stören
            ErrorLogger.Log("TachymeterSignal.Senden", ex);
        }
    }
}
