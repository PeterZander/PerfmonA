using System;
using System.Collections.Generic;

namespace PerfMonLib
{
    [PerfMonMetric( NetworkUsageMetric.MetricName )]
    public class NetworkUsageMetric: IPerfMonMetric, IDisposable
    {
        public const string MetricName = "NetworkUse";
        public string Name { get => MetricName; }
        public event Action<IPerfMonMetric,DateTime>? Update;

        Dictionary<string,Snapshot> PrevValues = new Dictionary<string, Snapshot>();

        IPerfMonSource ProcNet = PerfMonContext.GetSource( "/proc/net/dev" )!;
        
        IDictionary<string,PerfMonValue>? IPerfMonMetric.Values { get => Values; }
        public Dictionary<string,PerfMonValue>? Values { get; protected set; }

        NetworkUsageMetric()
        {
            ProcNet.Update += PNSUpdate;
        }

        void IDisposable.Dispose()
        {
            ProcNet.Update -= PNSUpdate;
        }

        void PNSUpdate( IPerfMonSource s )
        {
            var newvalues = new Dictionary<string, Snapshot>();

            if ( s.Values is null )
                return;

            foreach( var c in s.Values!.Categories )
            {
                newvalues[c.Key] = new Snapshot( c.Value, s.Values.Time );
            }

            if ( PrevValues.Count > 0 )
            {

                var newdic = new Dictionary<string,PerfMonValue>();

                foreach( var c in newvalues )
                {
                    var prev = PrevValues[c.Key];
                    var dt = c.Value.Time - prev.Time;

                    newdic[$"{c.Key}.TransmitBitsPerSecond"] = new PerfMonValue(
                            MessurementValueTypes.Bitrate,
                            8 * ( c.Value.TransmitBytes - prev.TransmitBytes ) / dt.TotalSeconds );

                    newdic[$"{c.Key}.ReceiveBitsPerSecond"] = new PerfMonValue(
                            MessurementValueTypes.Bitrate,
                            8 * ( c.Value.ReceiveBytes - prev.ReceiveBytes ) / dt.TotalSeconds );
                }

                Values = newdic;

                Update?.Invoke( this, s.Values.Time );
            }

            PrevValues = newvalues;
        }

        public class Snapshot
        {
            public DateTime Time { get; }
            public long TransmitBytes { get; }
            public long ReceiveBytes { get; }

            public Snapshot( PerfMonSourceCategory c, DateTime time )
            {
                Time = time;

                TransmitBytes = Convert.ToInt64( c.Values["TransmitBytes"] );
                ReceiveBytes = Convert.ToInt64( c.Values["ReceiveBytes"] );
           }
        }
    }
}