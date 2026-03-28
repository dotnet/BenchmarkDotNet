using BenchmarkDotNet.Loggers;
using System.Text;

namespace BenchmarkDotNet.ConsoleArguments
{
    internal class LoggerWrapper : TextWriter
    {
        private readonly ILogger logger;

        public LoggerWrapper(ILogger logger) => this.logger = logger;

        public override Encoding Encoding { get; } = Encoding.ASCII;

        public override void Write(string? value)
        {
            if (value is null)
                return;

            logger.WriteInfo(value);
        }
    }
}