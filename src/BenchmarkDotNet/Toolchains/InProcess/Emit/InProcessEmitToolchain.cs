using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    [PublicAPI]
    public class InProcessEmitToolchain : Toolchain
    {
        /// <summary>The default toolchain instance.</summary>
        public static readonly IToolchain Instance = new InProcessEmitToolchain(true);

        /// <summary>The toolchain instance without output logging.</summary>
        public static readonly IToolchain DontLogOutput = new InProcessEmitToolchain(false);

        /// <summary>Initializes a new instance of the <see cref="InProcessEmitToolchain" /> class.</summary>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessEmitToolchain(bool logOutput) :
            this(TimeSpan.Zero, logOutput)
        { }

        /// <summary>Initializes a new instance of the <see cref="InProcessEmitToolchain" /> class.</summary>
        /// <param name="timeout">Timeout for the run.</param>
        /// <param name="logOutput"><c>true</c> if the output should be logged.</param>
        public InProcessEmitToolchain(TimeSpan timeout, bool logOutput) :
            base(
                nameof(InProcessEmitToolchain),
                new InProcessEmitGenerator(),
                new InProcessEmitBuilder(),
                new InProcessEmitExecutor(timeout, logOutput))
        {
        }

        public override bool IsInProcess => true;
    }
}