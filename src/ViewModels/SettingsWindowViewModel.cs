using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.ReactiveUI;
using PerfMonLib;
using ReactiveUI;

namespace PerfmonA.ViewModels
{
    public class SettingsWindowViewModel : ReactiveObject
    {
        Window MyWindow;

        TimeSpan? SamplingRateField = PerfMonContext.SamplingRate;

        public string? SamplingRate
        {
            get => SamplingRateField?.ToString();
            set
            {
                if ( TimeSpan.TryParse( value, out var newval ) )
                {
                    if ( newval.TotalSeconds < 1.0 )
                    {
                        this.RaiseAndSetIfChanged( ref SamplingRateField, null );
                        throw new DataValidationException( "Have to be 1 second or more" );
                    }

                    this.RaiseAndSetIfChanged( ref SamplingRateField, newval );
                    return;
                }
                this.RaiseAndSetIfChanged( ref SamplingRateField, null );
                throw new DataValidationException( "Not a time" );
            }
        }
        
        public TimeSpan? PresentationRateField = PerfMonContext.PresentationRate;

        public string? PresentationRate
        {
            get => PresentationRateField?.ToString();
            set
            {
                if ( TimeSpan.TryParse( value, out var newval ) )
                {
                    if ( newval.TotalSeconds < 1.0 )
                    {
                        this.RaiseAndSetIfChanged( ref SamplingRateField, null );
                        throw new DataValidationException( "Have to be 1 second or more" );
                    }

                    this.RaiseAndSetIfChanged( ref PresentationRateField, newval );
                    return;
                }
                this.RaiseAndSetIfChanged( ref PresentationRateField, null );
                throw new DataValidationException( "Not a time" );
            }
        }
        
        public int HistoryLengthField = PerfMonContext.HistoryLength;

        public int HistoryLength {
            get => HistoryLengthField;
            set
            {
                if ( value <= 1 )
                {
                    this.RaiseAndSetIfChanged( ref SamplingRateField, null );
                    throw new DataValidationException( "Needs to be > 1" );
                }
                this.RaiseAndSetIfChanged( ref HistoryLengthField, value );
            }
        }
        
        public string NetworkInterface { get; set; } = PerfMonContext.NetworkInterface;

        public IList<string> NetworkInterfaces
        {
            get
            {
                var ifs = NetworkMetric.Values
                    ?.Select( p => p.Key.Split( '.' )[0] )
                    .Distinct()
                    .ToList();
                return ifs!;
            }
        }

        readonly IPerfMonMetric NetworkMetric;

        public SettingsWindowViewModel( Window w, IPerfMonMetric num )
        {
            MyWindow = w;
            NetworkMetric = num;

            var isok = this.WhenAnyValue(
                                x => x.SamplingRate,
                                x => x.PresentationRate,
                                x => x.HistoryLength,
                                ( sr, pr, hl ) =>
                                    sr != null && pr != null && hl > 1 );
            
            OkCommand = ReactiveCommand.Create( OkChanges, isok );
            CancelCommand = ReactiveCommand.Create( CancelChanges );
        }

        public ReactiveCommand<Unit, Unit> OkCommand { get; }
        public void OkChanges()
        {
            PerfMonLib.PerfMonContext.SamplingRate = SamplingRateField.HasValue
                                                                ? SamplingRateField.Value
                                                                : TimeSpan.FromSeconds( 5 );
            PerfMonLib.PerfMonContext.PresentationRate = PresentationRateField.HasValue
                                                                ? PresentationRateField.Value
                                                                : TimeSpan.FromSeconds( 30 );
            PerfMonLib.PerfMonContext.HistoryLength = HistoryLength;
            PerfMonContext.NetworkInterface = NetworkInterface;

            MyWindow.Close();

            Program.SaveConfiguration();
        }

        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public void CancelChanges()
        {
            MyWindow.Close();
        }
    }
}
