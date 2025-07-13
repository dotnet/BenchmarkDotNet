using BenchmarkDotNet.Environments;
using System;
using System.Diagnostics;
using static BenchmarkDotNet.Portability.RuntimeInformation;

namespace BenchmarkDotNet.Portability
{
    // Implementation is based on article https://medium.com/@meriffa/net-core-concepts-tiered-compilation-10f7da3a29c7
    // documentation https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation
    // and source https://github.com/dotnet/runtime/blob/71c30b405516b1fe774a1bfdbc43cd804468568f/src/coreclr/vm/eeconfig.cpp
    internal static class JitInfo
    {
        public const string MinOptsEnv = "JITMinOpts";
        public const string TieredCompilationEnv = "TieredCompilation";
        public const string DynamicPGOEnv = "TieredPGO";
        public const string AggressiveTieringEnv = "TC_AggressiveTiering";
        public const string CallCountThresholdEnv = "TC_CallCountThreshold";
        public const string CallCountingDelayMsEnv = "TC_CallCountingDelayMs";

        // .Net 5 and older uses COMPlus_ prefix,
        // .Net 6+ uses DOTNET_ prefix, but still supports legacy COMPlus_.
        private static bool IsEnvVarEnabled(string name)
            => Environment.GetEnvironmentVariable($"COMPlus_{name}") == "1"
            || Environment.GetEnvironmentVariable($"DOTNET_{name}") == "1";

        private static bool IsEnvVarDisabled(string name)
            => Environment.GetEnvironmentVariable($"COMPlus_{name}") == "0"
            || Environment.GetEnvironmentVariable($"DOTNET_{name}") == "0";

        private static bool TryParseEnvVar(string name, out int value)
            => int.TryParse(Environment.GetEnvironmentVariable($"COMPlus_{name}"), out value)
            || int.TryParse(Environment.GetEnvironmentVariable($"DOTNET_{name}"), out value);

        private static bool IsKnobEnabled(string name)
            => AppContext.TryGetSwitch(name, out bool isEnabled) && isEnabled;

        private static bool IsKnobDisabled(string name)
            => AppContext.TryGetSwitch(name, out bool isEnabled) && !isEnabled;

        private static bool TryParseKnob(string name, out int value)
            => int.TryParse(AppContext.GetData(name) as string, out value);

        /// <summary>
        /// Is tiered JIT enabled?
        /// </summary>
        public static readonly bool IsTiered =
            IsNetCore
            // JITMinOpts disables tiered compilation (all methods are effectively tier0 instead of tier1).
            && !IsEnvVarEnabled(MinOptsEnv)
            && ((CoreRuntime.TryGetVersion(out var version) && version.Major >= 3)
                // Enabled by default in netcoreapp3.0+, check if it's disabled.
                ? !IsEnvVarDisabled(TieredCompilationEnv) && !IsKnobDisabled("System.Runtime.TieredCompilation")
                // Disabled by default in netcoreapp2.X, check if it's enabled.
                : IsEnvVarEnabled(TieredCompilationEnv) || IsKnobEnabled("System.Runtime.TieredCompilation"));

        /// <summary>
        /// Is tiered JIT enabled with dynamic profile-guided optimization (tier0 instrumented)?
        /// </summary>
        public static readonly bool IsDPGO =
            IsTiered
            // Added experimentally in .Net 6
            && Environment.Version.Major >= 6
            // Disabled if QuickJit is disabled in .Net 7+.
            && (Environment.Version.Major < 7 || (!IsEnvVarDisabled("TC_QuickJit") && !IsKnobDisabled("System.Runtime.TieredCompilation.QuickJit")))
            && (Environment.Version.Major >= 8
                // Enabled by default in .Net 8, check if it's disabled
                ? !IsEnvVarDisabled(DynamicPGOEnv) && !IsKnobDisabled("System.Runtime.TieredPGO")
                // Disabled by default in earlier versions, check if it's enabled.
                : IsEnvVarEnabled(DynamicPGOEnv) || IsKnobEnabled("System.Runtime.TieredPGO"));


