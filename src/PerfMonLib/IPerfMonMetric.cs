using System;
using System.Collections.Generic;

namespace PerfMonLib
{
    public interface IPerfMonMetric
    {
        public string Name { get; }
        public event Action<IPerfMonMetric,DateTime>? Update;
        IDictionary<string,PerfMonValue>? Values { get; }
    }
}