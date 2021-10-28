using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace PerfMonLib
{
    [PerfMonSource( ProcNetDevSource.SourceName )]
    public class ProcNetDevSource: IPerfMonSource
    {
        public const string SourceName = "/proc/net/dev";
        public string Name { get => SourceName; }
        public event Action<IPerfMonSource>? Update;

        ProcNetDevSource()
        {
            Task.Run( async () =>
            {
                var ct = TerminationSource.Token;
                while ( !ct.IsCancellationRequested )
                {
                    await Task.Delay( PerfMonContext.SamplingRate );
                    try
                    {
                        await Poll( ct );
                    }
                    catch( Exception ex )
                    {
                        Debug.WriteLine( ex.ToString() );
                    }
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
            if ( Update is null ) return;

            var st = await File.ReadAllTextAsync( "/proc/net/dev", Encoding.ASCII, ct );
            if ( st is null ) return;

            var newvalues = new PerfMonSourceValues
            {
                Time = DateTime.Now,
            };

            var lines = st.Split( '\n' );

            foreach( var line in lines )
            {
                var columns = line.Split( ' ', StringSplitOptions.RemoveEmptyEntries );
                if ( columns.Length < 1 || !columns[0].Contains( ':' ) ) continue;

                var newcat = new PerfMonSourceCategory();
                var key = columns[0].Substring( 0, columns[0].Length - 1 );

                var lcolumns = columns
                                    .Skip( 1 )
                                    .Select( c => long.Parse( c ) )
                                    .ToArray();

                newcat.Values["ReceiveBytes"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[0] );
                newcat.Values["ReceivePackets"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[1] );
                newcat.Values["ReceiveErrors"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[2] );
                newcat.Values["ReceiveDropped"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[3] );
                newcat.Values["ReceiveFifo"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[4] );
                newcat.Values["ReceiveFrame"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[5] );
                newcat.Values["ReceiveCompressed"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[6] );
                newcat.Values["ReceiveMulticast"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[7] );

                newcat.Values["TransmitBytes"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[8] );
                newcat.Values["TransmitPackets"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[9] );
                newcat.Values["TransmitErrors"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[10] );
                newcat.Values["TransmitDropped"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[11] );
                newcat.Values["TransmitFifo"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[12] );
                newcat.Values["TransmitCollisions"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[13] );
                newcat.Values["TransmitCarrier"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[14] );
                newcat.Values["TransmitCompressed"] = new PerfMonValue( MessurementValueTypes.Count, lcolumns[15] );

                newvalues.Categories[key] = newcat;
            }

            ValuesField = newvalues;

            Update?.Invoke( this );
        }

        PerfMonSourceValues? ValuesField;
        public PerfMonSourceValues? Values { get => ValuesField; }
    }
}