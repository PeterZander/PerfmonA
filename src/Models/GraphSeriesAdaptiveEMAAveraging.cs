using PerfMonLib;
using Graph;
using static PerfMonLib.PerfMonContext;

namespace PerfmonA
{
    public class GraphSeriesAdaptiveEMAAveraging: GraphSeriesAveraging
    {
        double Value;

        public GraphSeriesAdaptiveEMAAveraging()
        {
        }

        public override void NewBar() {}
        
        public override double Add( double newval )
        {
            var periods = PresentationRate.TotalSeconds / SamplingRate.TotalSeconds;
            var factor = 2 / ( periods + 1 );

            Value = ( newval - Value ) * factor + Value;
            return Value;
        }
    }
}