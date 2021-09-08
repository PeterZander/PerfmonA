using Avalonia;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Graph
{
    public class GraphRectangularLinearScale : GraphScale
    {
        public override GraphYAxis YAxis { get; protected set; } = new GraphLinearYAxis();

        public override IList<GraphSeries> Series { get; protected set; } = new List<GraphSeries>();

        public override void Add( GraphSeries series )
        {
            Series.Add( series );
        }
        public override void Render( DrawingContext dc, GraphXAxis xaxis, Rect dest )
        {
            YAxis.Render( dc, this, dest );

            var xrange = new GraphRange { Min = 0, Max = dest.Width };
            var yrange = new GraphRange { Min = 0, Max = dest.Height };

            foreach( var one in Series.ToArray() )
            {
                var p = Enumerable.Zip(
                        xaxis.Transform( one, xrange ),
                        YAxis.Transform( one, yrange ),
                        ( x, y ) => new GraphPosition
                                        {
                                            X = x,
                                            Y = y,
                                        } )
                        .ToArray();

                Style?.Render( dc, p, one, dest );
            }
        }

        public GraphRectangularLinearScale()
        {
            Style = new GraphLineStyle();
        }
    }
}