using System;

namespace BenchmarkDotNet.Jobs
{
    public interface IArgument
    {
        string TextRepresentation { get; }
    }

    /// <summary>
    /// Argument passed directly to mono when executing benchmarks (mono [options])
    /// example: new MonoArgument("--gc=sgen")
    /// </summary>
    public class MonoArgument : IArgument
    {
        public MonoArgument(string value)
        {
            if(value == "--llvm" || value == "--nollvm")
                throw new NotSupportedException("Please use job.Env.Jit to specify Jit in explicit way");

            TextRepresentation = value;
        }

        public string TextRepresentation { get; }
    }

    /// <summary>
    /// Argument passed to dotnet cli when restoring and building the project
    /// example: new MsBuildArgument("/p:MyCustomSetting=123")
    /// </summary>
    public class MsBuildArgument : IArgument
    {
        public MsBuildArgument(string value) => TextRepresentation = value;

        public string TextRepresentation { get; }
    }
}