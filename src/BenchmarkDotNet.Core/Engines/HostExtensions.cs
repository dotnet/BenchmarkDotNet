using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public static class HostExtensions
    {
        [StringFormatMethod("messageFormat")]
        public static void WriteLine(
            this IHost host, string messageFormat, params object[] args) =>
            host.WriteLine(string.Format(messageFormat, args));

        public static void BeforeAnythingElse(this IHost host) =>
            host.SendSignal(HostSignal.BeforeAnythingElse);

        public static void AfterGlobalSetup(this IHost host) =>
            host.SendSignal(HostSignal.AfterGlobalSetup);

        public static void BeforeMainRun(this IHost host) =>
            host.SendSignal(HostSignal.BeforeMainRun);

        public static void BeforeGlobalCleanup(this IHost host) =>
            host.SendSignal(HostSignal.BeforeGlobalCleanup);

        public static void AfterAnythingElse(this IHost host) =>
            host.SendSignal(HostSignal.AfterAnythingElse);
    }
}
