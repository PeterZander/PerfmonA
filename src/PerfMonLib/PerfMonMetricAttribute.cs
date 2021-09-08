using System;

namespace PerfMonLib
{
    public class PerfMonMetricAttribute: Attribute
    {
        public string Name { get; }

        public PerfMonMetricAttribute( string name )
        {
            Name = name;
        }
    }
}