using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;

#nullable enable

namespace BenchmarkDotNet.Jobs
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class InfrastructureMode : JobMode<InfrastructureMode>
    {
        public const string ReleaseConfigurationName = "Release";

        public static readonly Characteristic<IToolchain> ToolchainCharacteristic = CreateCharacteristic<IToolchain>(nameof(Toolchain));
        public static readonly Characteristic<IClock> ClockCharacteristic = CreateCharacteristic<IClock>(nameof(Clock));
        public static readonly Characteristic<IEngineFactory> EngineFactoryCharacteristic = CreateCharacteristic<IEngineFactory>(nameof(EngineFactory));
        public static readonly Characteristic<string> BuildConfigurationCharacteristic = CreateCharacteristic<string>(nameof(BuildConfiguration));
        public static readonly Characteristic<IReadOnlyList<Argument>> ArgumentsCharacteristic = CreateCharacteristic<IReadOnlyList<Argument>>(nameof(Arguments));

        public static readonly InfrastructureMode InProcess = new InfrastructureMode(InProcessEmitToolchain.Default);

        public InfrastructureMode() { }

        private InfrastructureMode(IToolchain toolchain)
        {
            Toolchain = toolchain;
        }

        public IToolchain? Toolchain
        {
            get { return ToolchainCharacteristic[this]; }
            set { ToolchainCharacteristic[this] = value; }
        }

        public IClock? Clock
        {
            get { return ClockCharacteristic[this]; }
            set { ClockCharacteristic[this] = value; }
        }

        /// <summary>
        /// this type will be used in the auto-generated program to create engine in separate process
        /// <remarks>it must have parameterless constructor</remarks>
        /// </summary>
        public IEngineFactory? EngineFactory
        {
            get { return EngineFactoryCharacteristic[this]; }
            set { EngineFactoryCharacteristic[this] = value; }
        }

        public string? BuildConfiguration
        {
            get => BuildConfigurationCharacteristic[this];
            set => BuildConfigurationCharacteristic[this] = value;
        }

        public IReadOnlyList<Argument>? Arguments
        {
            get => ArgumentsCharacteristic[this];
            set => ArgumentsCharacteristic[this] = value;
        }

        public bool TryGetToolchain([NotNullWhen(true)]out IToolchain? toolchain)
        {
            toolchain = HasValue(ToolchainCharacteristic) ? Toolchain : default;
            return toolchain != default;
        }
    }
}