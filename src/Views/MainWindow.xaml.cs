using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace PerfmonA.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var vm = (ViewModels.MainWindowViewModel)DataContext!;

            switch ( e.Key )
            {
                case Key.F1:
                    vm.ShowCPUPage();
                    break;

                case Key.F2:
                    vm.ShowCPUHistoryPage();
                    break;

                case Key.F3:
                    vm.ShowNetworkHistoryPage();
                    break;

                case Key.F:
                case Key.F11:
                    vm.DoToggleFullscreen();
                    break;
            }
        }

        public void ForceLayoutPass()
        {
            if ( VisualRoot is TopLevel toplevel )
            {
                toplevel.LayoutManager.ExecuteLayoutPass();
            }
        }
    }
}