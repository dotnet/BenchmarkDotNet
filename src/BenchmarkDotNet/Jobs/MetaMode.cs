using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public class MetaMode : JobMode<MetaMode>
    {
        public static readonly Characteristic<bool> IsBaselineCharacteristic = Characteristic.Create((MetaMode a) => a.IsBaseline);

        public bool IsBaseline
        {
            get => IsBaselineCharacteristic[this];
            set => IsBaselineCharacteristic[this] = value;
        }
    }
}