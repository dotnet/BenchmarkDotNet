// #define FAKE_RAPL

using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Diagnosers
{
    internal static class EnergyCounterDiscovery 
    {
        public static IEnumerable<EnergyCounter> Discover(EnergyCountersSetup setup)
        {
#if FAKE_RAPL
            return Filter(GetFake(), setup);
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Filter(DiscoverLinux(), setup);
            else
                throw new NotImplementedException(string.Format("RAPL support for {0} is not implemented yet", RuntimeInformation.OSDescription));
#endif
        }

        private static IEnumerable<EnergyCounter> Filter(IEnumerable<EnergyCounter> counters, EnergyCountersSetup setup)
        {
            switch (setup)
            {
                case EnergyCountersSetup.All:
                    return counters;

                case EnergyCountersSetup.Default:
                    return counters.Where(c => c.Name == "core");

                default:
                    throw new NotImplementedException();
            }
        }

        private static IEnumerable<EnergyCounter> DiscoverLinux()
        {
            const int MAX_PACKAGES = int.MaxValue;
            const int MAX_PACKAGE_UNITS = int.MaxValue;            

            for (int i = 0; i < MAX_PACKAGES; i++)
            {
                string path = $"/sys/class/powercap/intel-rapl/intel-rapl:{i}";
                if (LinuxEnergyCounter.IsValid(path))
                {
                    yield return LinuxEnergyCounter.FromPath(path);

                    for (int j = 0; j < MAX_PACKAGE_UNITS; j++)
                    {
                        path = $"/sys/class/powercap/intel-rapl/intel-rapl:{i}/intel-rapl:{i}:{j}";
                        if (LinuxEnergyCounter.IsValid(path))
                            yield return LinuxEnergyCounter.FromPath(path);
                        else
                            break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

#if FAKE_RAPL
        private static IEnumerable<EnergyCounter> GetFake()
        {
            yield return new FakeEnergyCounter("package-0", 17995193, "intel-rapl:0");
            yield return new FakeEnergyCounter("core", 9955052, "intel-rapl:0/intel-rapl:0:0");
            yield return new FakeEnergyCounter("dram", 1858455, "intel-rapl:0/intel-rapl:0:1");
            yield return new FakeEnergyCounter("uncore", 773924, "intel-rapl:0/intel-rapl:0:2");
        }

        private class FakeEnergyCounter : EnergyCounter
        {
            private long _value;

            public FakeEnergyCounter(string name, long value, string id) : base(name, id) {
                _value = value;
            }

            public override (bool, string) TestRead() => (true, string.Empty);

            public override void FixStart() {}

            public override void FixFinish() {}

            public override long GetValue() => _value;
        }
#endif
    }
}
