using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Models;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Perfolizer.Horology;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Jobs
{
    public sealed class InfrastructureMode : JobMode<InfrastructureMode>
    {
        public const string ReleaseConfigurationName = "Release";

        public static readonly Characteristic<IToolchain> ToolchainCharacteristic = CreateCharacteristic<IToolchain>(nameof(Toolchain));
        public static readonly Characteristic<Runtime> RuntimeCharacteristic = CreateCharacteristic<Runtime>(nameof(Runtime));
        public static readonly Characteristic<IClock> ClockCharacteristic = CreateCharacteristic<IClock>(nameof(Clock));
        public static readonly Characteristic<IEngineFactory> EngineFactoryCharacteristic = CreateCharacteristic<IEngineFactory>(nameof(EngineFactory));
        public static readonly Characteristic<string> BuildConfigurationCharacteristic = CreateCharacteristic<string>(nameof(BuildConfiguration));
        public static readonly Characteristic<IReadOnlyList<Argument>> ArgumentsCharacteristic = CreateCharacteristic<IReadOnlyList<Argument>>(nameof(Arguments));

        public static readonly InfrastructureMode InProcess = new(RuntimeInformation.IsAot ? InProcessNoEmitToolchain.Default : InProcessEmitToolchain.Default);

        public InfrastructureMode() { }

        private InfrastructureMode(IToolchain toolchain)
        {
            // Go through the property so the coupled runtime characteristic is kept in sync with the toolchain.
            Toolchain = toolchain;
        }

        public IToolchain? Toolchain
        {
            get { return ToolchainCharacteristic[this]; }
            set
            {
                ToolchainCharacteristic[this] = value;
                // The toolchain and runtime are coupled, and the toolchain is the source of truth for the runtime.
                // Setting the toolchain overwrites the runtime with the one the toolchain targets.
                RuntimeCharacteristic[this] = value?.Runtime;
            }
        }

        /// <summary>
        /// Runtime
        /// </summary>
        public Runtime? Runtime
        {
            get { return RuntimeCharacteristic[this]; }
            set
            {
                // The toolchain and runtime are coupled. Setting the runtime clears any explicitly set toolchain,
                // which is instead derived from the runtime when the benchmark is built.
                if (!Equals(RuntimeCharacteristic[this], value))
                    ToolchainCharacteristic[this] = null;
                RuntimeCharacteristic[this] = value;
            }
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

        public bool TryGetToolchain([NotNullWhen(true)] out IToolchain? toolchain)
        {
            toolchain = HasValue(ToolchainCharacteristic) ? Toolchain : default;
            return toolchain != default;
        }

        internal BdnInfrastructure ToPerfonar() => new()
        {
            Runtime = HasValue(RuntimeCharacteristic) ? Runtime?.ToString() : null
        };
    }
}