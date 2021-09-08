using Avalonia;
using System.Collections.Generic;
using Avalonia.Media;

namespace Graph
{
    public abstract class GraphStyle
    {
        public abstract void Render( DrawingContext dc, IEnumerable<GraphPosition> points, GraphSeries series, Rect destination );
    }
}