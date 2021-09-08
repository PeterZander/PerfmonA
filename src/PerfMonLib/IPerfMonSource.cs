using System;
using System.Collections.Generic;

namespace PerfMonLib
{
    public class PerfMonSourceCategory
    {
        public DateTime Time { get; set; }
        public readonly Dictionary<string,PerfMonValue> Values = new Dictionary<string, PerfMonValue>();
    }

    public class PerfMonSourceValues
    {
        public DateTime Time { get; set; }
        public readonly Dictionary<string,PerfMonSourceCategory> Categories = new Dictionary<string, PerfMonSourceCategory>();
    }

    public interface IPerfMonSource
    {
        public string Name { get; }
        public event Action<IPerfMonSource> Update;
        public PerfMonSourceValues? Values { get; }
        public void Terminate();
    }
}