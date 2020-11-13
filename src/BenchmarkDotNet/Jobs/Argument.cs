using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public abstract class Argument
    {
        [PublicAPI] public string TextRepresentation { get; protected set; }

        // CharacteristicPresenters call ToString(), this is why we need this override
        public override string ToString() => TextRepresentation;
    }

    /// <summary>
    /// Argument passed directly to mono when executing benchmarks (mono [options])
    /// example: new MonoArgument("--gc=sgen")
    /// </summary>
    public class MonoArgument : Argument
    {
        public MonoArgument(string value)
        {
            if (value == "--llvm" || value == "--nollvm")
                throw new NotSupportedException("Please use job.Env.Jit to specify Jit in explicit way");

            TextRepresentation = value;
        }
    }

    /// <summary>
    /// Argument passed to dotnet cli when restoring and building the project
    /// example: new MsBuildArgument("/p:MyCustomSetting=123")
    /// </summary>
    [PublicAPI]
    public class MsBuildArgument : Argument
    {

        public MsBuildArgument(string value) => TextRepresentation = EscapeBuildArguments(value);

        /// <summary>
        /// Escapes the \ with \\ when \ is part of the build argument.
        /// <see href="https://github.com/dotnet/BenchmarkDotNet/issues/1536">Fixes issue 1536</see>
        /// </summary>
        internal string EscapeBuildArguments(string args)
        {
            if (args.Contains("\\"))
                args = args.Replace("\\", "\\\\");
            return args;
        }
    }
}