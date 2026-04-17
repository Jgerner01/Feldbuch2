using System;
using System.IO;
using System.Windows;
using MangelManager.Data;
using MangelManager.ViewModels;

namespace MangelManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MangelManager");
        Directory.CreateDirectory(appData);

        var dbPath = Path.Combine(appData, "mangel.db");
        var fotoPath = Path.Combine(appData, "Fotos");

        var dbContext = new DatabaseContext(dbPath);
        var repository = new MangelRepository(dbContext);
        var viewModel = new MainViewModel(repository, fotoPath);

        var window = new MainWindow(viewModel);
        window.Show();
    }
}
