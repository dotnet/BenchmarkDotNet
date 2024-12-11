using BenchmarkDotNet.Reports;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Diagnosers
{
    public class DescriptorConfigInjector<TConfig>
    {
        protected TConfig? Config { get; set; }
        public DescriptorConfigInjector(TConfig config)
        {
            Config = config;
        }
    }
}
