using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BenchmarkDotNet.Helpers;

[UsedImplicitly]
public static class IpcHelper
{
    public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(1);

    internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    internal const string PortDescriptor = "--ipcPort";

    public static IHost GetHost(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == PortDescriptor)
            {
#if NET8_0_OR_GREATER
                if (Portability.RuntimeInformation.IsWasm)
                {
                    // The port is used to open the websocket in JS, we don't need to use it here.
                    return new WebSocketHost();
                }
#endif
                int port = int.Parse(args[i + 1]);
                var client = new TcpClient();
                client.Connect(IPAddress.Loopback, port);
                return new TcpHost(client);
            }
        }
        return new NoAcknowledgementConsoleHost();
    }
}
