using BenchmarkDotNet.Configs;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// determines if BenchmarDotNet changes power plan to High Performance
    /// </summary>
    [PublicAPI]
    public class HighPerformancePowerPlanAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public HighPerformancePowerPlanAttribute(bool value = true)
        {
            Config = ManualConfig.CreateEmpty().WithHighPerformancePowerPlan(value);
        }
    }
}
