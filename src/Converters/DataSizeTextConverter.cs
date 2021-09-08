using System;
using System.Globalization;
using Avalonia.Data.Converters;
using System.Text.RegularExpressions;
using Avalonia.Data;

namespace PerfmonA.Converters
{
    public class DataSizeTextConverter: IValueConverter
    {
        Regex Pattern;

        const long kB = 1024L;
        const long MB = kB * 1024L;
        const long GB = MB * 1024L;
        const long TB = GB * 1024L;

        public DataSizeTextConverter()
        {
            Pattern = new Regex( @"^\s*([0-9]+)\s*([kmgt]b?)?$",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase );
        }

        public object? Convert( object value, Type targettype, object parameter,
            System.Globalization.CultureInfo culture )
        {
            if ( value is null )
                throw new ArgumentNullException();

            var l = System.Convert.ToInt64( value );

            if ( l > TB ) return $"{l/TB}TB";
            if ( l > GB ) return $"{l/GB}GB";
            if ( l > MB ) return $"{l/MB}MB";
            if ( l > kB ) return $"{l/kB}kB";

            return value.ToString();
        }

        public object? ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is null ) return 0L;

            string v = ( value as string )!;
            if ( string.IsNullOrWhiteSpace( v ) ) return 0L;

            var matches = Pattern.Matches( v );
            
            if ( matches.Count == 0 )
            {
                return new BindingNotification(
                    new InvalidCastException( $"Unable to convert {v} to bytes." ),
                    BindingErrorType.Error );
            };

            var scale = 1L;
            var valstr = matches[0].Groups[1].Value;
            var scalestr = matches[0].Groups[2].Value.ToLower();

            if ( !string.IsNullOrWhiteSpace( scalestr ) )
            {
                switch ( scalestr[0] )
                {
                    case 'k': scale = kB; break;
                    case 'm': scale = MB; break;
                    case 'g': scale = GB; break;
                    case 't': scale = TB; break;
                }
            }

            return System.Convert.ToInt64( valstr ) * scale;
        }
    }
}