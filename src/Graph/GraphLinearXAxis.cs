using Avalonia;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Graph
{
    public class GraphLinearXAxis: GraphXAxis
    {
        public GraphLinearXAxis()
        {
            PlotRangeMargins = new GraphRange( 20, 20 );
        }

        public override IEnumerable<GraphValue> Transform( GraphSeries series, GraphRange dest )
        {
            var scale = ( dest.Max - dest.Min - PlotRangeMargins.Min - PlotRangeMargins.Max ) / ( Range.Max - Range.Min );

            double Transform( double p )
            {
                return ( p - Range.Min ) * scale + PlotRangeMargins.Min;
            }

            return series.Intervals
                            .Select( v => new GraphValue
                            {
                                Middle = Transform( v.Point.X.Middle ),
                                Low = Transform( v.Point.X.Low ),
                                High = Transform( v.Point.X.High ),
                                First = Transform( v.Point.X.First ),
                                Last = Transform( v.Point.X.Last ),
                            } )
                            .ToArray();
        }

        public override void Render( DrawingContext dc, IEnumerable<GraphScale> scales, Rect dest )
        {
            if ( ! scales.Any() || !scales.Any( s => s.Series.Any( se => se.Intervals.Any() ) ) )
                return;

            Range = new GraphRange {
                Min = scales.Min( s => s.Series.Min( se => se.Intervals.Min( p => p.Point.X.Low ) ) ),
                Max = scales.Max( s => s.Series.Max( se => se.Intervals.Max( p => p.Point.X.High ) ) ),
            };

            if ( Range.Max == Range.Min )
            {
                Range = new GraphRange( Range.Min - 10, Range.Max + 10 );
            }

            if ( !IsVisible ) 
                    return;
            
            var xscale = ( dest.Width - PlotRangeMargins.Max - PlotRangeMargins.Min ) / ( Range.Max - Range.Min );

            double Transform( double p )
            {
                return ( p - Range.Min ) * xscale + PlotRangeMargins.Min;
            }

            var fontheight = 12;

            var scsub = GraphYAxis.CreateScaleSubdivision( dest.Width, Range, fontheight * 10 );

            var startpen = new Pen( new SolidColorBrush( Colors.Wheat, 1 ) );
            var surfacepen = new Pen( new SolidColorBrush( Colors.Wheat, 0.1 ) );

            var x = scsub.ScaleMin;
            while ( x <= scsub.ScaleMax )
            {
                var xc = (float)Transform( x );
                if ( xc > dest.Right )
                    break;
                    
                var xtypedval = scales.First().Series[0].XLabel( x );

                dc.DrawLine( startpen, new Point( xc, dest.Height ), new Point( xc, dest.Height - 10 ) );
                dc.DrawLine( surfacepen, new Point( xc, dest.Height - 10 ), new Point( xc, 0 ) );

                var size = new Size( 10 * fontheight, 1.5 * fontheight );
                var txtpos = new Point( xc - size.Width / 2, dest.Height - 2 * fontheight );

                if ( txtpos.X + size.Width < dest.Right
                        && txtpos.X > dest.Left )
                {
                    dc.DrawText(
                            Brushes.Wheat,
                            txtpos,
                            new FormattedText(
                                    $"{xtypedval}",
                                    Typeface.Default,
                                    fontheight,
                                    TextAlignment.Center,
                                    TextWrapping.NoWrap,
                                    size ) );
                }

                x += scsub.Step;
            }
        }
    }
}