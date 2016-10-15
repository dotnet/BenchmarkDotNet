using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Jobs
{
    public sealed class InfrastructureMode
    {
        public static readonly InfrastructureMode Default = new InfrastructureMode();

        private InfrastructureMode()
        {
        }

        private static ICharacteristic<T> Create<T>(string id) => Characteristic<T>.Create("Infrastructure", id);

        public ICharacteristic<IToolchain> Toolchain { get; private set; } = Create<IToolchain>(nameof(Toolchain));
        public ICharacteristic<IClock> Clock { get; private set; } = Create<IClock>(nameof(Clock));

        /// <summary>
        /// this type will be used in the auto-generated program to create engine in separate process
        /// <remarks>it must have parameterless constructor</remarks>
        /// </summary>
        public ICharacteristic<IEngineFactory> EngineFactory { get; private set; } = Create<IEngineFactory>(nameof(EngineFactory));

        public static InfrastructureMode Parse(CharacteristicSet set)
        {
            var mode = new InfrastructureMode();
            mode.Toolchain = mode.Toolchain.Mutate(set);
            mode.Clock = mode.Clock.Mutate(set);
            mode.EngineFactory = mode.EngineFactory.Mutate(set);
            return mode;
        }

        public CharacteristicSet ToSet() => new CharacteristicSet(
            Toolchain,
            Clock,
            EngineFactory
        );
    }
}