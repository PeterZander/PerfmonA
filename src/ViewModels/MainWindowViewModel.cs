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
        ObservableCollection<GraphScale> ScalesField = new ObservableCollection<GraphScale>();
        public ObservableCollection<GraphScale> Scales { get => ScalesField; set => this.RaiseAndSetIfChanged( ref ScalesField, value ); }

        public GraphHistorySeries CpuHistory = new GraphHistorySeries( "Total CPU" );
        public GraphHistorySeries CpuSystemHistory = new GraphHistorySeries( "System CPU" );
        
        public PerfMonValue? LastValueField;
        public PerfMonValue? LastValue { get => LastValueField; set => this.RaiseAndSetIfChanged( ref LastValueField, value ); }

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

            Scales.Add( scale );
        }

        public void Update( IPerfMonMetric s, DateTime time )
        {
            if ( s is null || s.Values is null )
                return;

            LastValue = s.Values![$"CPUUse{Suffix}"];
            CpuHistory.Add( LastValue, time );
            CpuSystemHistory.Add( s.Values[$"CPUUseSystem{Suffix}"], time );

            this.RaisePropertyChanged( "Scales" );
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        IPerfMonMetric? NetUsage = PerfMonContext.GetMetric( "NetworkUse" );
        IPerfMonMetric? CpuSource = PerfMonContext.GetMetric( "CPUUse" );
        
        GraphHistorySeries TxHistory = new GraphHistorySeries( "Network transmit" );
        GraphHistorySeries RxHistory = new GraphHistorySeries( "Network receive" );
        
        public IndividualCpu AllCpus { get; } = new IndividualCpu( "" );

        double AllCpusLoadField;
        public double AllCpusLoad { get => AllCpusLoadField; set => this.RaiseAndSetIfChanged( ref AllCpusLoadField, value ); }

        string AllCpusLoadTextField = "";
        public string AllCpusLoadText { get => AllCpusLoadTextField; set => this.RaiseAndSetIfChanged( ref AllCpusLoadTextField, value ); }

        public ObservableCollection<GraphScale> NetScales { get; set; } = new ObservableCollection<GraphScale>();

        public ObservableCollection<IndividualCpu> CPUs { get; set; } = new ObservableCollection<IndividualCpu>();

        public string NetworkInterface {
            get => PerfMonContext.NetworkInterface;
            set
            {
                PerfMonContext.NetworkInterface = value;
                this.RaisePropertyChanged();
            }
        }

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
            
            NetScales.Add( new GraphRectangularLinearScale() );
            NetScales[0].Add( TxHistory );
            NetScales[0].Add( RxHistory );

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

                UpdateUI( () =>
                {
                    SettingsEnabled = true;

                    this.RaisePropertyChanged( "NetScales" );
                } );
            };

            CpuSource!.Update += async ( s, time ) =>
            {
                if ( s is null || s.Values is null )
                    return;

                var cpucount = Convert.ToInt64( s.Values!["CPUCount"] );

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

                var scpuuse = s.Values[$"CPUUse"];
                AllCpusLoad = Convert.ToDouble( scpuuse );
                AllCpusLoadText = scpuuse.ToString();

                UpdateUI( () => 
                {
                    AllCpus.Update( s, time );

                    for ( int i = 0; i < cpucount; ++i )
                    {
                        var one = CPUs[i];
                        one.Update( s, time );
                    }
                } );
            };
        }

        object UpdateUILock = new object();

        void UpdateUI( Action action )
        {
            lock ( UpdateUILock )
            {
                try
                {
                    if ( Avalonia.Threading.Dispatcher.UIThread.CheckAccess() )
                    {
                        action?.Invoke();
                    }
                    else
                    {
                        var t = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync( () =>
                        {
                            try
                            {
                                action?.Invoke();
                            }
                            catch( Exception ex )
                            {
                                System.Diagnostics.Debug.WriteLine( ex.ToString() );
                            }
                        } );

                        t.ConfigureAwait( false );
                    }
                }
                catch( Exception ex )
                {
                    System.Diagnostics.Debug.WriteLine( ex.ToString() );
                }
            }
        }

        public void ShowCPUPage()
        {
            if ( CPUVisible )
                return;
                
            UpdateUI( () =>
            {
                CPUHistoryVisible = false;
                NetworkHistoryVisible = false;
                CPUVisible = true;
            } );
        }

        public void ShowCPUHistoryPage()
        {
            UpdateUI( () =>
            {
                CPUVisible = false;
                NetworkHistoryVisible = false;
                CPUHistoryVisible = true;
            } );
        }

        public void ShowNetworkHistoryPage()
        {
            UpdateUI( () =>
            {
                CPUVisible = false;
                CPUHistoryVisible = false;
                NetworkHistoryVisible = true;
            } );
        }

        public void DoToggleFullscreen()
        {
            UpdateUI( () =>
            {
                MyWindow.WindowState = MyWindow.WindowState == WindowState.FullScreen
                                ? WindowState.Normal
                                : WindowState.FullScreen;
            } );
        }

        public async Task DoShowSettings()
        {
            var sw = new Views.SettingsWindow();
            sw.DataContext = new SettingsWindowViewModel( sw, NetUsage! );
            await sw.ShowDialog( MyWindow );
            UpdateUI( () => this.RaisePropertyChanged( "NetworkInterface" ) );
        }
    }
}
