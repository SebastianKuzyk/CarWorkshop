using System.Windows;

namespace CarWorkshopWPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Aplikacja korzysta z gotowej bazy z projektu Python.
        // Brak EnsureCreated() - schemat już istnieje.
    }
}
