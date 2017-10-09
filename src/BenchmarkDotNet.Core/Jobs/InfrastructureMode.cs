using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Jobs
{
    public sealed class InfrastructureMode : JobMode<InfrastructureMode>
    {
        public const string ReleaseConfigurationName = "Release";

        public static readonly Characteristic<IToolchain> ToolchainCharacteristic = Characteristic.Create((InfrastructureMode i) => i.Toolchain);
        public static readonly Characteristic<IClock> ClockCharacteristic = Characteristic.Create((InfrastructureMode i) => i.Clock);
        public static readonly Characteristic<IEngineFactory> EngineFactoryCharacteristic = Characteristic.Create((InfrastructureMode i) => i.EngineFactory);
        public static readonly Characteristic<string> BuildConfigurationCharacteristic = Characteristic.Create((InfrastructureMode i) => i.BuildConfiguration);
        public static readonly Characteristic<IReadOnlyList<EnvironmentVariable>> EnvironmentVariablesCharacteristic = Characteristic.Create((InfrastructureMode i) => i.EnvironmentVariables);
        public static readonly Characteristic<IReadOnlyList<Argument>> ArgumentsCharacteristic = Characteristic.Create((InfrastructureMode i) => i.Arguments);

        public static readonly InfrastructureMode InProcess = new InfrastructureMode(InProcessToolchain.Instance);
        public static readonly InfrastructureMode InProcessDontLogOutput = new InfrastructureMode(InProcessToolchain.DontLogOutput);

        public InfrastructureMode() { }

        private InfrastructureMode(IToolchain toolchain)
        {
            Toolchain = toolchain;
        }

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

        public string BuildConfiguration
        {
            get => BuildConfigurationCharacteristic[this];
            set => BuildConfigurationCharacteristic[this] = value;
        }

        public IReadOnlyList<EnvironmentVariable> EnvironmentVariables
        {
            get => EnvironmentVariablesCharacteristic[this];
            set => EnvironmentVariablesCharacteristic[this] = value;
        }

        public IReadOnlyList<Argument> Arguments
        {
            get => ArgumentsCharacteristic[this];
            set => ArgumentsCharacteristic[this] = value;
        }
    }
}