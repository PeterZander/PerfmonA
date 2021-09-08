using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Graph
{
    public class GraphLineStyle: GraphStyle
    {
        public override void Render( DrawingContext dc, IEnumerable<GraphPosition> points, GraphSeries series, Rect dest )
        {
            if ( !series.Intervals.Any() )
                return;

            var rangegeo = new PolylineGeometry(
                points
                    .Select( gp => new Point( gp.X.Middle, gp.Y.High ) )
                    .Concat( points.Select( gp => new Point( gp.X.Middle, gp.Y.Low ) ).Reverse() )
                , true );

            dc.DrawGeometry(
                    new SolidColorBrush( series.MinMaxFill, 0.3 ),
                    new Pen( new SolidColorBrush( series.MinMaxFill ), 0.3 ),
                    rangegeo );

            var sg = new StreamGeometry();
            using var sgc = sg.Open();

            var tangents = new List<Point>();
            var pointsar = points.ToArray();
            for( int i = 1; i < points.Count() - 1; ++i )
            {
                var p1 = new Point( pointsar[i - 1].X.Middle, pointsar[i - 1].Y.Middle );
                var p2 = new Point( pointsar[i].X.Middle, pointsar[i].Y.Middle );
                var p3 = new Point( pointsar[i + 1].X.Middle, pointsar[i + 1].Y.Middle );

                var newp = p2 - p1 + p3 - p2;
                tangents.Add( newp * 0.15 );
            }

            Point prevp = new Point( points.First().X.Middle, points.First().Y.Middle );
            sgc.BeginFigure( prevp, false );

            var tangentix = 0;
            var prevtangent = new Point( 0, 0 );
            var isfirst = true;

            foreach( var p in points.Skip( 1 ) )
            {
                //sgc.LineTo( p.Middle );

                var newp = new Point( p.X.Middle, p.Y.Middle );
                var tangent = tangentix < tangents.Count ? tangents[tangentix] : new Point( 0, 0 );

                if ( isfirst )
                {
                    var newt = newp - prevp;
                    newt = ( newt / ( new Vector( newt.X, newt.Y ) ).Length ) * ( new Vector( tangent.X, tangent.Y ) ).Length * 0.3;
                    prevtangent = newt;
                    isfirst = false;
                }

                sgc.CubicBezierTo( prevp + prevtangent, newp - tangent, newp );

                // Tangent
                //dc.DrawLine( new Pen( Brushes.Violet, 2 ), newp, newp + tangent * 3 );

                prevp = newp;
                prevtangent = tangent;
                ++tangentix;
            }
            sgc.EndFigure( false );

            dc.DrawGeometry(
                    Brushes.Transparent,
                    series.MiddlePen,
                    sg );


            if ( series.Intervals.Count > 0 ) 
            {
                var fontheight = 12;

                var last = series.Intervals.Last();
                var lastcoord = points.Last();

                var ft = new FormattedText(
                                $"{series.Intervals.LastOrDefault()?.Label}",
                                Typeface.Default,
                                fontheight,
                                TextAlignment.Right,
                                TextWrapping.NoWrap,
                                Size.Infinity );

                var textpos = new Point( lastcoord.X.Middle - ft.Bounds.Width - 10, lastcoord.Y.Middle - ft.Bounds.Height / 2 );
                var textsize = new Size( ft.Bounds.Width, ft.Bounds.Height );

                var boxpos = textpos + new Point( -5, -5 );
                var boxsize = textsize + new Size( 10, 10 );

                var edgedist = boxpos.Y + boxsize.Height - dest.Bottom;
                if ( edgedist > 0 )
                {
                    textpos = new Point( textpos.X, textpos.Y - edgedist - 5 );
                    boxpos = new Point( boxpos.X, boxpos.Y - edgedist - 5 );
                }

                dc.DrawRectangle(
                        new SolidColorBrush( series.MinMaxFill ),
                        new Pen( new SolidColorBrush( Colors.LightGray, 1 ) ),
                        new Rect( boxpos, boxsize ) );

                dc.DrawText(
                        series.MiddlePen.Brush,
                        textpos,
                        ft );
            }
        }
    }
}