using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PerfMonLib;
using Avalonia.Media;
using Avalonia.Controls.Shapes;

namespace PerfmonA
{
    public struct GraphRange
    {
        public double Min;
        public double Max;
    }

    public class GraphSeries
    {
        public IList<Point>? Values { get; set; }
    }

    public abstract class GraphAxis
    {
        public GraphRange Range { get; set; } = new GraphRange { Min = 0, Max = 100.0 };

        public bool AutoRange { get; set; } = false;

        public abstract IEnumerable<double> Transform( GraphSeries series, GraphRange destination );
        public abstract void Render( DrawingContext dc, GraphScale scale );
    }

    public abstract class GraphStyle
    {
        public abstract void Render( DrawingContext dc, GraphScale scale );
    }

    public abstract class GraphScale
    {
        public abstract GraphAxis XAxis { get; protected set; }
        public abstract GraphAxis YAxis { get; protected set; }

        public abstract IList<GraphSeries> Series { get; protected set; }

        public GraphStyle? Style { get; set; }

        public abstract void Add( GraphSeries series );

        public abstract void Render( DrawingContext dc, Rect destination );
    }

    public class GraphLinearXAxis: GraphAxis
    {
        public override IEnumerable<double> Transform( GraphSeries series, GraphRange destination )
        {
            if ( AutoRange )
            {
                Range = new GraphRange {
                    Min = series.Values.Min( p => p.X ),
                    Max = series.Values.Max( p => p.X )
                };
            }

            var scale = ( destination.Max - destination.Min ) / ( Range.Max - Range.Min );
            return series.Values.Select( v => ( v.X - Range.Min ) * scale );
        }

        public override void Render( DrawingContext dc, GraphScale scale )
        {
        }
    }

    public class GraphLinearYAxis: GraphAxis
    {
        public override IEnumerable<double> Transform( GraphSeries series, GraphRange destination )
        {
            if ( AutoRange )
            {
                Range = new GraphRange {
                    Min = series.Values.Min( p => p.Y ),
                    Max = series.Values.Max( p => p.Y )
                };
            }

            var scale = ( destination.Max - destination.Min ) / ( Range.Max - Range.Min );
            return series.Values.Select( v => ( v.Y - Range.Min ) * scale );
        }

        public override void Render( DrawingContext dc, GraphScale scale )
        {
        }
    }

    public class GraphRectangularLinearScale : GraphScale
    {
        public override GraphAxis XAxis { get; protected set; } = new GraphLinearXAxis();
        public override GraphAxis YAxis { get; protected set; } = new GraphLinearYAxis();

        public override IList<GraphSeries> Series { get; protected set; } = new List<GraphSeries>();

        public override void Add( GraphSeries series )
        {
            Series.Add( series );
        }
        public override void Render( DrawingContext dc, Rect destination )
        {
            foreach( var one in Series )
            {
                var p = Enumerable.Zip(
                        XAxis.Transform( one, new GraphRange { Min = destination.Left, Max = destination.Right } ),
                        YAxis.Transform( one, new GraphRange { Min = destination.Top, Max = destination.Bottom } ),
                        ( x, y ) => new Point( x, y ) );

                Style.Render( dc, 
            }
        }
    }

    public class Graph : Control
    {
        readonly List<GraphScale> Scales = new List<GraphScale>();

        public Graph()
        {
            Scales.Add( new GraphRectangularLinearScale() );
            Scales[0].Add( new GraphSeries
                {
                    Values = new List<Point>( Enumerable.Range( 1, 100 )
                            .Select( i => new Point( i, Math.Sin( i / 50.0 ) ) ) )
                } );
        }

        private void InitializeComponent()
        {
        }

        public override void Render( DrawingContext dc )
        {
            dc.DrawLine( new Pen( Brushes.Yellow ), Bounds.TopLeft, Bounds.BottomRight );
            Scales.ForEach( s => s.Render( dc, Bounds ) );
        }
    }
}