        /// <summary>
        /// The number of times a method must be called before it will be eligible for the next JIT tier.
        /// </summary>
        public static readonly int TieredCallCountThreshold = GetTieredCallCountThreshold();

        private static int GetTieredCallCountThreshold()
        {
            if (!IsTiered)
            {
                return 0;
            }
            // AggressiveTiering was added in .Net 5.
            if (Environment.Version.Major >= 5 && IsEnvVarEnabled(AggressiveTieringEnv))
            {
                return 1;
            }
            if (TryParseEnvVar(CallCountThresholdEnv, out int callCountThreshold))
            {
                return callCountThreshold;
            }
            // CallCountThreshold was added as a knob in .Net 8.
            if (Environment.Version.Major >= 8 && TryParseKnob("System.Runtime.TieredCompilation.CallCountThreshold", out callCountThreshold))
            {
                return callCountThreshold;
            }
            // Default 30 if it's not configured.
            return 30;
        }

        /// <summary>
        /// How long to wait to ensure tiered JIT call counting has begun.
        /// </summary>
        public static readonly TimeSpan TieredDelay = GetTieredDelay();

        private static TimeSpan GetTieredDelay()
        {
            if (!IsTiered)
            {
                return TimeSpan.Zero;
            }
            // AggressiveTiering was added in .Net 5.
            if (Environment.Version.Major >= 5 && IsEnvVarEnabled(AggressiveTieringEnv))
            {
                return TimeSpan.Zero;
            }
            if (TryParseEnvVar(CallCountingDelayMsEnv, out int callCountDelay))
            {
                return TimeSpan.FromMilliseconds(callCountDelay);
            }
            // CallCountingDelayMs was added as a knob in .Net 8.
            if (Environment.Version.Major >= 8 && TryParseKnob("System.Runtime.TieredCompilation.CallCountingDelayMs", out callCountDelay))
            {
                return TimeSpan.FromMilliseconds(callCountDelay);
            }
            // Default 100 if it's not configured.
            return TimeSpan.FromMilliseconds(100);
        }

        /// <summary>
        /// How long to wait for the JIT to have completed tiered compilation in the background.
        /// </summary>
        public static readonly TimeSpan BackgroundCompilationDelay =
            IsTiered
                // It's impossible for us to know exactly how long to wait without hooking into JIT notifications (which we can't do in-process).
                // 100ms should be enough most of the time, but we bump it up to 250ms for higher confidence.
                // When https://github.com/dotnet/runtime/issues/101868 is resolved, if AggressiveTiering is enabled, we can skip the wait time and return TimeSpan.Zero.
                ? TimeSpan.FromMilliseconds(250)
                : TimeSpan.Zero;

        public static readonly bool IsRyuJit = GetIsRyuJit();

        private static bool GetIsRyuJit()
        {
            if (IsNetCore) // CoreCLR supports only RyuJIT.
                return true;
            if (IsMono || !IsFullFramework) // If it's not Core or Framework, it's not RyuJIT.
                return false;
            if (!Is64BitPlatform()) // Framework supports RyuJIT only in 64-bit process.
                return false;

            // https://stackoverflow.com/a/31534544
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                // clrjit.dll -> RyuJit
                // compatjit.dll -> Legacy Jit
                if (module.ModuleName == "clrjit.dll")
                {
                    return true;
                }
            }
            return false;
        }

        public static Jit GetCurrentJit() => IsRyuJit ? Jit.RyuJit : Jit.LegacyJit;

        public static string GetInfo()
        {
            if (IsNativeAOT)
                return "NativeAOT";
            if (IsAot)
                return "AOT";
            if (IsMono || IsWasm)
                return ""; // There is no helpful information about JIT on Mono
            if (IsRyuJit)
                return "RyuJIT";
            if (IsFullFramework)
                return "LegacyJIT";

            return Unknown;
        }
    }
}