using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CoreRt
{
    public class Publisher : DotNetCliPublisher
    {
        public Publisher(string customDotNetCliPath, string ilcPath, bool useCppCodeGenerator, string runtimeIdentifier) : base(customDotNetCliPath)
        {
            IlcPath = ilcPath;
            UseCppCodeGenerator = useCppCodeGenerator;
            RuntimeIdentifier = runtimeIdentifier;
        }
        
        private string IlcPath { get; }
        private bool UseCppCodeGenerator { get; }
        private string RuntimeIdentifier { get; }

        protected override string GetExtraArguments(BuildPartition buildPartition)
        {
            string extraArguments = UseCppCodeGenerator ? $"-r {RuntimeIdentifier} /p:NativeCodeGen=cpp" : $"-r {RuntimeIdentifier}";

            return $"{extraArguments} {base.GetExtraArguments(buildPartition)}";
        }

        // https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built
        // we have to pass IlcPath env var to get it working
        protected override IReadOnlyList<EnvironmentVariable> GetEnvironmentVariables()
            => IlcPath == null
                ? Array.Empty<EnvironmentVariable>()
                : new[] { new EnvironmentVariable("IlcPath", IlcPath) };
    }
}
