
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PerfMonLib
{
    public class PerfMonConfig
    {
        public TimeSpan SamplingRate { get; set; } = TimeSpan.FromSeconds( 5 );
        public TimeSpan PresentationRate { get; set; } = TimeSpan.FromSeconds( 30 );
        public int HistoryLength { get; set; } = 200;
        public string NetworkInterface { get; set; } = "";
    }

    public static class PerfMonContext
    {
        public static PerfMonConfig Config = new PerfMonConfig();
        public static TimeSpan SamplingRate { get => Config.SamplingRate; set => Config.SamplingRate = value; }
        public static TimeSpan PresentationRate { get => Config.PresentationRate; set => Config.PresentationRate = value; }
        public static int HistoryLength { get => Config.HistoryLength; set => Config.HistoryLength = value; }
        public static string NetworkInterface { get => Config.NetworkInterface; set => Config.NetworkInterface = value; }
        
        static (string name, Type t)[] GetSources()
        {
            var sources = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany( a => a.GetTypes()
                                .Where( t => t.IsDefined( 
                                                typeof( PerfMonSourceAttribute ),
                                                false )
                                             && typeof( IPerfMonSource ).IsAssignableFrom( t ) ) );

            return sources
                    .Select( s => ( s.GetCustomAttribute<PerfMonSourceAttribute>( false )!.Name, s ) )
                    .ToArray();
        }

        static (string name, Type t)[] GetMetrics()
        {
            var sources = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany( a => a.GetTypes()
                                .Where( t => t.IsDefined( 
                                                typeof( PerfMonMetricAttribute ),
                                                false )
                                             && typeof( IPerfMonMetric ).IsAssignableFrom( t ) ) );

            return sources
                    .Select( s => ( s.GetCustomAttribute<PerfMonMetricAttribute>( false )!.Name, s ) )
                    .ToArray();
        }

        class NameAndType
        {
            public string Name { get; }
            public Type Type { get; }
            public object? Instance { get; set; }

            public NameAndType( string name, Type type )
            {
                Name = name;
                Type = type;
            }
        }

        static Dictionary<string,NameAndType> Sources;
        static Dictionary<string,NameAndType> Metrics;

        static PerfMonContext()
        {
            Sources = GetSources().ToDictionary( s => s.name, s => new NameAndType( s.name, s.t ) );
            Metrics = GetMetrics().ToDictionary( s => s.name, s => new NameAndType( s.name, s.t ) );
        }

        public static IPerfMonSource? GetSource( string name )
        {
            lock( Sources )
            {
                if ( Sources.TryGetValue( name, out var s ) )
                {
                    if ( s.Instance is null )
                    {
                        s.Instance = (IPerfMonSource?)Activator.CreateInstance( s.Type, true );
                    }
                    return (IPerfMonSource?)s.Instance;
                }

                return null;
            }
        }

        public static IPerfMonMetric? GetMetric( string name )
        {
            lock( Metrics )
            {
                if ( Metrics.TryGetValue( name, out var s ) )
                {
                    if ( s.Instance is null )
                    {
                        s.Instance = (IPerfMonMetric?)Activator.CreateInstance( s.Type, true );
                    }
                    return (IPerfMonMetric?)s.Instance;
                }

                return null;
            }
        }

    }
}
