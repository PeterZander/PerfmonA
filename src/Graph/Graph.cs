using Avalonia;
using Avalonia.Controls;
using System;
using System.Linq;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Graph
{
    public class Graph : Control
    {
        public GraphXAxis XAxis { get; set; } = new GraphLinearXAxis();
        public bool IsLegendVisible { get; set; } = true;

        ObservableCollection<GraphScale> ScalesField = new ObservableCollection<GraphScale>();

        public static readonly DirectProperty<Graph, ObservableCollection<GraphScale>> ScalesProperty =
            AvaloniaProperty.RegisterDirect<Graph, ObservableCollection<GraphScale>>(
                nameof( Scales ),
                o => o.Scales,
                ( o, v ) => o.Scales = v );

        public ObservableCollection<GraphScale> Scales
        {
            get { return ScalesField; }
            set
            {
                if ( !SetAndRaise( ScalesProperty, ref ScalesField, value ) )
                {
                    // Any change affects the entire surface
                    RaisePropertyChanged( ScalesProperty, ScalesField, value );
                }
            }
        }

        static Graph()
        {
            AffectsRender<Graph>(
                ScalesProperty );
        }

        public void RenderLegend( DrawingContext dc, GraphScale[]? scar )
        {
            if ( scar is null ) 
                return;

            var fontheight = 12;

            var legendinfo = scar!
                .SelectMany( sc => sc.Series
                    .Select( s => new 
                    {
                        Series = s,
                        Text = new FormattedText(
                                    $"{s.Title}",
                                    Typeface.Default,
                                    fontheight,
                                    TextAlignment.Left,
                                    TextWrapping.NoWrap,
                                    Size.Infinity ),
                    } )
                    .Select( snt => new
                    {
                        snt.Series.Title,
                        snt.Text,
                        Series = snt.Series,
                        Background = snt.Series.MinMaxFill,
                        Pen = snt.Series.MiddlePen,
                        TextBounds = snt.Text.Bounds,
                    } ) )
                .ToArray();

            var minsize = new Size( 300, 250 );

            if ( scar.Any() && Bounds.Width > minsize.Width && Bounds.Height > minsize.Height )
            {
                var p = new Point( Math.Max( 50, Bounds.Width / 10 ), 20 );
                var legendmargin = new Point( 10.0, 10.0 );
                var textpadding = new Point( 3, 3 );

                var maxtextwidth = legendinfo.Max( li => li.TextBounds.Width );

                var border = new Rect(
                    p.X,
                    p.Y,
                    maxtextwidth + 2 * legendmargin.X + 2 * textpadding.X,
                    legendinfo.Sum( li => li.TextBounds.Height * 1.1 + 2 * textpadding.Y ) + 2 * legendmargin.Y );

                dc.DrawRectangle( Brushes.Black, new Pen( Brushes.DarkGray, 2 ), border, 4, 4 );

                var ystart = p.Y + legendmargin.Y;
                foreach( var l in legendinfo.Reverse() )
                {
                    var boxdest = new Rect(
                            new Point( p.X + legendmargin.X, ystart ),
                            new Size( maxtextwidth + 2 * textpadding.X, l.TextBounds.Height + 2 * textpadding.Y ) ); 

                    var dest = new Rect( 
                            new Point( p.X + legendmargin.X + textpadding.X, ystart + textpadding.Y ), 
                            new Size( maxtextwidth, l.TextBounds.Height ) ); 

                    dc.DrawRectangle( new SolidColorBrush( l.Background ), l.Pen, boxdest ); 
                    dc.DrawText( l.Pen.Brush, dest.TopLeft, l.Text );

                    ystart += l.TextBounds.Height * 1.1 + textpadding.Y * 2;
                }
            }
        }

        public override void Render( DrawingContext dc )
        {
            if ( ScalesField == null || !ScalesField.Any() )
                return;
                
            try
            {
                var scar = ScalesField.ToArray();

                if ( IsLegendVisible ) RenderLegend( dc, scar );

                var b = new Rect( Bounds.Size );

                // Render surface
                XAxis.Render( dc, scar, b );

                foreach( var s in scar )
                {
                    s.Render( dc, XAxis, b );
                }
            }
            catch( Exception ex )
            {
                Debug.WriteLine( ex.ToString() );
            }
        }
    }
}