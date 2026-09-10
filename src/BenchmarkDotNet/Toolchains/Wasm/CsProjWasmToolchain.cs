using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Toolchains.Wasm;

public abstract class CsProjWasmToolchain : CsProjNetToolchain
{
    private readonly WasmSettings settings;

    protected CsProjWasmToolchain(string name, WasmRuntime runtime, WasmSettings settings, bool aot, bool useCoreClrRuntime)
        : this(name, runtime, settings, Resolve(settings, runtime), aot, useCoreClrRuntime) { }

    // The build components receive `resolved` (target framework moniker filled in from the runtime); the original
    // `settings` is stored for equality and the settings column so an unset moniker is not surfaced as the runtime's.
    // The executor does not use the moniker, so it keeps the original settings.
    private CsProjWasmToolchain(string name, WasmRuntime runtime, WasmSettings settings, WasmSettings resolved, bool aot, bool useCoreClrRuntime)
        : base(name, runtime, settings,
            new CsProjWasmGenerator(resolved, aot, useCoreClrRuntime),
            new DotNetCliPublisher(resolved, logOutput: aot),
            new WasmExecutor(settings))
        => this.settings = settings;

    // Fills the target framework moniker in from the runtime only when the user left it unset, avoiding both the
    // settings copy and the GetTfm() string otherwise.
    private static WasmSettings Resolve(WasmSettings settings, WasmRuntime runtime)
        => settings.TargetFrameworkMoniker.IsNotBlank() ? settings : settings with { TargetFrameworkMoniker = runtime.GetTfm() };

    public override async IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver)
    {
        await foreach (var validationError in base.ValidateAsync(benchmarkCase, resolver).ConfigureAwait(false))
        {
            yield return validationError;
        }

        if (!ProcessHelper.TryResolveExecutableInPath(settings.JavaScriptEngine, out _))
        {
            yield return new ValidationError(true,
                $"The JavaScript engine '{settings.JavaScriptEngine}' was not found. Make sure it is installed and on your PATH.",
                benchmarkCase);
        }
    }
}
