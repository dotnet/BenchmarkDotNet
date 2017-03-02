using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.Uap;
using System.IO;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.Framework
{
    [UapJob("<device_portal_uri>", "<csrf_cookie>", "<wmid_cookie>", @"<BDN_UAP10.0_build output>")]
    public class Framework_DateTime
    {
        [Benchmark]
        public long UtcNowLatency()
        {
            return DateTime.UtcNow.Ticks;
        }

        [Benchmark]
        public long NowLatency()
        {
            return DateTime.Now.Ticks;
        }

        [Benchmark]
        public long UtcNowGranularity()
        {
            long lastTicks = DateTime.UtcNow.Ticks;
            while (DateTime.UtcNow.Ticks == lastTicks)
            {
            }
            return lastTicks;
        }

        [Benchmark]
        public long NowGranularity()
        {
            long lastTicks = DateTime.Now.Ticks;
            while (DateTime.Now.Ticks == lastTicks)
            {
            }
            return lastTicks;
        }
    }
}
