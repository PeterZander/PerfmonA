using System;
using System.Collections.Generic;
using System.Linq;
using Graph;
using PerfMonLib;

namespace PerfmonA
{
    public class PerfmonSeriesInterval: GraphSeriesInterval
    {
        public GraphPosition PointField = new GraphPosition();

        public override GraphPosition Point
        {
            get => PointField;
        }

        public string LabelField = "";
        public override string Label
        {
            get
            {
                return LabelField;
            }
        }

        public List<PerfMonValue> Meassures = new List<PerfMonValue>();
    }

    public class GraphHistorySeries: GraphSeries
    {
        public override string Title { get; set; }
        public TimeSpan Window { get; set; } = TimeSpan.FromSeconds( 30 );
        
        DateTime LastUpdate = DateTime.Now;

        public GraphSeriesAveraging MiddleAveragingMessure = new GraphSeriesEMAAveraging( 4 );

        public GraphHistorySeries( string title )
        {
            Title = title;
        }

        
        public override string XLabel( double x )
        {
            return ( DateTime.FromFileTime( Convert.ToInt64( x * 1E6 ) ) ).ToLongTimeString();
        }

        public override string YLabel( double y )
        {
            if ( Intervals.Count == 0 ) return "";

            return ( ( (PerfmonSeriesInterval)Intervals.First() ).Meassures.First().FromDouble( y ) ).ToString();
        }


        public void Add( PerfMonValue val, DateTime time )
        {
            var dval = Convert.ToDouble( val );

            if ( Intervals.Count < 1 || DateTime.Now - LastUpdate > Window )
            {
                while ( Intervals.Count > PerfMonContext.HistoryLength )
                {
                    Intervals.RemoveAt( 0 );
                }

                MiddleAveragingMessure.NewBar();

                var newinter = new PerfmonSeriesInterval();
                var xval = time.ToFileTime() / 1E6;
                newinter.PointField.X = new GraphValue
                {
                    Middle = xval,
                    First = xval,
                    Last = xval,
                    High = xval,
                    Low = xval,
                };

                dval = MiddleAveragingMessure.Add( dval );
                newinter.PointField.Y = new GraphValue
                {
                    Middle = dval,
                    First = dval,
                    Last = dval,
                    High = dval,
                    Low = dval,
                };
                newinter.LabelField = val.FromDouble( dval ).ToString();

                newinter.Meassures.Add( val );
                Intervals.Add( newinter );
                
                LastUpdate = DateTime.Now;
            }
            else
            {
                var lastinter = (PerfmonSeriesInterval)Intervals.Last();
                var m = lastinter.Point;

                m.Y = new GraphValue
                {
                    Middle = MiddleAveragingMessure.Add( dval ),
                    First = m.Y.First,
                    Last = dval,
                    High = Math.Max( m.Y.High, dval ),
                    Low = Math.Min( m.Y.Low, dval ),
                };

                lastinter.Meassures.Add( val );
                lastinter.LabelField = val.FromDouble( m.Y.Middle ).ToString();
            }
        }
    }
}
