using System.Reactive;
using ReactiveUI;
using PerfMonLib;
using Graph;
using System.Collections.ObjectModel;
using Avalonia.Media;
using System.Collections.Generic;
using System;
using PerfmonA.Views;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Linq;

namespace PerfmonA.ViewModels
{
    public class IndividualCpu: ReactiveObject
    {
        public ObservableCollection<GraphScale> Scale { get; set; } = new ObservableCollection<GraphScale>();

        public GraphHistorySeries CpuHistory = new GraphHistorySeries( "Total CPU" );
        public GraphHistorySeries CpuSystemHistory = new GraphHistorySeries( "System CPU" );
        public PerfMonValue? LastValue { get; set; }

        public string Suffix { get; set; }

        public IndividualCpu( string suffix )
        {
            Suffix = suffix;

            CpuHistory.MinMaxFill = Colors.DarkBlue;

            CpuSystemHistory.MinMaxFill = Colors.DarkRed;

            var scale = new GraphRectangularLinearScale();
            scale.YAxis.PlotRangeMargins = new GraphRange { Min = 30, Max = 3 };

            if ( !string.IsNullOrWhiteSpace( suffix ) )
            {
                CpuHistory.MiddlePen = new Pen( Brushes.Cyan, 1 );
                CpuSystemHistory.MiddlePen = new Pen( Brushes.LightGoldenrodYellow, 1 );

                scale.YAxis.AutoRange = false;
                scale.YAxis.Range = new GraphRange { Min = 0, Max = 1.0 };
            }
            else
            {
                CpuHistory.MiddlePen = new Pen( Brushes.Cyan, 2 );
                CpuSystemHistory.MiddlePen = new Pen( Brushes.LightGoldenrodYellow, 2 );
            }

            scale.Add( CpuSystemHistory );
            scale.Add( CpuHistory );

            Scale.Add( scale );
        }

