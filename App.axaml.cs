using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PerfmonA.ViewModels;
using PerfmonA.Views;

namespace PerfmonA
{
    public class App : Application
    {
        PerfMonLib.ProcNetDevSource test = new PerfMonLib.ProcNetDevSource();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            test.Update += ( s ) => {};
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}