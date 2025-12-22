using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Toolchains.CompositeR2R
{
    [PublicAPI]
    public class CompositeR2RToolchain : CsProjCoreToolchain, IEquatable<CompositeR2RToolchain>
    {
        private readonly string _customDotNetCliPath;
        private CompositeR2RToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor, customDotNetCliPath)
        {
            _customDotNetCliPath = customDotNetCliPath;
        }

        [PublicAPI]
        public static new IToolchain From(NetCoreAppSettings settings)
            => new CompositeR2RToolchain(settings.Name,
                new CompositeR2RGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.CustomRuntimePack, settings.AOTCompilerPath),
                new DotNetCliPublisher(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath),
                new Executor(),
                settings.CustomDotNetCliPath);

        public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
        {
            foreach (var validationError in DotNetSdkValidator.ValidateCoreSdks(_customDotNetCliPath, benchmarkCase))
            {
                yield return validationError;
            }
        }

        public override bool Equals(object obj) => obj is CompositeR2RToolchain typed && Equals(typed);

        public bool Equals(CompositeR2RToolchain other) => Generator.Equals(other.Generator);

        public override int GetHashCode() => Generator.GetHashCode();
    }
}
