using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Jobs
{
    public sealed class InfraMode
    {
        public static readonly InfraMode Default = new InfraMode();

        private InfraMode()
        {
        }

        private static ICharacteristic<T> Create<T>(string id) => Characteristic<T>.Create("Infra", id);

        public ICharacteristic<IToolchain> Toolchain { get; private set; } = Create<IToolchain>(nameof(Toolchain));
        public ICharacteristic<IClock> Clock { get; private set; } = Create<IClock>(nameof(Clock));
        public ICharacteristic<IEngine> Engine { get; private set; } = Create<IEngine>(nameof(Engine));

        public static InfraMode Parse(CharacteristicSet set)
        {
            var mode = new InfraMode();
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