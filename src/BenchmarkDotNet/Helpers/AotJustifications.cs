namespace BenchmarkDotNet.Helpers
{
    /// <summary>
    /// The reasons given where trimming or Native AOT warnings are suppressed, shared by the places they apply to.
    /// </summary>
    internal static class AotJustifications
    {
        internal const string DiscoveredBenchmarkTypes =
            "Benchmark types found in an assembly or passed in a collection cannot carry annotations. In a trimmed or Native AOT"
            + " application, BenchmarkDotNet's source generator (NativeAotRegistrationGenerator) keeps them with all their members"
            + " and constructs the closed generic ones.";

        internal const string AssemblyLocation =
            "The location is only used to build out of process and to tell builds apart. It is empty in a single-file or Native AOT"
            + " application, which runs its benchmarks in-process.";

        internal const string ReturnTypeShape =
            "Recognizes the shape of a benchmark's return type (awaitable, async enumerable). The framework types BenchmarkDotNet"
            + " supports keep these members; a custom awaitable or async enumerable is not supported in a Native AOT application,"
            + " where NativeAotHostValidator reports it.";

        internal const string DynamicCodeOnly =
            "Only reached where code can be generated: in a Native AOT application AotBenchmarkActions creates the action instead,"
            + " and NativeAotHostValidator reports a result type it cannot handle before anything runs.";

        internal const string EmitToolchain =
            "Part of InProcessEmitToolchain, which only runs where code can be generated: BenchmarkDotNet picks InProcessNoEmitToolchain"
            + " otherwise (BenchmarkCase.GetToolchain, [InProcess]), and NativeAotHostValidator rejects it in a Native AOT application.";

        internal const string HostOnly =
            "Only used by the regular (JIT) host process: generating, building and analyzing benchmark projects, or diagnosers that"
            + " attach to another process. A Native AOT application runs its benchmarks in-process and never reaches it.";
    }
}
