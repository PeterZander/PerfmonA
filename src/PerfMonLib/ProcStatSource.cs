using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace PerfMonLib
{
    [PerfMonSource( ProcStatSource.SourceName )]
    public class ProcStatSource: IPerfMonSource
    {
        public const string SourceName = "/proc/stat";
        public string Name { get => SourceName; }
        public event Action<IPerfMonSource>? Update;

        ProcStatSource()
        {
            Task.Run( async () =>
            {
                var ct = TerminationSource.Token;
                while ( !ct.IsCancellationRequested )
                {
                    await Task.Delay( PerfMonContext.SamplingRate );
                    await Poll( ct );
                }
            } );
        }
        
        CancellationTokenSource TerminationSource = new CancellationTokenSource();
        public void Terminate()
        {
            TerminationSource.Cancel();
        }


        async Task Poll( CancellationToken ct )
        {
            void ParseCpu( PerfMonSourceCategory dest, string[] scolumns )
            {
                var columns = scolumns
                                    .Skip( 1 )
                                    .Select( c => long.Parse( c ) )
                                    .ToArray();

                dest.Values["UserTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[0] );
                dest.Values["NiceTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[1] );
                dest.Values["SystemTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[2] );
                dest.Values["IdleTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[3] );
                dest.Values["IowaitTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[4] );
                dest.Values["IrqTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[5] );
                dest.Values["SoftIrqTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[6] );
                dest.Values["StealTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[7] );
                dest.Values["GuestTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[8] );
                dest.Values["GuestniceTicks"] = new PerfMonValue( MessurementValueTypes.Tick, columns[9] );
            }

            if ( Update is null ) return;

            var st = await File.ReadAllTextAsync( "/proc/stat", Encoding.ASCII, ct );
            if ( st is null ) return;

            var newvalues = new PerfMonSourceValues
            {
                Time = DateTime.Now,
            };

            var lines = st.Split( '\n' );

            foreach( var line in lines )
            {
                var columns = line.Split( ' ', StringSplitOptions.RemoveEmptyEntries );
                if ( columns.Length < 1 ) continue;

                var newcat = new PerfMonSourceCategory();
                var key = columns[0];

                if ( key.StartsWith( "cpu" ) )
                {
                    ParseCpu( newcat, columns );
                }
                else
                {
                    var lcolumns = columns
                                        .Skip( 1 )
                                        .Select( c => long.Parse( c ) )
                                        .ToArray();
                    switch ( key )
                    {
                        case "ctxt":
                            newcat.Values["ContextSwitches"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[0] );
                            break;

                        case "btime":
                            newcat.Values["SecondsSinceEpoch"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[0] );
                            break;
                            
                        case "processes":
                            newcat.Values["ProcessesCreated"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[0] );
                            break;
                            
                        case "procs_running":
                            newcat.Values["RunnableThreads"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[0] );
                            break;
                            
                        case "procs_blocked":
                            newcat.Values["WaitingForIO"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[0] );
                            break;

                        case "intr":
                            for ( int i = 0; i < lcolumns.Length; ++i )
                            {
                                newcat.Values[$"Interrupt{i}"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[i] );
                            }
                            break;

                        case "softirq":
                            for ( int i = 0; i < lcolumns.Length; ++i )
                            {
                                newcat.Values[$"SoftInterrupt{i}"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[i] );
                            }
                            break;
                    }
                }

                newvalues.Categories[key] = newcat;
            }

            ValuesField = newvalues;

            Update?.Invoke( this );
        }

        PerfMonSourceValues? ValuesField;
        public PerfMonSourceValues? Values { get => ValuesField; }
    }
}