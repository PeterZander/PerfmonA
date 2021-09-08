using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PerfmonA.Converters
{
    public class ControlHeightSubdividerConverter: IMultiValueConverter
    {

        public ControlHeightSubdividerConverter()
        {
        }

        public object Convert( IList<object> values, Type targetType, object parameter, CultureInfo culture )
        {
            if ( values.Count != 2 || !( values[0] is double ) || !( values[1] is int ) )
            {
                throw new NotImplementedException();
            }

            var itemcount = (int)values[1];
            var height = (double)values[0] - 1;
            var horizitems = Math.Ceiling( Math.Sqrt( itemcount ) );

            if ( horizitems < 0.01 ) horizitems = 1.0;
            
            return Math.Max( 5.0, height / horizitems );
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return value;
        }
    }
}