using System.Collections.Generic;
using PerfMonLib;
using Avalonia.Media;

namespace Graph
{
    public abstract class GraphSeries
    {
        public abstract string Title { get; set; }
        public List<GraphSeriesInterval> Intervals = new List<GraphSeriesInterval>();

        public abstract string XLabel( double x );
        public abstract string YLabel( double y );

        public Pen MiddlePen { get; set; } = new Pen( Brushes.Cyan, 2 );
        public Color MinMaxFill { get; set; } = Colors.DarkBlue;
    }
}