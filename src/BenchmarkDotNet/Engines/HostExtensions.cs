using JetBrains.Annotations;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines
{
    public static class HostExtensions
    {
        [StringFormatMethod("messageFormat")]
        public static ValueTask WriteLineAsync(this IHost host, string messageFormat, params object[] args)
            => host.WriteLineAsync(string.Format(messageFormat, args));

        public static ValueTask BeforeAnythingElseAsync(this IHost host) => host.SendSignalAsync(HostSignal.BeforeAnythingElse);

        public static ValueTask BeforeMainRunAsync(this IHost host) => host.SendSignalAsync(HostSignal.BeforeActualRun);

        public static ValueTask AfterMainRunAsync(this IHost host) => host.SendSignalAsync(HostSignal.AfterActualRun);

        public static ValueTask AfterAllAsync(this IHost host) => host.SendSignalAsync(HostSignal.AfterAll);
    }
}
