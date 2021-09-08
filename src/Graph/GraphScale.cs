using Avalonia;
using System.Collections.Generic;
using Avalonia.Media;

namespace Graph
{
    public abstract class GraphScale
    {
        public abstract GraphYAxis YAxis { get; protected set; }

        public abstract IList<GraphSeries> Series { get; protected set; }

        public GraphStyle? Style { get; set; }

        public abstract void Add( GraphSeries series );

        public abstract void Render( DrawingContext dc, GraphXAxis xaxis, Rect destination );
    }
}