        public void Update( IPerfMonMetric s, DateTime time )
        {
            if ( s is null || s.Values is null )
                return;

            LastValue = s.Values![$"CPUUse{Suffix}"];
            CpuHistory.Add( LastValue, time );
            CpuSystemHistory.Add( s.Values[$"CPUUseSystem{Suffix}"], time );

            Scale[0].Series[0] = CpuSystemHistory;
            Scale[0].Series[1] = CpuHistory;

            this.RaisePropertyChanged( "LastValue" );
            this.RaisePropertyChanged( "Scale" );
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        IPerfMonMetric? NetUsage = PerfMonContext.GetMetric( "NetworkUse" );
        IPerfMonMetric? CpuSource = PerfMonContext.GetMetric( "CPUUse" );
        
        GraphHistorySeries TxHistory = new GraphHistorySeries( "Network transmit" );
        GraphHistorySeries RxHistory = new GraphHistorySeries( "Network receive" );
        
        IndividualCpu AllCpus = new IndividualCpu( "" );

        public ObservableCollection<GraphScale> AllCpusScale { get => AllCpus.Scale; }
        public double AllCpusLoad { get; set; }
        public string AllCpusLoadText { get; set; } = "";
        public ObservableCollection<GraphScale> NetScale { get; set; } = new ObservableCollection<GraphScale>();

        public ObservableCollection<IndividualCpu> CPUs { get; set; } = new ObservableCollection<IndividualCpu>();

        readonly MainWindow MyWindow;

        public bool SettingsEnabledField;
        public bool SettingsEnabled { get => SettingsEnabledField; set => this.RaiseAndSetIfChanged( ref SettingsEnabledField, value ); }

#region Command objects
        bool CPUVisibleField = true;
        public bool CPUVisible { get => CPUVisibleField; set => this.RaiseAndSetIfChanged( ref CPUVisibleField, value ); }
        public ReactiveCommand<Unit, Unit> ShowCPU { get; }

        bool CPUHistoryVisibleField = false;
        public bool CPUHistoryVisible { get => CPUHistoryVisibleField; set => this.RaiseAndSetIfChanged( ref CPUHistoryVisibleField, value ); }
        public ReactiveCommand<Unit, Unit> ShowCPUHistory { get; }

        bool NetworkHistoryVisibleField = false;
        public bool NetworkHistoryVisible { get => NetworkHistoryVisibleField; set => this.RaiseAndSetIfChanged( ref NetworkHistoryVisibleField, value ); }
        public ReactiveCommand<Unit, Unit> ShowNetworkHistory { get; }

        public ReactiveCommand<Unit, Unit> ToggleFullscreen { get; }
        public ReactiveCommand<Unit, Unit> ShowSettings { get; }

        public string NetworkInterface {
            get => PerfMonContext.NetworkInterface;
            set
            {
                PerfMonContext.NetworkInterface = value;
                this.RaisePropertyChanged();
            }
        }

#endregion
        
        public MainWindowViewModel( MainWindow window )
        {
            if ( NetUsage is null || CpuSource is null )
                throw new NotImplementedException( "NetUsage and CpuSource must not be null" );

            MyWindow = window;
            ShowCPU = ReactiveCommand.Create( ShowCPUPage );
            ShowCPUHistory = ReactiveCommand.Create( ShowCPUHistoryPage );
            ShowNetworkHistory = ReactiveCommand.Create( ShowNetworkHistoryPage );
            ToggleFullscreen = ReactiveCommand.Create( DoToggleFullscreen );
            ShowSettings = ReactiveCommand.CreateFromTask( DoShowSettings );

            RxHistory.MinMaxFill = Colors.DarkGreen;
            RxHistory.MiddlePen = new Pen( Brushes.GreenYellow, 2 );

            AllCpus.CpuSystemHistory.MinMaxFill = Colors.DarkRed;
            AllCpus.CpuSystemHistory.MiddlePen = new Pen( Brushes.LightGoldenrodYellow, 2 );
            
            NetScale.Add( new GraphRectangularLinearScale() );
            NetScale[0].Add( TxHistory );
            NetScale[0].Add( RxHistory );

            NetUsage!.Update += ( s, time ) => 
            {
                if ( s is null || s.Values is null )
                    return;

                if ( string.IsNullOrWhiteSpace( PerfMonContext.NetworkInterface ) )
                {
                    var ifs = s.Values!.Select( p => p.Key.Split( '.' )[0] ).Distinct();

                    if ( ifs.Count() > 1 ) ifs = ifs.Where( i => i != "lo" );
                    NetworkInterface = ifs.FirstOrDefault() ?? "";
                }

                var val = s.Values[$"{PerfMonContext.NetworkInterface}.TransmitBitsPerSecond"];
                TxHistory.Add( val, time );

                val = s.Values[$"{PerfMonContext.NetworkInterface}.ReceiveBitsPerSecond"];
                RxHistory.Add( val, time );

                NetScale[0].Series[0] = TxHistory;
                NetScale[0].Series[1] = RxHistory;

                SettingsEnabled = true;

                this.RaisePropertyChanged( "NetScale" );
            };

            CpuSource!.Update += async ( s, time ) =>
            {
                if ( s is null || s.Values is null )
                    return;

                var cpucount = Convert.ToInt64( s.Values!["CPUCount"] );

                AllCpus.Update( s, time );
                var scpuuse = s.Values[$"CPUUse"];
                AllCpusLoad = Convert.ToDouble( scpuuse );
                AllCpusLoadText = scpuuse.ToString();
                this.RaisePropertyChanged( "AllCpusLoad" );
                this.RaisePropertyChanged( "AllCpusLoadText" );

                if ( CPUs.Count < cpucount )
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync( () =>
                    {
                        for ( int i = CPUs.Count; i < cpucount; ++i )
                        {
                            var cpu = new IndividualCpu( $"{i}" );
                            CPUs.Add( cpu );
                        }
                    } );
                }

                for ( int i = 0; i < cpucount; ++i )
                {
                    var one = CPUs[i];
                    one.Update( s, time );
                    one.RaisePropertyChanged( "Scale" );
                }

                this.RaisePropertyChanged( "AllCpusScale" );
                this.RaisePropertyChanged( "CPUs" );
            };
        }
        public void ShowCPUPage()
        {
            if ( CPUVisible )
                return;
                
            CPUVisible = true;
            CPUHistoryVisible = false;
            NetworkHistoryVisible = false;
            MyWindow.ForceLayoutPass();
        }

        public void ShowCPUHistoryPage()
        {
            CPUHistoryVisible = true;
            CPUVisible = false;
            NetworkHistoryVisible = false;
        }

        public void ShowNetworkHistoryPage()
        {
            NetworkHistoryVisible = true;
            CPUVisible = false;
            CPUHistoryVisible = false;
        }

        public void DoToggleFullscreen()
        {
            MyWindow.WindowState = MyWindow.WindowState == WindowState.FullScreen
                                ? WindowState.Normal
                                : WindowState.FullScreen;
        }

        public async Task DoShowSettings()
        {
            var sw = new Views.SettingsWindow();
            sw.DataContext = new SettingsWindowViewModel( sw, NetUsage! );
            await sw.ShowDialog( MyWindow );
            this.RaisePropertyChanged( "NetworkInterface" );
        }
    }
}
