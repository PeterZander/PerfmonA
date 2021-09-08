using System;
using System.Collections.Generic;
using PerfMonLib;

namespace Graph
{
    public abstract class GraphSeriesInterval
    {
        public abstract GraphPosition Point { get; }
        public abstract string Label { get; }
    }
}