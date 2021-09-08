using System;

namespace PerfMonLib
{
    public class PerfMonSourceAttribute: Attribute
    {
        public string Name { get; }

        public PerfMonSourceAttribute( string name )
        {
            Name = name;
        }
    }
}