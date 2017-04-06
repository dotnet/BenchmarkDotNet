using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Jobs
{
    public sealed class InfrastructureMode : JobMode<InfrastructureMode>
    {
        public static readonly Characteristic<IToolchain> ToolchainCharacteristic = Characteristic.Create((InfrastructureMode i) => i.Toolchain);
        public static readonly Characteristic<IClock> ClockCharacteristic = Characteristic.Create((InfrastructureMode i) => i.Clock);
        public static readonly Characteristic<IEngineFactory> EngineFactoryCharacteristic = Characteristic.Create((InfrastructureMode i) => i.EngineFactory);

        public IToolchain Toolchain
        {
            get { return ToolchainCharacteristic[this]; }
            set { ToolchainCharacteristic[this] = value; }
        }

        public IClock Clock
        {
            get { return ClockCharacteristic[this]; }
            set { ClockCharacteristic[this] = value; }
        }

        /// <summary>
        /// this type will be used in the auto-generated program to create engine in separate process
        /// <remarks>it must have parameterless constructor</remarks>
        /// </summary>
        public IEngineFactory EngineFactory
        {
            get { return EngineFactoryCharacteristic[this]; }
            set { EngineFactoryCharacteristic[this] = value; }
        }
    }
}