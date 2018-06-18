using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public class MetaMode : JobMode<MetaMode>
    {
        public static readonly Characteristic<bool> IsBaselineCharacteristic = CreateCharacteristic<bool>(nameof(IsBaseline));
        public static readonly Characteristic<bool> IsMutatorCharacteristic = CreateCharacteristic<bool>(nameof(IsMutator));

        public bool IsBaseline
        {
            get => IsBaselineCharacteristic[this];
            set => IsBaselineCharacteristic[this] = value;
        }
        
        /// <summary>
        /// mutator job should not be added to the config, but instead applied to other jobs in given config
        /// </summary>
        public bool IsMutator
        {
            get => IsMutatorCharacteristic[this];
            set => IsMutatorCharacteristic[this] = value;
        }
    }
}