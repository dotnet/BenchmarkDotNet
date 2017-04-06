using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Jobs
{
    public class DiagnoserMode : JobMode<DiagnoserMode>
    {
        public static readonly Characteristic<IReadOnlyCollection<HardwareCounter>> HardwareCountersCharacteristic = Characteristic.Create((DiagnoserMode a) => a.HardwareCounters);

        public IReadOnlyCollection<HardwareCounter> HardwareCounters
        {
            get { return HardwareCountersCharacteristic[this]; }
            set { HardwareCountersCharacteristic[this] = value; }
        }
    }
}