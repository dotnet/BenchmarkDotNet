using System.CommandLine;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.ConsoleArguments
{
    internal class CommandLineConsole : IConsole
    {
        private readonly ILogger logger;
        private readonly CommandLineStreamWriter streamWriter;
        public CommandLineConsole(ILogger logger)
        {
            this.logger = logger;
            this.streamWriter = new CommandLineStreamWriter(logger);
        }

        public IStandardStreamWriter Out => streamWriter;
        public bool IsOutputRedirected => true;
        public IStandardStreamWriter Error => streamWriter;
        public bool IsErrorRedirected => true;
        public bool IsInputRedirected => false;
    }
}