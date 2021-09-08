using Avalonia;
using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace Graph
{
    public enum YAxisAlignment { Left, Right }

    public abstract class GraphYAxis
    {
        public GraphRange Range { get; set; } = new GraphRange { Min = 0, Max = 100.0 };
        public YAxisAlignment Alignment { get; set; } = YAxisAlignment.Left;

        public bool IsVisible { get; set; } = true;
        public bool AutoRange { get; set; } = true;
        public GraphRange PlotRangeMargins { get; set; }

        public abstract IEnumerable<GraphValue> Transform( GraphSeries series, GraphRange destination );
        public abstract void Render( DrawingContext dc, GraphScale scale, Rect destination );

        public static ScaleSubdivision CreateScaleSubdivision(
            double space,
            GraphRange range,
            double spacing
        )
        {
            var targetsubdiv = (int)( space / spacing );
            var subdivisions = targetsubdiv switch
            {
                >= 10 => targetsubdiv - targetsubdiv % 5,
                >= 5 => 5,
                >= 2 => 2,
                _ => 1
            };

            var expmax = Math.Floor( Math.Log10( range.Max ) );

            var delta = range.Max - range.Min;
            var deltamax = Math.Floor( Math.Log10( delta ) );
            var deltafactor = Math.Pow( 10, deltamax );

            var scalemax = ( Math.Ceiling( range.Max / deltafactor ) ) * deltafactor;
            var scalemin = ( Math.Floor( range.Min / deltafactor ) ) * deltafactor;

            var ds = scalemax - scalemin;
            var s = ds / subdivisions;
            var sf = Math.Floor( Math.Log10( s ) );
            var sfactor = Math.Pow( 10, sf );

            var snap = Math.Floor( s / sfactor );
            var step = snap switch 
            {
                >= 5 => 5,
                >= 2 => 2,
                _ => 1
            } * sfactor;

            return new ScaleSubdivision
            {
                ScaleMin = scalemin,
                ScaleMax = scalemax,
                Step = step,
            };
        }
    }

    public abstract class GraphXAxis
    {
        public GraphRange Range { get; set; } = new GraphRange { Min = 0, Max = 100.0 };

        public bool IsVisible { get; set; } = true;
        public bool AutoRange { get; set; } = true;
        
        public GraphRange PlotRangeMargins { get; set; }

        public abstract IEnumerable<GraphValue> Transform( GraphSeries series, GraphRange destination );
        public abstract void Render( DrawingContext dc, IEnumerable<GraphScale> scales, Rect destination );
    }
}