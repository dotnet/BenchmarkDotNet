using Microsoft.Testing.Platform.Extensions;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// Identifies BenchmarkDotNet to Microsoft.Testing.Platform.
    /// </summary>
    /// <remarks>
    /// Several platform services, such as the tree node filter behind <c>--filter</c>, are registered on behalf of an
    /// extension rather than of the test framework itself, so the identity lives in its own type.
    /// </remarks>
    internal sealed class BenchmarkDotNetExtension : IExtension
    {
        /// <summary>
        /// The uid shared by every extension this package registers.
        /// </summary>
        public const string ExtensionUid = "BenchmarkDotNet.TestAdapter.TestingPlatform";

        /// <inheritdoc />
        public string Uid => ExtensionUid;

        /// <inheritdoc />
        public string Version => typeof(BenchmarkDotNetExtension).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        /// <inheritdoc />
        public string DisplayName => "BenchmarkDotNet";

        /// <inheritdoc />
        public string Description => "Runs BenchmarkDotNet benchmarks as tests.";

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
}
