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
        private readonly Process process;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly Dictionary<int, TaskCompletionSource<bool>> pendingRequests = [];
        private readonly Dictionary<string, TaskCompletionSource<bool>> pendingRuns = [];
        private readonly List<ServerNode> nodes = [];
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
        /// <returns>The nodes the discovery reported, and the last state each ran node was reported in.</returns>
        public static (IReadOnlyList<ServerNode> Discovered, IReadOnlyList<ServerNode> Ran) DiscoverThenRun(
            string application,
            string runFilter,
            TimeSpan timeout)
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

                return session.Run(runFilter);
            }
            finally
            {
                listener.Stop();
            }
        }

        private (IReadOnlyList<ServerNode>, IReadOnlyList<ServerNode>) Run(string runFilter)
        {
            SendRequest("initialize", new
            {
                processId = Environment.ProcessId,
                clientInfo = new { name = "BenchmarkDotNet.IntegrationTests", version = "1.0.0" },
                capabilities = new { testing = new { debuggerProvider = false } }
            });
            Send(new { jsonrpc = "2.0", method = "initialized", @params = new { } });

            var discovered = Exchange("testing/discoverTests", runId => new { runId });

            var selected = discovered
                .Where(node => node.DisplayName.Contains(runFilter, StringComparison.Ordinal))
                .ToArray();

            if (selected.Length == 0)
                throw new InvalidOperationException($"No discovered benchmark matched '{runFilter}'.");

            var ran = Exchange("testing/runTests", runId => new
            {
                runId,
                tests = selected
                    .Select(node => new Dictionary<string, object> { ["uid"] = node.Uid, ["display-name"] = node.DisplayName })
                    .ToArray()
            });

            Send(new { jsonrpc = "2.0", method = "exit", @params = new { } });

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("The test host did not exit after the session was closed.");
            }

            // Lets the probe write the report file its process exit handler produces.
            process.WaitForExit();

            return (discovered, ran);
        }

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
                lock (pendingRequests)
                {
                    if (pendingRequests.TryGetValue(id.GetInt32(), out var request))
                        request.TrySetResult(true);
                }
            }

            if (!root.TryGetProperty("method", out var method)
                || method.GetString() != "testing/testUpdates/tests"
                || !root.TryGetProperty("params", out var parameters))
            {
                return;
            }

            if (parameters.TryGetProperty("changes", out var changes) && changes.ValueKind == JsonValueKind.Array)
            {
                lock (nodes)
                {
                    foreach (var change in changes.EnumerateArray())
                    {
                        var node = change.GetProperty("node");
                        if (node.TryGetProperty("node-type", out var nodeType) && nodeType.GetString() == "action")
                        {
                            nodes.Add(new ServerNode(
                                node.GetProperty("uid").GetString()!,
                                node.GetProperty("display-name").GetString()!,
                                node.TryGetProperty("execution-state", out var state) ? state.GetString()! : ""));
                        }
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

        internal sealed record ServerNode(string Uid, string DisplayName, string ExecutionState);
    }
}
#endif
