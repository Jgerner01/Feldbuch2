using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MangelManager.Data;
using MangelManager.Models;
using Microsoft.Win32;

namespace MangelManager.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly MangelRepository _repository;
    private readonly string _fotoPfad;

    private Mangel? _ausgewaehlterMangel;
    private string _suchbegriff = "";
    private MangelStatus? _filterStatus;
    private MangelPrioritaet? _filterPrioritaet;
    private string? _filterGewerk;

    public ObservableCollection<Mangel> Maengel { get; } = new();
    public ObservableCollection<string> Gewerke { get; } = new();

    public MainViewModel(MangelRepository repository, string fotoPfad)
    {
        _repository = repository;
        _fotoPfad = fotoPfad;
        Directory.CreateDirectory(_fotoPfad);

        LadenCommand = new RelayCommand(_ => Laden());
        NeuCommand = new RelayCommand(_ => Neu());
        LoeschenCommand = new RelayCommand(_ => Loeschen(), _ => AusgewaehlterMangel != null);
        SpeichernCommand = new RelayCommand(_ => Speichern(), _ => AusgewaehlterMangel != null);
        StatusAendernCommand = new RelayCommand(param => StatusAendern(param), _ => AusgewaehlterMangel != null);
        FilternCommand = new RelayCommand(_ => Filtern());
        FotoHinzufuegenCommand = new RelayCommand(_ => FotoHinzufuegen(), _ => AusgewaehlterMangel != null);
        FotoLoeschenCommand = new RelayCommand(_ => FotoLoeschen(), _ => AusgewaehlterMangel?.HatFotos == true);
        KameraAufnahmeCommand = new RelayCommand(async _ => await KameraAufnahmeAsync(), _ => AusgewaehlterMangel != null);

        Laden();
    }

    // Eigenschaften
    public Mangel? AusgewaehlterMangel
    {
        get => _ausgewaehlterMangel;
        set
        {
            _ausgewaehlterMangel = value;
            OnPropertyChanged();
            ((RelayCommand)LoeschenCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SpeichernCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StatusAendernCommand).RaiseCanExecuteChanged();
            ((RelayCommand)FotoHinzufuegenCommand).RaiseCanExecuteChanged();
            ((RelayCommand)KameraAufnahmeCommand).RaiseCanExecuteChanged();
            ((RelayCommand)FotoLoeschenCommand).RaiseCanExecuteChanged();
        }
    }

    public string Suchbegriff
    {
        get => _suchbegriff;
        set => SetField(ref _suchbegriff, value);
    }

    public MangelStatus? FilterStatus
    {
        get => _filterStatus;
        set => SetField(ref _filterStatus, value);
    }

    public MangelPrioritaet? FilterPrioritaet
    {
        get => _filterPrioritaet;
        set => SetField(ref _filterPrioritaet, value);
    }

    public string? FilterGewerk
    {
        get => _filterGewerk;
        set => SetField(ref _filterGewerk, value);
    }

    // Commands
    public ICommand LadenCommand { get; }
    public ICommand NeuCommand { get; }
    public ICommand LoeschenCommand { get; }
    public ICommand SpeichernCommand { get; }
    public ICommand StatusAendernCommand { get; }
    public ICommand FilternCommand { get; }
    public ICommand FotoHinzufuegenCommand { get; }
    public ICommand FotoLoeschenCommand { get; }
    public ICommand KameraAufnahmeCommand { get; }

    // Methoden
    public void Laden()
    {
        Maengel.Clear();
        Gewerke.Clear();

        var maengel = _repository.GetAll();
        foreach (var m in maengel)
            Maengel.Add(m);

        var gewerke = _repository.GetAllGewerke();
        foreach (var g in gewerke)
            Gewerke.Add(g);
    }

    private void Filtern()
    {
        Maengel.Clear();
        var maengel = _repository.FilterBy(FilterStatus, FilterPrioritaet, FilterGewerk, Suchbegriff);
        foreach (var m in maengel)
            Maengel.Add(m);
    }

    private void Neu()
    {
        var mangel = new Mangel
        {
            MangelNummer = _repository.GetNextMangelNummer(),
            ErfasstAm = DateTime.Now,
            Status = MangelStatus.Offen,
            Prioritaet = MangelPrioritaet.Mittel
        };
        AusgewaehlterMangel = mangel;
    }

    private void Speichern()
    {
        if (AusgewaehlterMangel == null) return;

        if (AusgewaehlterMangel.Id == 0)
        {
            _repository.Insert(AusgewaehlterMangel);
            Maengel.Insert(0, AusgewaehlterMangel);
        }
        else
        {
            _repository.Update(AusgewaehlterMangel);
        }

        // Gewerke aktualisieren
        if (!Gewerke.Contains(AusgewaehlterMangel.Gewerk) && !string.IsNullOrWhiteSpace(AusgewaehlterMangel.Gewerk))
            Gewerke.Add(AusgewaehlterMangel.Gewerk);

        MessageBox.Show($"Mangel {AusgewaehlterMangel.MangelNummer} gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Loeschen()
    {
        if (AusgewaehlterMangel == null) return;

        var result = MessageBox.Show(
            $"Mangel {AusgewaehlterMangel.MangelNummer} wirklich löschen?\nDies kann nicht rückgängig gemacht werden.",
            "Löschen bestätigen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            // Fotos löschen
            foreach (var foto in AusgewaehlterMangel.Fotos)
            {
                _repository.DeleteFoto(foto.Id, foto.Dateipfad);
            }

            _repository.Delete(AusgewaehlterMangel.Id);
            Maengel.Remove(AusgewaehlterMangel);
            AusgewaehlterMangel = null;
        }
    }

    private void StatusAendern(object? parameter)
    {
        if (AusgewaehlterMangel == null || parameter is not MangelStatus neuerStatus) return;

        AusgewaehlterMangel.Status = neuerStatus;
        if (neuerStatus == MangelStatus.Erledigt)
            AusgewaehlterMangel.ErledigtAm = DateTime.Now;

        _repository.Update(AusgewaehlterMangel);
        OnPropertyChanged(nameof(AusgewaehlterMangel));
    }

    private void FotoHinzufuegen()
    {
        FotoHinzufuegenAsync().ConfigureAwait(false);
    }

    private async System.Threading.Tasks.Task FotoHinzufuegenAsync()
    {
        if (AusgewaehlterMangel == null) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Bilder|*.jpg;*.jpeg;*.png;*.bmp|Alle Dateien|*.*",
            Multiselect = true,
            Title = "Foto(s) auswählen"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var datei in dialog.FileNames)
            {
                await FotoSpeichernAsync(datei);
            }
            AktualisiereFotos();
        }
    }

    private System.Threading.Tasks.Task KameraAufnahmeAsync()
    {
        if (AusgewaehlterMangel == null) return System.Threading.Tasks.Task.CompletedTask;

        var dateiPfad = Services.KameraService.FotoAufnehmen(_fotoPfad);
        if (!string.IsNullOrEmpty(dateiPfad))
        {
            var foto = new MangelFoto
            {
                MangelId = AusgewaehlterMangel.Id,
                Dateipfad = dateiPfad,
                Beschreibung = "Kameraaufnahme",
                ErstelltAm = DateTime.Now
            };

            if (AusgewaehlterMangel.Id > 0)
                _repository.InsertFoto(foto);

            AusgewaehlterMangel.Fotos.Add(foto);
            AktualisiereFotos();
        }

        return System.Threading.Tasks.Task.CompletedTask;
    }

    private async System.Threading.Tasks.Task FotoSpeichernAsync(string quellDatei)
    {
        var zielName = $"{AusgewaehlterMangel!.MangelNummer}_{Guid.NewGuid()}{Path.GetExtension(quellDatei)}";
        var zielPfad = Path.Combine(_fotoPfad, zielName);

        await System.Threading.Tasks.Task.Run(() => File.Copy(quellDatei, zielPfad, true));

        var foto = new MangelFoto
        {
            MangelId = AusgewaehlterMangel.Id,
            Dateipfad = zielPfad,
            ErstelltAm = DateTime.Now
        };

        if (AusgewaehlterMangel.Id > 0)
        {
            _repository.InsertFoto(foto);
        }

        AusgewaehlterMangel.Fotos.Add(foto);
    }

    private void AktualisiereFotos()
    {
        OnPropertyChanged(nameof(AusgewaehlterMangel));
        ((RelayCommand)FotoLoeschenCommand).RaiseCanExecuteChanged();
    }

    private void FotoLoeschen()
    {
        // Letztes Foto löschen (einfache Implementierung)
        if (AusgewaehlterMangel?.Fotos.Count > 0 == true)
        {
            var foto = AusgewaehlterMangel.Fotos.Last();
            _repository.DeleteFoto(foto.Id, foto.Dateipfad);
            AusgewaehlterMangel.Fotos.Remove(foto);
            OnPropertyChanged(nameof(AusgewaehlterMangel));
        }
    }

    // PDF-Export
    public void ExportierePDF(string dateiPfad)
    {
        Services.PdfService.Export(Maengel.ToList(), dateiPfad);
    }

    public void RefreshAusgewaehlterMangel()
    {
        OnPropertyChanged(nameof(AusgewaehlterMangel));
    }
}

// RelayCommand-Implementierung
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
