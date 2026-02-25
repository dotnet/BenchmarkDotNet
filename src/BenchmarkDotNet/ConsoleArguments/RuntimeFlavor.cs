namespace BenchmarkDotNet.Environments
{
    /// <summary>
    /// Specifies the .NET runtime flavor to use for WASM benchmarks.
    /// </summary>
    public enum RuntimeFlavor
    {
        /// <summary>Uses the Mono runtime pack (Microsoft.NETCore.App.Runtime.Mono.browser-wasm).</summary>
        Mono,

        /// <summary>Uses the CoreCLR runtime pack (Microsoft.NETCore.App.Runtime.browser-wasm).</summary>
        CoreCLR
    }
}
