using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.TestHost;
using System.Reflection;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Internals
{
    /// <summary>
    /// The parts of Microsoft.Testing.Platform that surround a test framework while it serves one request, recorded
    /// rather than acted on.
    /// </summary>
#pragma warning disable TPEXP // Completing a request is still marked as experimental by the platform.
    internal sealed class PlatformStub : IMessageBus, IOutputDevice, IExecuteRequestCompletionNotifier
#pragma warning restore TPEXP
    {
        private readonly List<string> messages = [];

        /// <summary>
        /// Gets the node states the framework published, as "&lt;state&gt; &lt;display name&gt;" lines, and the
        /// output device data it displayed, as "output &lt;text&gt;" lines.
        /// </summary>
        public IReadOnlyList<string> Messages => messages;

        /// <summary>
        /// Gets whether the framework completed the request.
        /// </summary>
        public bool IsComplete { get; private set; }

        public Task PublishAsync(IDataProducer dataProducer, IData data)
        {
            if (data is TestNodeUpdateMessage update)
            {
                var state = update.TestNode.Properties.SingleOrDefault<TestNodeStateProperty>();

                lock (messages)
                    messages.Add($"{StateName(state)} {update.TestNode.DisplayName}");
            }

            return Task.CompletedTask;
        }

        public Task DisplayAsync(IOutputDeviceDataProducer producer, IOutputDeviceData data, CancellationToken cancellationToken)
        {
            var text = data switch
            {
                WarningMessageOutputDeviceData warning => warning.Message,
                ErrorMessageOutputDeviceData error => error.Message,
                TextOutputDeviceData plain => plain.Text,
                _ => data.ToString() ?? string.Empty
            };

            lock (messages)
                messages.Add($"output {text}");

            return Task.CompletedTask;
        }

        public void Complete() => IsComplete = true;

        /// <summary>
        /// Builds the service provider the framework resolves the output device from.
        /// </summary>
        /// <remarks>
        /// <c>GetOutputDevice</c> casts to the platform's own provider rather than asking any
        /// <see cref="IServiceProvider"/> for the service, so this has to be that type - which the platform keeps to
        /// itself - rather than something implemented here.
        /// </remarks>
        /// <param name="outputDevice">The output device to resolve.</param>
        /// <returns>The created service provider.</returns>
        public static IServiceProvider CreateServiceProvider(IOutputDevice outputDevice)
        {
            var type = typeof(IOutputDevice).Assembly.GetType("Microsoft.Testing.Platform.Services.ServiceProvider", throwOnError: true)!;
            var provider = (IServiceProvider)Activator.CreateInstance(type)!;
            var addService = type.GetMethod("AddService", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("The platform's service provider no longer has an AddService method.");

            addService.Invoke(provider, [outputDevice, false]);

            return provider;
        }

        /// <summary>
        /// Creates the session a request belongs to. The platform keeps the constructor to itself, and there is no
        /// other way to build one of these from outside it.
        /// </summary>
        /// <returns>The created session context.</returns>
        public static TestSessionContext CreateSessionContext()
        {
            var constructor = typeof(TestSessionContext).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(SessionUid)],
                modifiers: null)
                ?? throw new InvalidOperationException(
                    $"{nameof(TestSessionContext)} no longer has a constructor taking a {nameof(SessionUid)}.");

            return (TestSessionContext)constructor.Invoke([new SessionUid(Guid.NewGuid().ToString())]);
        }

        private static string StateName(TestNodeStateProperty? state) => state switch
        {
            null => "none",
            DiscoveredTestNodeStateProperty => "discovered",
            InProgressTestNodeStateProperty => "in-progress",
            PassedTestNodeStateProperty => "passed",
            FailedTestNodeStateProperty failed => $"failed({failed.Explanation})",
            ErrorTestNodeStateProperty error => $"error({error.Explanation})",
            SkippedTestNodeStateProperty => "skipped",
            _ => state.GetType().Name
        };
    }
}
