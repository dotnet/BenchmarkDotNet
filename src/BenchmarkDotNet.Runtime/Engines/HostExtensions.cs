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

        public static void AfterSetup(this IHost host) =>
            host.SendSignal(HostSignal.AfterSetup);

        public static void BeforeMainRun(this IHost host) =>
            host.SendSignal(HostSignal.BeforeMainRun);

        public static void BeforeCleanup(this IHost host) =>
            host.SendSignal(HostSignal.BeforeCleanup);

        public static void AfterAnythingElse(this IHost host) =>
            host.SendSignal(HostSignal.AfterAnythingElse);
    }
}
