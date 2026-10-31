using System.Text;

namespace Feldbuch;

/// <summary>Setzt beliebig fragmentierte Empfangsblöcke zu vollständigen Zeilen zusammen.</summary>
public sealed class SerielleZeilen
{
    private readonly StringBuilder puffer = new();

    public IReadOnlyList<string> Empfange(string daten)
    {
        lock (puffer)
        {
            var zeilen = new List<string>();
            foreach (char c in daten)
            {
                if (c is '\r' or '\n')
                {
                    if (puffer.Length > 0) zeilen.Add(puffer.ToString());
                    puffer.Clear();
                }
                else
                {
                    if (puffer.Length >= 65536)
                    {
                        puffer.Clear();
                        throw new IOException("Serielle Antwort überschreitet die maximale Zeilenlänge.");
                    }
                    puffer.Append(c);
                }
            }
            return zeilen;
        }
    }
}
