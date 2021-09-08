using System;
using System.Collections.Generic;

namespace PerfMonLib
{
    [PerfMonMetric( CPUUsageMetric.MetricName )]
    public class CPUUsageMetric: IPerfMonMetric, IDisposable
    {
        public const string MetricName = "CPUUse";
        public string Name { get => MetricName; }
        public event Action<IPerfMonMetric,DateTime>? Update;

        IPerfMonSource ProcStat = PerfMonContext.GetSource( "/proc/stat" )!;
        
        IDictionary<string,PerfMonValue>? IPerfMonMetric.Values { get => Values; }
        public Dictionary<string,PerfMonValue>? Values { get; protected set; }

        readonly long TickFrequency;
        readonly long CPUCount;

        CPUState AllCpus;
        List<CPUState> IndividualCpus = new List<CPUState>();

        CPUUsageMetric()
        {
            TickFrequency = LibC.sysconf( LibC.SysconfArgs._SC_CLK_TCK );
            CPUCount = LibC.sysconf( LibC.SysconfArgs._SC_NPROCESSORS_ONLN );

            AllCpus = new CPUState( "cpu", "", CPUCount );

            ProcStat.Update += PSUpdate;
        }

        void IDisposable.Dispose()
        {
            ProcStat.Update -= PSUpdate;
        }

        public class TicksSnapshot
        {
            public long UserTicks { get; set; }
            public long NiceTicks { get; set; }
            public long SystemTicks { get; set; }

            public DateTime Time { get; }

            public TicksSnapshot( PerfMonSourceCategory cat, DateTime dt )
            {
                UserTicks = Convert.ToInt64( cat.Values["UserTicks"] ) - Convert.ToInt64( cat.Values["GuestTicks"] );
                NiceTicks = Convert.ToInt64( cat.Values["NiceTicks"] ) - Convert.ToInt64( cat.Values["GuestniceTicks"] );
                SystemTicks = Convert.ToInt64( cat.Values["SystemTicks"] )
                                + Convert.ToInt64( cat.Values["IrqTicks"] )
                                + Convert.ToInt64( cat.Values["SoftIrqTicks"] );
                Time = dt;
            }
        }

        protected double TicksToSeconds( double ticks )
        {
            return (double)ticks / TickFrequency;
        }

        public class CPUState
        {
            TicksSnapshot? PrevValues;

            public string Category { get; }
            public string Suffix { get; }

            readonly long Divider;

            public CPUState( string cat, string suffix, long divider )
            {
                Category = cat;
                Suffix = suffix;
                Divider = divider;
            }

            public bool Update( Dictionary<string,PerfMonValue> dic, IPerfMonSource s, Func<double,double> t2s )
            {
                var result = false;

                if ( s.Values is null )
                    return false;

                var newvalues = new TicksSnapshot( s.Values!.Categories[Category], s.Values.Time );

                if ( PrevValues != null )
                {
                    var dt = s.Values.Time - PrevValues.Time;

                    var totalcputime = t2s( 
                                            newvalues.UserTicks - PrevValues.UserTicks
                                            + newvalues.NiceTicks - PrevValues.NiceTicks
                                            + newvalues.SystemTicks - PrevValues.SystemTicks );

                    dic[$"CPUUse{Suffix}"] = new PerfMonValue(
                            MessurementValueTypes.Percent,
                            totalcputime / ( dt.TotalSeconds * Divider ) );
                    dic[$"CPUUseNice{Suffix}"] = new PerfMonValue(
                            MessurementValueTypes.Percent,
                            t2s( newvalues.NiceTicks - PrevValues.NiceTicks ) / ( dt.TotalSeconds * Divider ) );
                    dic[$"CPUUseSystem{Suffix}"] = new PerfMonValue(
                            MessurementValueTypes.Percent,
                            t2s( newvalues.SystemTicks - PrevValues.SystemTicks ) / ( dt.TotalSeconds * Divider ) );

                    result = true;
                }

                PrevValues = newvalues;

                return result;
            }
        }

        void PSUpdate( IPerfMonSource s )
        {
            Dictionary<string,PerfMonValue>? newdic = Values;

            if ( Values is null )
            {
                newdic = new Dictionary<string,PerfMonValue>();
                newdic["CPUCount"] = new PerfMonValue( MessurementValueTypes.Count, CPUCount );
            }

            var update = AllCpus.Update( newdic!, s, TicksToSeconds );

            for ( int i = 0; i < CPUCount; ++i )
            {
                CPUState one;
                
                if ( IndividualCpus.Count <= i )
                {
                    one = new CPUState( $"cpu{i}", $"{i}", 1 );
                    IndividualCpus.Add( one );
                }
                else
                {
                    one = IndividualCpus[i];
                };

                update |= one.Update( newdic!, s, TicksToSeconds );
            }

            if ( update && s.Values != null )
            {
                Values = newdic;

                Update?.Invoke( this, s.Values!.Time );
            }
        }
    }
}