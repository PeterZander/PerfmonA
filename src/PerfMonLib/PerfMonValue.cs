using System;

namespace PerfMonLib
{
    public enum MessurementValueTypes
    {
        Tick,
        Percent,
        Time,
        TimeSpan,
        Count,
        Frequency,
        Temperature,
        Bitrate,
    }

    public class PerfMonValue: IConvertible
    {
        const long k = 1000L;
        const long M = k * 1000L;
        const long G = M * 1000L;
        const long T = G * 1000L;

        const long kB = 1024L;
        const long MB = kB * 1024L;
        const long GB = MB * 1024L;
        const long TB = GB * 1024L;

        public MessurementValueTypes Type { get; set; }

        public object Value { get; set; }

        public PerfMonValue( MessurementValueTypes type, object value )
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            switch ( Type )
            {
                case MessurementValueTypes.Bitrate when Value is double:
                    var v = (double)Value;
                    return v switch
                    {
                        >= T * 10 => $"{v / T:F0} Tbps",
                        >= G * 10 => $"{v / G:F0} Gbps",
                        >= M * 10 => $"{v / M:F0} Mbps",
                        >= k * 10 => $"{v / k:F0} kbps",
                        _ => $"{Value:F0} bps",
                    };

                case MessurementValueTypes.Percent when Value is double:
                    var v2 = (double)Value * 100.0;
                    return v2 switch
                    {
                        >= 10 => $"{v2:F0} %",
                        >= 1 => $"{v2:F1} %",
                        _ => $"{v2:F2} %",
                    };

                default:
                    return Value.ToString()!;
            }
        }

        public PerfMonValue FromDouble( double v )
        {
            return new PerfMonValue( Type, v );
        }

#region IConvertible
        static T ThrowNotSupported<T>()
        {
            return (T)ThrowNotSupported( typeof( T ) );
        }

        static object ThrowNotSupported( Type type )
        {
            throw new InvalidCastException( $"Converting type \"{typeof( PerfMonValue )}\" to type \"{type}\" is not supported." );
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean( IFormatProvider? provider ) => ThrowNotSupported<bool>();
        char IConvertible.ToChar( IFormatProvider? provider ) => ThrowNotSupported<char>();
        sbyte IConvertible.ToSByte( IFormatProvider? provider ) => ThrowNotSupported<sbyte>();
        byte IConvertible.ToByte( IFormatProvider? provider ) => ThrowNotSupported<byte>();
        short IConvertible.ToInt16( IFormatProvider? provider ) => ThrowNotSupported<short>();
        ushort IConvertible.ToUInt16( IFormatProvider? provider ) => ThrowNotSupported<ushort>();
        int IConvertible.ToInt32( IFormatProvider? provider ) => ThrowNotSupported<int>();
        uint IConvertible.ToUInt32( IFormatProvider? provider ) => ThrowNotSupported<uint>();
        ulong IConvertible.ToUInt64( IFormatProvider? provider ) => ThrowNotSupported<ulong>();
        float IConvertible.ToSingle( IFormatProvider? provider ) => ThrowNotSupported<float>();
        decimal IConvertible.ToDecimal( IFormatProvider? provider ) => ThrowNotSupported<decimal>();
        DateTime IConvertible.ToDateTime( IFormatProvider? provider ) => ThrowNotSupported<DateTime>();

        string IConvertible.ToString( IFormatProvider? provider )
        {
            return ToString();
        }

        double IConvertible.ToDouble( IFormatProvider? provider )
        {
            switch ( Type )
            {
                case MessurementValueTypes.Count:
                    return Convert.ToDouble( Value );

                case MessurementValueTypes.Bitrate:
                    return (double)Value;

                case MessurementValueTypes.Time:
                    return Convert.ToDouble( ( (DateTime)Value ).ToFileTime() / 1E6 );

                case MessurementValueTypes.Percent:
                    return Convert.ToDouble( Value );

                default:
                    throw new NotImplementedException();
            }
        }

        long IConvertible.ToInt64( IFormatProvider? provider )
        {
            switch ( Type )
            {
                case MessurementValueTypes.Tick:
                    return Convert.ToInt64( Value );

                case MessurementValueTypes.Count:
                    return Convert.ToInt64( Value );

                default:
                    throw new NotImplementedException();
            }
        }

        object IConvertible.ToType( Type convtype, IFormatProvider? provider )
        {
            if ( convtype == typeof( PerfMonValue ) )
            {
                return this;
            }

            return ThrowNotSupported( convtype );
        }   

#endregion
    }
}