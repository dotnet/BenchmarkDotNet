using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System.Reflection;

namespace BenchmarkDotNet.Validators
{
    /// <summary>
    /// Stops a run early when BenchmarkDotNet itself runs inside a Native AOT application on a desktop OS, where the
    /// benchmarks would otherwise fail later with errors that don't point at the cause.
    /// </summary>
    public class NativeAotHostValidator : IValidator
    {
        // Mobile OSes always run in-process, so only a desktop host is expected to use the JIT instead.
        internal static readonly bool IsNativeAotHost = RuntimeInformation.IsNativeAOT && !OsDetector.IsMobile();

        public static readonly IValidator FailOnError = new NativeAotHostValidator(IsNativeAotHost);

        private readonly bool isNativeAotHost;

        internal NativeAotHostValidator(bool isNativeAotHost) => this.isNativeAotHost = isNativeAotHost;

        public bool TreatsWarningsAsErrors => true;

        internal static string RunWithJitHint
            => $"Run the host with the JIT (`dotnet run -c Release`) and target Native AOT with `--runtimes nativeaot{Environment.Version.Major}.{Environment.Version.Minor}`.";

        /// <summary>Appended to "No [Benchmark] attribute found" in a Native AOT host, where the likely cause is the trimmer.</summary>
        internal static string TrimmedBenchmarksHint(string assemblyName)
            => " BenchmarkDotNet is running inside a Native AOT application, where the trimmer removes benchmark methods that are only found through reflection. "
            + RunWithJitHint
            + $" To run in-process anyway, keep the methods with `<TrimmerRootAssembly Include=\"{assemblyName}\" />` in the project file.";

        public async IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
        {
            if (!isNativeAotHost)
                yield break;

            foreach (var benchmark in validationParameters.Benchmarks)
            {
                var toolchain = benchmark.GetToolchain();

                if (!toolchain.IsInProcess)
                {
                    yield return new ValidationError(true,
                        $"BenchmarkDotNet is running inside a Native AOT application, so the {toolchain} toolchain cannot generate and build a project for '{benchmark.DisplayInfo}'. {RunWithJitHint}",
                        benchmark);
                    continue;
                }

                if (toolchain is InProcessEmitToolchain)
                {
                    yield return new ValidationError(true,
                        $"BenchmarkDotNet is running inside a Native AOT application, where {nameof(InProcessEmitToolchain)} cannot emit code for '{benchmark.DisplayInfo}'. {RunWithJitHint} Alternatively, use {nameof(InProcessNoEmitToolchain)}.",
                        benchmark);
                    continue;
                }

                if (toolchain is InProcessNoEmitToolchain { BenchmarkActionFactory: null })
                {
                    var method = GetMethodsInvokedByAction(benchmark.Descriptor).FirstOrDefault(m => RequiresGenericAction(m.ReturnType));
                    if (method != null)
                    {
                        yield return new ValidationError(true,
                            $"BenchmarkDotNet is running inside a Native AOT application, where {nameof(InProcessNoEmitToolchain)} cannot invoke '{method.Name}' because it returns {method.ReturnType.GetDisplayName()}"
                            + " (only void, Task and ValueTask are supported there). "
                            + $"{RunWithJitHint} Alternatively, provide {nameof(InProcessNoEmitSettings)}.{nameof(InProcessNoEmitSettings.BenchmarkActionFactory)}.",
                            benchmark);
                    }
                }
            }
        }

        private static IEnumerable<MethodInfo> GetMethodsInvokedByAction(Descriptor descriptor)
        {
            yield return descriptor.WorkloadMethod;

            foreach (var method in new[] { descriptor.GlobalSetupMethod, descriptor.GlobalCleanupMethod, descriptor.IterationSetupMethod, descriptor.IterationCleanupMethod })
            {
                if (method != null)
                    yield return method;
            }
        }

        // The other return types are invoked through a closed generic action (BenchmarkAction<T>, BenchmarkActionTask<T>, ...)
        // that BenchmarkActionFactory builds with MakeGenericType and Activator.CreateInstance. In a Native AOT application
        // that fails for reference and value types alike: the constructors are trimmed, and value types also lack native code.
        private static bool RequiresGenericAction(Type returnType)
            => returnType != typeof(void)
            && returnType != typeof(void*)
            && returnType != typeof(Task)
            && returnType != typeof(ValueTask);
    }
}
