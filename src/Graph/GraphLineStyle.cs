using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Graph
{
    public class GraphLineStyle: GraphStyle
    {
        static readonly Point ZeroP = new Point( 0, 0 );
        static readonly Point FiveYP = new Point( 0, 5 );

        public static IEnumerable<IEnumerable<T>> Batch<T>( IEnumerable<T> source, int batchsize )
        {
            if ( batchsize <= 0 )
                throw new ArgumentOutOfRangeException( "batchsize", "Must be greater than zero." );

            using var enumerator = source.GetEnumerator();

            while( enumerator.MoveNext() )
            {
                int i = 0;

                IEnumerable<T> Batch()
                {
                    do yield return enumerator.Current;
                    while( ++i < batchsize && enumerator.MoveNext() );
                }

                yield return Batch();
                while( ++i < batchsize && enumerator.MoveNext() );
            }
        }

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

            var tangents = new List<(Point,Point)>();
            var pointsar = points.ToArray();
            var uselines = pointsar.Length > 500;

            if ( !uselines && pointsar.Length >= 3 )
            {
                var p1 = new Point( pointsar[0].X.Middle, pointsar[0].Y.Middle );
                var p2 = new Point( pointsar[1].X.Middle, pointsar[1].Y.Middle );

                for( int i = 1; i < points.Count() - 1; ++i )
                {
                    var rightp = pointsar[i + 1];
                    var p3 = new Point( rightp.X.Middle, rightp.Y.Middle );

                    var v1 = (Vector)( p2 - p1 );
                    var v2 = (Vector)( p3 - p2 );
                    Vector newv;

                    if ( p2.Y < p1.Y && p2.Y < p3.Y
                        || p2.Y > p1.Y && p2.Y > p3.Y )
                    {
                        newv = new Vector( 1.0, 0.0 );
                    }
                    else
                    {
                        newv = ( v1 + v2 ).Normalize();
                    }

                    var tscalef = 0.5 * v1.X;
                    var newp = (Point) newv * tscalef;

                    tangents.Add( ( -newp, newp ) );

                    p1 = p2;
                    p2 = p3;
                }
            }

            Point prevp = new Point( pointsar.First().X.Middle, pointsar.First().Y.Middle );

            var pointsbatch = Batch( pointsar.Skip( 1 ), 150 );
            
            var tangentix = 0;
            var prevtangent = ZeroP;

            foreach( var pb in pointsbatch )
            {
                var sg = new StreamGeometry();
                using ( var sgc = sg.Open() )
                {
                    sgc.BeginFigure( prevp, false );

                    foreach( var p in pb )
                    {
                        var newp = new Point( p.X.Middle, p.Y.Middle );

                        if ( uselines )
                        {
                            sgc.LineTo( newp );
                        }
                        else
                        {
                            var tangent = tangentix < tangents.Count ? tangents[tangentix] : ( ZeroP, ZeroP );
                            sgc.CubicBezierTo( prevp + prevtangent, newp + tangent.Item1, newp );

                            // Tangent
                            /*
                            dc.DrawLine( new Pen( Brushes.Khaki, 1 ), prevp, prevp + prevtangent * 3 );
                            dc.DrawLine( new Pen( Brushes.Violet, 1 ), newp, newp + tangent.Item1 * 3 );
                            dc.DrawLine( new Pen( Brushes.Gray, 1 ), newp - FiveYP, newp + FiveYP );
                            */

                            prevtangent = tangent.Item2;
                            ++tangentix;
                        }

                        prevp = newp;
                    }
                    sgc.EndFigure( false );
                }

                dc.DrawGeometry(
                        Brushes.Transparent,
                        series.MiddlePen,
                        sg );
            }


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