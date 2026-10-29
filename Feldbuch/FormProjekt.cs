namespace Feldbuch;

public partial class FormProjekt : Form
{
    public FormProjekt()
    {
        InitializeComponent();
        ZeigeAktuellesProjekt();
    }

    private void ZeigeAktuellesProjekt()
    {
        if (ProjektManager.IstGeladen)
        {
            lblAktName.Text = $"Name:         {ProjektManager.ProjektName}";
            lblAktVerz.Text = $"Verzeichnis:  {ProjektManager.ProjektVerzeichnis}";
            // Felder vorbelegen
            txtName.Text        = ProjektManager.ProjektName;
            txtVerzeichnis.Text = ProjektManager.ProjektVerzeichnis;
        }
        else
        {
            lblAktName.Text = "Name:         (kein Projekt gewählt)";
            lblAktVerz.Text = "Verzeichnis:  –";
        }
    }

    // ── "..." Verzeichnis durchsuchen ─────────────────────────────────────────
    private void btnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description         = "Projektverzeichnis auswählen",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };
        if (!string.IsNullOrWhiteSpace(txtVerzeichnis.Text) &&
            Directory.Exists(txtVerzeichnis.Text))
            dlg.InitialDirectory = txtVerzeichnis.Text;

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        txtVerzeichnis.Text = dlg.SelectedPath;

        // Projektname aus Ordnernamen ableiten, wenn noch leer
        if (string.IsNullOrWhiteSpace(txtName.Text))
            txtName.Text = Path.GetFileName(dlg.SelectedPath);
    }

    // ── "Neues Projekt anlegen" ───────────────────────────────────────────────
    // Legt unter dem übergeordneten Ordner des aktuellen Projekts (ohne geladenes
    // Projekt: neben der EXE) ein Verzeichnis mit dem eingegebenen Namen an und
    // startet dort ein leeres Projekt.
    private void btnNeuAnlegen_Click(object? sender, EventArgs e)
    {
        string name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Bitte zuerst einen Projektnamen eingeben.",
                "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtName.Focus();
            return;
        }
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show("Der Projektname enthält ungültige Zeichen für einen Verzeichnisnamen.",
                "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtName.Focus();
            return;
        }

        string elternVerz = ErmittleElternVerzeichnis();
        string neuerPfad  = Path.Combine(elternVerz, name);

        if (Directory.Exists(neuerPfad))
        {
            MessageBox.Show(
                $"Das Verzeichnis existiert bereits:{Environment.NewLine}{neuerPfad}{Environment.NewLine}{Environment.NewLine}" +
                "Bitte einen anderen Namen wählen oder das vorhandene Projekt über „…“ und OK öffnen.",
                "Neues Projekt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtName.Focus();
            return;
        }

        try
        {
            Directory.CreateDirectory(neuerPfad);
            ProjektManager.NeuesLeeresProjekt(name, neuerPfad);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Projekt konnte nicht angelegt werden:" + Environment.NewLine + ex.Message,
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        DialogResult = DialogResult.OK;
    }

    /// <summary>Übergeordneter Ordner für neue Projekte.</summary>
    private static string ErmittleElternVerzeichnis()
    {
        if (ProjektManager.IstGeladen)
        {
            string? eltern = Directory.GetParent(ProjektManager.ProjektVerzeichnis)?.FullName;
            if (!string.IsNullOrEmpty(eltern) && Directory.Exists(eltern)) return eltern;
        }
        return AppPfade.Basis;
    }

    // ── OK ────────────────────────────────────────────────────────────────────
    private void btnOK_Click(object? sender, EventArgs e)
    {
        string name = txtName.Text.Trim();
        string verz = txtVerzeichnis.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Bitte einen Projektnamen eingeben.",
                "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtName.Focus();
            return;
        }
        if (string.IsNullOrEmpty(verz))
        {
            MessageBox.Show("Bitte ein Projektverzeichnis angeben.",
                "Eingabefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtVerzeichnis.Focus();
            return;
        }

        // Verzeichnis anlegen falls nicht vorhanden
        if (!Directory.Exists(verz))
        {
            var ans = MessageBox.Show(
                $"Verzeichnis existiert noch nicht:\n{verz}\n\nJetzt anlegen?",
                "Verzeichnis anlegen?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans != DialogResult.Yes) return;
            try { Directory.CreateDirectory(verz); }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler:\n" + ex.Message, "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        ProjektManager.SetProjekt(name, verz);
        DialogResult = DialogResult.OK;
    }

    // ── Abbrechen ─────────────────────────────────────────────────────────────
    private void btnAbbrechen_Click(object? sender, EventArgs e)
        => DialogResult = DialogResult.Cancel;
}
