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
        public ICharacteristic<IEngine> Engine { get; private set; } = Create<IEngine>(nameof(Engine));

        public static InfrastructureMode Parse(CharacteristicSet set)
        {
            var mode = new InfrastructureMode();
            mode.Toolchain = mode.Toolchain.Mutate(set);
            mode.Clock = mode.Clock.Mutate(set);
            mode.Engine = mode.Engine.Mutate(set);
            return mode;
        }

        public CharacteristicSet ToSet() => new CharacteristicSet(
            Toolchain,
            Clock,
            Engine
        );
    }
}