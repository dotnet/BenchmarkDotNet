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

            Console.WriteLine("== taken-back");
            Console.WriteLine(await ReportTakenBackRequestAsync().ConfigureAwait(false));

            Console.WriteLine("== already-disposed");
            Console.WriteLine(await ReportAlreadyDisposedValuesAsync().ConfigureAwait(false));

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
        /// BenchmarkDotNet - the run in flight that an IDE cancels, or that a server-mode client sending `exit` walks
        /// away from.
        /// </summary>
        /// <remarks>
        /// They are BenchmarkDotNet's to dispose from the hand-off until the run returns, so disposing them here too
        /// would either be a second disposal or the disposal of a value the run is using or about to use, which
        /// reaches the user as an ObjectDisposedException thrown from inside their own benchmark.
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
            request.HandOver(cases);

            // Deliberately no CompleteAsync, as above: this request is abandoned too.
            await lifetime.AfterRunAsync(0, CancellationToken.None).ConfigureAwait(false);

            return $"created={HandedOverBenchmarks.HandedOver.Created} disposed={HandedOverBenchmarks.HandedOver.Disposed}";
        }

        /// <summary>
        /// Ends an application while a request that handed its benchmarks over has taken them back, which is what a
        /// run stopped by a critical validation error leaves behind.
        /// </summary>
        /// <remarks>
        /// BenchmarkDotNet returned before the run stage it disposes parameter values from, so it disposed nothing and
        /// they are the request's again. This is the direction that fails silently rather than loudly: values nobody
        /// disposes are not an exception anyone sees, they are the finalizer, which is the dotnet/BenchmarkDotNet#1383
        /// hang.
        /// </remarks>
        /// <returns>The counts, as a line.</returns>
        private static async Task<string> ReportTakenBackRequestAsync()
        {
            var lifetime = new ParameterValueLifetime();
            var cases = BenchmarkConverter.TypeToBenchmarks(typeof(TakenBackBenchmarks)).BenchmarksCases;

            var request = lifetime.BeginRequest();
            request.Track(cases);
            request.HandOver(cases);
            request.TakeBack();

            // Deliberately no CompleteAsync: the request is abandoned while holding them again.
            await lifetime.AfterRunAsync(0, CancellationToken.None).ConfigureAwait(false);

            return $"created={TakenBackBenchmarks.TakenBack.Created} disposed={TakenBackBenchmarks.TakenBack.Disposed}";
        }

        /// <summary>
        /// Ends an application while a request still in flight has enumerated values that BenchmarkDotNet disposed
        /// when it ran them for the request before, which is the server-mode sequence: a run, then the discovery or
        /// run the IDE issues next over the same cached values.
        /// </summary>
        /// <remarks>
        /// Disposing them again is a user's Dispose() called twice, which counts wrong, throws, or releases a shared
        /// handle somebody else is holding. The sweep knows them as disposed however it reaches them - through what is
        /// held from the completed request, or through the scope of the one that enumerated them again.
        /// </remarks>
        /// <returns>The counts, as a line.</returns>
        private static async Task<string> ReportAlreadyDisposedValuesAsync()
        {
            var lifetime = new ParameterValueLifetime();
            var cases = BenchmarkConverter.TypeToBenchmarks(typeof(AlreadyDisposedBenchmarks)).BenchmarksCases;

            // The run that BenchmarkDotNet disposed these for, which is what completing with them as the ran cases
            // says. It disposed them itself, so nothing here counts as disposal yet.
            var ran = lifetime.BeginRequest();
            ran.Track(cases);
            await ran.CompleteAsync(cases).ConfigureAwait(false);

            // The request the IDE issued next, enumerating the very same cached values, still in flight when the
            // application ends.
            var next = lifetime.BeginRequest();
            next.Track(cases);

            await lifetime.AfterRunAsync(0, CancellationToken.None).ConfigureAwait(false);

            return $"created={AlreadyDisposedBenchmarks.AlreadyDisposed.Created} "
                + $"disposed={AlreadyDisposedBenchmarks.AlreadyDisposed.Disposed}";
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
