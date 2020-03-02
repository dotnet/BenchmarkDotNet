using BenchmarkDotNet.Characteristics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public class MetaMode : JobMode<MetaMode>
    {
        [PublicAPI] public static readonly Characteristic<bool> BaselineCharacteristic = CreateHiddenCharacteristic<bool>(nameof(Baseline));
        [PublicAPI] public static readonly Characteristic<bool> IsMutatorCharacteristic = CreateIgnoreOnApplyCharacteristic<bool>(nameof(IsMutator));
        [PublicAPI] public static readonly Characteristic<bool> IsDefaultCharacteristic = CreateHiddenCharacteristic<bool>(nameof(IsDefault));

        public bool Baseline
        {
            get => BaselineCharacteristic[this];
            set => BaselineCharacteristic[this] = value;
        }

        /// <summary>
        /// mutator job should not be added to the config, but instead applied to other jobs in given config
        /// </summary>
        public bool IsMutator
        {
            get => IsMutatorCharacteristic[this];
            set => IsMutatorCharacteristic[this] = value;
        }

        /// <summary>
        /// set to true if you want to specify custom default settings for default job used by console arguments parser
        /// </summary>
        public bool IsDefault
        {
            get => IsDefaultCharacteristic[this];
            set => IsDefaultCharacteristic[this] = value;
        }
    }
}