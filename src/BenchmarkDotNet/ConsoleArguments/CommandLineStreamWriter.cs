using System.CommandLine;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.ConsoleArguments
{
    internal class CommandLineStreamWriter : IStandardStreamWriter
    {
        private readonly ILogger logger;

        public CommandLineStreamWriter(ILogger logger)
        {
            this.logger = logger;
        }
        public void Write(string value)
        {
            logger.WriteLine(value);
        }
    }
}