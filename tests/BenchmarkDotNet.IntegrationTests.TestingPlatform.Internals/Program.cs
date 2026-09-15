using BenchmarkDotNet.Running;
using BenchmarkDotNet.TestAdapter.TestingPlatform;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;
using System.Reflection;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// Drives the adapter's Microsoft.Testing.Platform types the way a real test host cannot, and prints what they
    /// did. See the README next to this file.
    /// </summary>
    internal static class Program
    {
        private static async Task<int> Main()
        {
            // First, so that the values it counts have not already been disposed by the requests below - they are
            // benchmarks of this assembly too, so those enumerate and dispose them as well.
            Console.WriteLine("== abandoned");
            Console.WriteLine(await ReportAbandonedRequestAsync().ConfigureAwait(false));

            Console.WriteLine("== handed");
            Console.WriteLine(await ReportHandedOverRequestAsync().ConfigureAwait(false));

            Console.WriteLine("== discover");
            foreach (var line in await ExecuteAsync(session => new DiscoverTestExecutionRequest(session, new UnrecognisedFilter())).ConfigureAwait(false))
                Console.WriteLine(line);

            Console.WriteLine("== run");
            foreach (var line in await ExecuteAsync(session => new RunTestExecutionRequest(session, new UnrecognisedFilter())).ConfigureAwait(false))
                Console.WriteLine(line);

            // An assembly that declares no benchmarks at all, so the refusal below has no node to travel on.
            Console.WriteLine("== empty");
            foreach (var line in await ExecuteAsync(
                session => new RunTestExecutionRequest(session, new UnrecognisedFilter()),
                typeof(BenchmarkDotNet.Attributes.BenchmarkAttribute).Assembly).ConfigureAwait(false))
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("== done");

            return 0;
        }

        /// <summary>
        /// Ends an application while a request is still in flight, which is what the client sending `exit` or an IDE
        /// cancelling looks like from in here, and reports what became of the values that request had enumerated.
        /// </summary>
        /// <remarks>
        /// Nothing else holds them: the request hands its values over by completing, and this one never does. Left
        /// undisposed they reach the finalizer, which is the dotnet/BenchmarkDotNet#1383 hang.
        /// </remarks>
        /// <returns>The counts, as a line.</returns>
        private static async Task<string> ReportAbandonedRequestAsync()
        {
            var lifetime = new ParameterValueLifetime();
            var request = lifetime.BeginRequest();

            request.Track(BenchmarkConverter.TypeToBenchmarks(typeof(AbandonedBenchmarks)).BenchmarksCases);

            // Deliberately no CompleteAsync: that is the point of this one.
            await lifetime.AfterRunAsync(0, CancellationToken.None).ConfigureAwait(false);

            return $"created={AbandonedBenchmarks.Abandoned.Created} disposed={AbandonedBenchmarks.Abandoned.Disposed}";
        }

        /// <summary>
        /// Ends an application the same way, but while the abandoned request had already handed its benchmarks to
        /// BenchmarkDotNet and BenchmarkDotNet had entered the run stage it disposes them from.
        /// </summary>
        /// <remarks>
        /// They are its to dispose at that point, whether the run completes, throws or is cancelled. Disposing them
        /// here too would either be a second disposal or - in the window this stands for, an IDE cancelling a run in
        /// progress - the disposal of a value a benchmark is still using, which reaches the user as an
        /// ObjectDisposedException thrown from inside their own benchmark.
        /// </remarks>
        /// <returns>The counts, as a line.</returns>
        private static async Task<string> ReportHandedOverRequestAsync()
        {
            var lifetime = new ParameterValueLifetime();
            var cases = BenchmarkConverter.TypeToBenchmarks(typeof(HandedOverBenchmarks)).BenchmarksCases;

            // Under server mode a discovery comes first and completes, leaving these very values held - so they are
            // reachable from the held set as well as from the request still running them, and neither may take them.
            var discovery = lifetime.BeginRequest();
            discovery.Track(cases);
            await discovery.CompleteAsync([]).ConfigureAwait(false);

            var request = lifetime.BeginRequest();
            request.Track(cases);
            request.HandOver(cases, () => true);

            // Deliberately no CompleteAsync, as above: this request is abandoned too.
            await lifetime.AfterRunAsync(0, CancellationToken.None).ConfigureAwait(false);

            return $"created={HandedOverBenchmarks.HandedOver.Created} disposed={HandedOverBenchmarks.HandedOver.Disposed}";
        }

        private static async Task<IReadOnlyList<string>> ExecuteAsync(
            Func<Microsoft.Testing.Platform.TestHost.TestSessionContext, IRequest> createRequest,
            Assembly? assembly = null)
        {
            var platform = new PlatformStub();
            var lifetime = new ParameterValueLifetime();
            var framework = new BenchmarkTestFramework(
                new TestFrameworkCapabilities(),
                PlatformStub.CreateServiceProvider(platform),
                assembly ?? Assembly.GetExecutingAssembly(),
                lifetime);

            var request = createRequest(PlatformStub.CreateSessionContext());
#pragma warning disable TPEXP // Building a request context is still marked as experimental by the platform.
            var context = new ExecuteRequestContext(request, platform, platform, CancellationToken.None);
#pragma warning restore TPEXP

            // The framework is expected to deal with the filter rather than throw at the caller, which is the whole
            // point: an exception here escapes after the request has already been completed, so the platform reports
            // a finished request that said nothing about any benchmark.
            try
            {
                await framework.ExecuteRequestAsync(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return [.. platform.Messages, $"threw {exception.GetType().Name}: {exception.Message}"];
            }
            finally
            {
                await lifetime.AfterRunAsync(0, CancellationToken.None).ConfigureAwait(false);
            }

            return [.. platform.Messages, $"complete {platform.IsComplete}"];
        }

        /// <summary>
        /// A filter of a kind the adapter has never been told about, which is what a newer Microsoft.Testing.Platform
        /// or an extension registering its own filter factory would hand it.
        /// </summary>
        private sealed class UnrecognisedFilter : ITestExecutionFilter
        {
        }
    }
}
