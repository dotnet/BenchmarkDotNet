using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Diagnosers
{
    /// <summary>
    /// Energy counters setup
    /// </summary>
    public enum EnergyCountersSetup
    {
        /// <summary>
        /// Default setup (core only)
        /// </summary>
        Default = 0,

        /// <summary>
        /// All discovered counters
        /// </summary>
        All = 1,
    }
}
