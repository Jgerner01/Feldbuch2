using System.IO;
using System.Windows;
using MangelManager.ViewModels;
using Microsoft.Win32;

namespace MangelManager;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void PdfExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF-Datei|*.pdf",
            FileName = $"Maengelbericht_{System.DateTime.Now:yyyyMMdd_HHmm}.pdf",
            Title = "PDF-Export speichern"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _viewModel.ExportierePDF(dialog.FileName);
                MessageBox.Show($"PDF erfolgreich gespeichert:\n{dialog.FileName}",
                    "PDF-Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fehler beim PDF-Export:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void FilterReset_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FilterStatus = null;
        _viewModel.FilterPrioritaet = null;
        _viewModel.FilterGewerk = null;
        _viewModel.Suchbegriff = "";
        _viewModel.Laden();
    }
}
