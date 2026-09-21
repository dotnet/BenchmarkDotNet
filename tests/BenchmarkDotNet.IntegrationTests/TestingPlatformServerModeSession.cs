#if NETCOREAPP
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace BenchmarkDotNet.IntegrationTests
{
    /// <summary>
    /// Drives a Microsoft.Testing.Platform application through its server mode, which is how Visual Studio and the
    /// Visual Studio Code Test Explorer run tests: one process serves a discovery request and then the run requests
    /// that follow it, rather than being started again for each of them.
    /// </summary>
    /// <remarks>
    /// The platform speaks JSON-RPC framed the way the language server protocol frames it, over a socket that the
    /// client listens on and the test host connects back to. Only the handful of messages this needs are implemented:
    /// `initialize`, `testing/discoverTests`, `testing/runTests` and `exit`.
    /// </remarks>
    internal sealed class TestingPlatformServerModeSession : IDisposable
    {
        private const string ActionNode = "action";
        private const string GroupNode = "group";

        private readonly Process process;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly Dictionary<int, TaskCompletionSource<bool>> pendingRequests = [];
        private readonly Dictionary<string, TaskCompletionSource<bool>> pendingRuns = [];
        private readonly List<ServerNode> nodes = [];
        private readonly List<ServerAttachment> attachments = [];
        private readonly List<ServerLogMessage> logs = [];
        private readonly TimeSpan timeout;
        private int lastRequestId;

        private TestingPlatformServerModeSession(Process process, TcpClient client, TimeSpan timeout)
        {
            this.process = process;
            this.client = client;
            this.timeout = timeout;
            stream = client.GetStream();

            Task.Run(ReadLoop);
        }

        /// <summary>
        /// Discovers every benchmark of the application and then runs the ones whose display name contains the given
        /// text, from the one process.
        /// </summary>
        /// <param name="application">The probe application to drive.</param>
        /// <param name="runFilter">The text the display name of a benchmark has to contain to be run.</param>
        /// <param name="timeout">How long any one step may take.</param>
        /// <param name="discoverAgain">Whether to discover a second time once the run is over, as an IDE refreshing does.</param>
        /// <returns>
        /// The nodes the discovery reported, the last state each ran node was reported in, and the group nodes the
        /// run reported - the ones carrying the summary table of their type.
        /// </returns>
        public static (IReadOnlyList<ServerNode> Discovered, IReadOnlyList<ServerNode> Ran, IReadOnlyList<ServerNode> Groups) DiscoverThenRun(
            string application,
            string runFilter,
            TimeSpan timeout,
            bool discoverAgain = false)
        {
            var session = DiscoverThenRunSession(application, runFilter, timeout, discoverAgain);

            return (session.Discovered, session.Ran, session.Groups);
        }

        /// <summary>
        /// The same session as <see cref="DiscoverThenRun"/>, reporting what it was told outside the test tree as
        /// well: the attachments of the run, and the messages the platform logged to the client.
        /// </summary>
        public static ServerSession DiscoverThenRunSession(
            string application,
            string runFilter,
            TimeSpan timeout,
            bool discoverAgain = false)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                var startInfo = new ProcessStartInfo(application)
                {
                    WorkingDirectory = Path.GetDirectoryName(application),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                foreach (var argument in new[] { "--server", "--client-host", "127.0.0.1", "--client-port", port.ToString(), "--no-ansi" })
                    startInfo.ArgumentList.Add(argument);

                using var process = Process.Start(startInfo)!;
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var client = listener.AcceptTcpClient();
                using var session = new TestingPlatformServerModeSession(process, client, timeout);

                var (discovered, ran, groups) = session.Run(runFilter, discoverAgain);

                return new ServerSession(discovered, ran, groups, session.attachments, session.logs);
            }
            finally
            {
                listener.Stop();
            }
        }

        private (IReadOnlyList<ServerNode>, IReadOnlyList<ServerNode>, IReadOnlyList<ServerNode>) Run(string runFilter, bool discoverAgain)
        {
            SendRequest("initialize", new
            {
                processId = Environment.ProcessId,
                clientInfo = new { name = "BenchmarkDotNet.IntegrationTests", version = "1.0.0" },
                capabilities = new { testing = new { debuggerProvider = false } }
            });
            Send(new { jsonrpc = "2.0", method = "initialized", @params = new { } });

            var discovered = OfType(Exchange("testing/discoverTests", runId => new { runId }), ActionNode);

            var selected = discovered
                .Where(node => node.DisplayName.Contains(runFilter, StringComparison.Ordinal))
                .ToArray();

            if (selected.Length == 0)
                throw new InvalidOperationException($"No discovered benchmark matched '{runFilter}'.");

            var reported = Exchange("testing/runTests", runId => new
            {
                runId,
                tests = selected
                    .Select(node => new Dictionary<string, object> { ["uid"] = node.Uid, ["display-name"] = node.DisplayName })
                    .ToArray()
            });

            if (discoverAgain)
                Exchange("testing/discoverTests", runId => new { runId });

            Send(new { jsonrpc = "2.0", method = "exit", @params = new { } });

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("The test host did not exit after the session was closed.");
            }

            // Lets the probe write the report file its process exit handler produces.
            process.WaitForExit();

            return (discovered, OfType(reported, ActionNode), OfType(reported, GroupNode));
        }

        /// <summary>
        /// The files reported on one node, which the platform serializes as an "attachments.{index}.uri" property
        /// per file rather than as a list.
        /// </summary>
        private static string[] ReadAttachments(JsonElement node)
        {
            var files = new List<string>();

            for (var index = 0; node.TryGetProperty($"attachments.{index}.uri", out var uri); index++)
                files.Add(uri.GetString() ?? "");

            return [.. files];
        }

        private static ServerNode[] OfType(IEnumerable<ServerNode> nodes, string nodeType)
            => nodes.Where(node => node.NodeType == nodeType).ToArray();

        /// <summary>
        /// Sends one request and collects the node updates the platform reports for it.
        /// </summary>
        private ServerNode[] Exchange(string method, Func<string, object> parameters)
        {
            var runId = Guid.NewGuid().ToString();
            var completion = new TaskCompletionSource<bool>();

            lock (pendingRuns)
                pendingRuns[runId] = completion;

            lock (nodes)
                nodes.Clear();

            SendRequest(method, parameters(runId));

            if (!completion.Task.Wait(timeout))
                throw new TimeoutException($"'{method}' did not complete within {timeout}.");

            lock (nodes)
            {
                // The platform reports a node again whenever its state changes, and the last one is the outcome.
                return nodes
                    .GroupBy(node => node.Uid, StringComparer.Ordinal)
                    .Select(group => group.Last())
                    .ToArray();
            }
        }

        private void SendRequest(string method, object parameters)
        {
            var id = Interlocked.Increment(ref lastRequestId);
            var completion = new TaskCompletionSource<bool>();

            lock (pendingRequests)
                pendingRequests[id] = completion;

            Send(new { jsonrpc = "2.0", id, method, @params = parameters });

            if (!completion.Task.Wait(timeout))
                throw new TimeoutException($"'{method}' was not answered within {timeout}.");
        }

        private void Send(object message)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var header = Encoding.UTF8.GetBytes($"Content-Length: {body.Length}\r\n\r\n");

            lock (stream)
            {
                stream.Write(header, 0, header.Length);
                stream.Write(body, 0, body.Length);
                stream.Flush();
            }
        }

        private void ReadLoop()
        {
            var buffer = new List<byte>();
            var chunk = new byte[8192];

            while (true)
            {
                int read;

                try
                {
                    read = stream.Read(chunk, 0, chunk.Length);
                }
                catch
                {
                    return;
                }

                if (read == 0)
                    return;

                buffer.AddRange(chunk.Take(read));

                while (TryReadMessage(buffer, out var json))
                    Handle(json);
            }
        }

        private static bool TryReadMessage(List<byte> buffer, out string json)
        {
            json = string.Empty;

            // The header is ASCII, so the byte offsets of the separator and of the character offsets agree.
            var text = Encoding.ASCII.GetString(buffer.ToArray());
            var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd < 0)
                return false;

            var lengthHeader = text.Substring(0, headerEnd)
                .Split(["\r\n"], StringSplitOptions.None)
                .First(header => header.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase));
            var length = int.Parse(lengthHeader.Split(':')[1].Trim());
            var bodyStart = headerEnd + 4;

            if (buffer.Count < bodyStart + length)
                return false;

            json = Encoding.UTF8.GetString(buffer.GetRange(bodyStart, length).ToArray());
            buffer.RemoveRange(0, bodyStart + length);

            return true;
        }

        private void Handle(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number)
            {
                // The files a request reported come back on its response rather than as an update of their own: the
                // attachment notification is only used by the passive connection a client opens for a second process.
                if (root.TryGetProperty("result", out var result)
                    && result.TryGetProperty("attachments", out var reported)
                    && reported.ValueKind == JsonValueKind.Array)
                {
                    lock (attachments)
                    {
                        foreach (var attachment in reported.EnumerateArray())
                        {
                            attachments.Add(new ServerAttachment(
                                attachment.TryGetProperty("uri", out var uri) ? uri.GetString() ?? "" : "",
                                attachment.TryGetProperty("display-name", out var name) ? name.GetString() ?? "" : "",
                                attachment.TryGetProperty("producer", out var producer) ? producer.GetString() ?? "" : ""));
                        }
                    }
                }

                lock (pendingRequests)
                {
                    if (pendingRequests.TryGetValue(id.GetInt32(), out var request))
                        request.TrySetResult(true);
                }
            }

            if (!root.TryGetProperty("method", out var method) || !root.TryGetProperty("params", out var parameters))
                return;

            // What the platform's output device was given, which a client shows in its output window. The level is
            // what an IDE filters on, so it is recorded rather than only the text.
            if (method.GetString() == "client/log")
            {
                lock (logs)
                {
                    logs.Add(new ServerLogMessage(
                        parameters.GetProperty("level").GetString()!,
                        parameters.GetProperty("message").GetString() ?? ""));
                }

                return;
            }

            // The same files, for a client that is sent them as an update instead - which is the shape a passive
            // connection uses.
            if (method.GetString() == "testing/testUpdates/attachments")
            {
                if (parameters.TryGetProperty("attachments", out var reported) && reported.ValueKind == JsonValueKind.Array)
                {
                    lock (attachments)
                    {
                        foreach (var attachment in reported.EnumerateArray())
                        {
                            attachments.Add(new ServerAttachment(
                                attachment.TryGetProperty("uri", out var uri) ? uri.GetString() ?? "" : "",
                                attachment.TryGetProperty("display-name", out var name) ? name.GetString() ?? "" : "",
                                attachment.TryGetProperty("producer", out var producer) ? producer.GetString() ?? "" : ""));
                        }
                    }
                }

                return;
            }

            if (method.GetString() != "testing/testUpdates/tests")
                return;

            if (parameters.TryGetProperty("changes", out var changes) && changes.ValueKind == JsonValueKind.Array)
            {
                lock (nodes)
                {
                    foreach (var change in changes.EnumerateArray())
                    {
                        var node = change.GetProperty("node");

                        // A node the platform serializes without an execution state is a `group` rather than an
                        // `action`: the tree node the benchmarks of a type hang from, which is what carries their
                        // summary table.
                        nodes.Add(new ServerNode(
                            node.GetProperty("uid").GetString()!,
                            node.GetProperty("display-name").GetString()!,
                            node.TryGetProperty("execution-state", out var state) ? state.GetString()! : "",
                            node.TryGetProperty("standardOutput", out var output) ? output.GetString() ?? "" : "",
                            node.TryGetProperty("node-type", out var nodeType) ? nodeType.GetString()! : "",
                            change.TryGetProperty("parent", out var parent) ? parent.GetString() : null,
                            ReadAttachments(node)));
                    }
                }

                return;
            }

            // A null "changes" is how the platform says the request is over.
            if (parameters.TryGetProperty("runId", out var runId))
            {
                lock (pendingRuns)
                {
                    if (pendingRuns.TryGetValue(runId.GetString()!, out var run))
                        run.TrySetResult(true);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // The process is gone, which is what was wanted.
            }

            client.Dispose();
        }

        /// <summary>
        /// Everything one session reported.
        /// </summary>
        /// <param name="Discovered">The benchmarks the discovery listed.</param>
        /// <param name="Ran">The last state each ran benchmark was reported in.</param>
        /// <param name="Groups">The group nodes the run reported - the ones carrying the summary table of their type.</param>
        /// <param name="Attachments">The files the run reported.</param>
        /// <param name="Logs">The messages the platform logged to the client, with the level each was logged at.</param>
        internal sealed record ServerSession(
            IReadOnlyList<ServerNode> Discovered,
            IReadOnlyList<ServerNode> Ran,
            IReadOnlyList<ServerNode> Groups,
            IReadOnlyList<ServerAttachment> Attachments,
            IReadOnlyList<ServerLogMessage> Logs);

        internal sealed record ServerAttachment(string Uri, string DisplayName, string Producer);

        internal sealed record ServerLogMessage(string Level, string Message);

        internal sealed record ServerNode(
            string Uid,
            string DisplayName,
            string ExecutionState,
            string StandardOutput,
            string NodeType,
            string? Parent,
            IReadOnlyList<string> Attachments);
    }
}
#endif
