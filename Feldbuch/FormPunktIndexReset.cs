namespace Feldbuch;

// ──────────────────────────────────────────────────────────────────────────────
// Warn-Dialog für das Zurücksetzen des Punkt-Index.
// "Abbrechen" ist vorausgewählt, um versehentliches Löschen zu verhindern.
// ──────────────────────────────────────────────────────────────────────────────
public class FormPunktIndexReset : Form
{
    public FormPunktIndexReset()
    {
        Text            = "Punkt-Index zurücksetzen";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        ShowInTaskbar   = false;
        ClientSize      = new Size(460, 240);
        BackColor       = Color.FromArgb(244, 246, 250);

        // Warn-Icon
        var picWarn = new PictureBox
        {
            Size       = new Size(48, 48),
            SizeMode   = PictureBoxSizeMode.AutoSize,
            Location   = new Point(24, 24)
        };
        // System-Warn-Icon verwenden
        picWarn.Image = SystemIcons.Warning.ToBitmap();

        // Warn-Text
        var lblWarn = new Label
        {
            Text = "Alle DXF-Punkte erhalten neue Nummern.\n\n" +
                   "Bereits erstellte Protokolle, Messdaten und\n" +
                   "Stationierungen können dadurch nicht mehr den\n" +
                   "richtigen Punkten zugeordnet werden!\n\n" +
                   "Dies sollte nur getan werden, wenn noch keine\n" +
                   "Messungen mit den aktuellen Punktnummern\n" +
                   "durchgeführt wurden.",
            Location  = new Point(88, 24),
            Size      = new Size(350, 110),
            Font      = new Font("Segoe UI", 9.5F),
            AutoSize  = false
        };

        // Trennlinie
        var sep = new Label
        {
            Location = new Point(0, 170),
            Size     = new Size(460, 2),
            BackColor = Color.FromArgb(200, 205, 220)
        };

        // Abbrechen-Button (Default, Focus)
        var btnAbbrechen = new Button
        {
            Text         = "Abbrechen",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(170, 184),
            Size         = new Size(120, 34),
            Font         = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor    = Color.FromArgb(52, 100, 175),
            ForeColor    = Color.White,
            FlatStyle    = FlatStyle.Flat,
            FlatAppearance = { BorderColor = Color.FromArgb(36, 78, 150) }
        };

        // Zurücksetzen-Button
        var btnZuruecksetzen = new Button
        {
            Text         = "Zurücksetzen",
            DialogResult = DialogResult.OK,
            Location     = new Point(300, 184),
            Size         = new Size(140, 34),
            Font         = new Font("Segoe UI", 10F),
            BackColor    = Color.FromArgb(180, 50, 40),
            ForeColor    = Color.White,
            FlatStyle    = FlatStyle.Flat,
            FlatAppearance = { BorderColor = Color.FromArgb(140, 30, 20) }
        };

        Controls.AddRange(new Control[] { picWarn, lblWarn, sep, btnAbbrechen, btnZuruecksetzen });
        AcceptButton  = btnZuruecksetzen;  // Enter = Zurücksetzen
        CancelButton  = btnAbbrechen;       // Escape = Abbrechen

        // Fokus auf Abbrechen – Benutzer muss aktiv auf "Zurücksetzen" klicken
        btnAbbrechen.Focus();
    }
}
