using Avalonia;
using System.Collections.Generic;
using System.Linq;
using PerfMonLib;
using Avalonia.Media;

namespace Graph
{
    public class GraphLinearYAxis: GraphYAxis
    {
        public GraphLinearYAxis()
        {
            PlotRangeMargins = new GraphRange( 30, 30 );
        }

        public override IEnumerable<GraphValue> Transform( GraphSeries series, GraphRange dest )
        {
            var scale = ( dest.Max - dest.Min - PlotRangeMargins.Min - PlotRangeMargins.Max ) / ( Range.Max - Range.Min );

            double Transform( double p )
            {
                return dest.Max - ( p - Range.Min ) * scale - PlotRangeMargins.Min;
            }
            
            return series.Intervals
                            .Select( v => new GraphValue
                            {
                                Middle = Transform( v.Point.Y.Middle ),
                                Low = Transform( v.Point.Y.Low ),
                                High = Transform( v.Point.Y.High ),
                                First = Transform( v.Point.Y.First ),
                                Last = Transform( v.Point.Y.Last ),
                            } )
                            .ToArray();
        }

        public override void Render( DrawingContext dc, GraphScale scale, Rect dest )
        {
            if ( !scale.Series.Any() || !scale.Series.First().Intervals.Any() )
                return;
                
            if ( AutoRange )
            {
                Range = new GraphRange {
                    Min = scale.Series.Min( s => s.Intervals.Min( p => p.Point.Y.Low ) ),
                    Max = scale.Series.Max( s => s.Intervals.Max( p => p.Point.Y.High ) ),
                };
            }

            if ( Range.Max == Range.Min )
            {
                Range = new GraphRange( Range.Min - 10, Range.Max + 10 );
            }

            var yscale = ( dest.Height - PlotRangeMargins.Min - PlotRangeMargins.Max ) / ( Range.Max - Range.Min );

            double Transform( double p )
            {
                return dest.Height - ( p - Range.Min ) * yscale - PlotRangeMargins.Min;
            }

            var fontheight = 12;

            var scsub = CreateScaleSubdivision( dest.Height, Range, fontheight * 5 );

            var startpen = new Pen( new SolidColorBrush( Colors.Wheat, 2 ) );
            var surfacepen = new Pen( new SolidColorBrush( Colors.Wheat, 0.1 ) );

            var v1 = Transform( 0 );
            var v2 = Transform( 1 );

            var y = scsub.ScaleMin;
            while ( y <= scsub.ScaleMax )
            {
                var yc = (float)Transform( y );
                var ytypedval = scale.Series[0].YLabel( y );

                var ft = new FormattedText(
                            $"{ytypedval}",
                            Typeface.Default,
                            fontheight,
                            TextAlignment.Left,
                            TextWrapping.NoWrap,
                            Size.Infinity );

                if ( yc > dest.Top && yc < dest.Bottom - PlotRangeMargins.Min )
                {
                    switch ( Alignment )
                    {
                        case YAxisAlignment.Left:
                            dc.DrawLine( startpen, new Point( 0, yc ), new Point( 10, yc ) );
                            dc.DrawLine( surfacepen, new Point( 10, yc ), new Point( dest.Width, yc ) );

                            dc.DrawText(
                                    Brushes.Wheat,
                                    new Point( 15, yc - fontheight / 2 ),
                                    ft );
                            break;

                        case YAxisAlignment.Right:
                            dc.DrawLine( startpen, new Point( dest.Width, yc ), new Point( dest.Width - 10, yc ) );
                            dc.DrawLine( surfacepen, new Point( 10, yc ), new Point( 0, yc ) );

                            dc.DrawText(
                                    Brushes.Wheat,
                                    new Point( dest.Width - 15 - ft.Bounds.Width, yc - fontheight / 2 ),
                                    ft );
                            break;
                    }
                }

                y += scsub.Step;
            }
        }
    }
}