using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Environments
{
    /// <summary>
    /// Describes a .NET runtime (e.g. .NET Core, .NET Framework, Mono, NativeAOT) that benchmarks can target.
    /// </summary>
    public abstract class Runtime
    {
        /// <summary>
        /// The name of the runtime.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The version of the runtime, or <see langword="null" /> if it is unknown.
        /// </summary>
        public abstract Version? Version { get; }

        /// <summary>
        /// Determines whether the specified object is a <see cref="Runtime" /> of the same type with equal <see cref="Name" /> and <see cref="Version" />.
        /// Concrete runtimes override this to also compare any additional state; this is the single equality entry point,
        /// so all comparison paths (including <see cref="System.Collections.Generic.EqualityComparer{T}" />) go through it.
        /// </summary>
        public override bool Equals(object? obj)
            => obj is Runtime other
            && other.GetType() == GetType()
            && Name == other.Name
            && Version == other.Version;

        /// <summary>
        /// Returns a hash code derived from the runtime type, <see cref="Name" /> and <see cref="Version" />.
        /// </summary>
        public override int GetHashCode()
            => HashCode.Combine(GetType(), Name, Version);

        /// <summary>
        /// Returns the <see cref="Name" />, followed by the <see cref="Version" /> when it is known.
        /// </summary>
        public override string ToString()
            => Version == null ? Name : $"{Name} {Version}";

        /// <summary>
        /// Returns the default <see cref="IToolchain" /> used to build and run benchmarks for this runtime when the job
        /// doesn't specify one explicitly. Custom runtimes should override this to provide their own toolchain.
        /// </summary>
        /// <param name="benchmarkCase">
        /// The benchmark the toolchain is being resolved for; runtimes may inspect its job characteristics or the
        /// descriptor's target assembly.
        /// </param>
        public abstract IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase);

        /// <summary>
        /// Parses a runtime moniker string (e.g. <c>net8.0</c>, <c>net472</c>, <c>netcoreapp3.1</c>, <c>nativeaot8.0</c>,
        /// <c>mono8.0</c>, <c>monowasm8.0</c>, <c>r2r8.0</c>) into the corresponding <see cref="Runtime" />. Both the dotted
        /// TFM spelling and the compact spelling (e.g. <c>net80</c>) are accepted, case-insensitively.
        /// </summary>
        /// <exception cref="ArgumentException">The moniker does not map to a known runtime.</exception>
        public static Runtime Parse(string moniker)
            => TryParse(moniker, out var runtime)
                ? runtime
                : throw new ArgumentException($"Unable to parse '{moniker}' into a known runtime.", nameof(moniker));

        /// <summary>
        /// Attempts to parse a runtime moniker string into the corresponding <see cref="Runtime" />. See <see cref="Parse" />.
        /// </summary>
        public static bool TryParse(string moniker, [NotNullWhen(true)] out Runtime? runtime)
        {
            runtime = null;
            if (moniker.IsBlank())
                return false;

            string s = moniker.Trim();

            // Split off a target-platform suffix (e.g. "net8.0-windows"); it applies to .NET (Core) runtimes.
            string? platform = null;
            int dash = s.IndexOf('-');
            if (dash >= 0)
            {
                platform = s[(dash + 1)..];
                s = s[..dash];
            }

            // Prefixes, most-specific first (so "monowasmaot"/"monoaot" beat "monowasm"/bare "mono", and "netcoreapp"
            // beats "net"). Both the moniker VALUES ("monowasm8.0") and the RuntimeMoniker field-name spellings
            // ("MonoWasm80") parse, since TryParseVersion accepts dotted, compact, and '_'-separated versions.
            if (TryStripPrefix(s, "nativeaot", out string rest) && TryParseVersion(rest, out var version))
                runtime = NativeAotRuntime.From(version);
            else if (TryStripPrefix(s, "monowasmaot", out rest) && TryParseVersion(rest, out version))
                runtime = MonoWasmAotRuntime.From(version);
            else if (TryStripPrefix(s, "monowasm", out rest) && TryParseVersion(rest, out version))
                runtime = MonoWasmRuntime.From(version);
            else if (TryStripPrefix(s, "corewasm", out rest) && TryParseVersion(rest, out version))
                runtime = CoreWasmRuntime.From(version);
            else if (TryStripPrefix(s, "monoaot", out rest) && rest.Length == 0)
                // Legacy Mono AOT is versionless (like classic Mono); the new Mono AOT ("monoaotX.Y") has no public toolchain.
                runtime = MonoAotRuntime.Default;
            else if (TryStripPrefix(s, "mono", out rest))
                // Bare "mono" is the classic Mono VM; "monoX.Y" is .NET on the Mono VM. Anything else (e.g. "monovm") isn't Mono.
                runtime = rest.Length == 0 ? MonoRuntime.Default
                    : TryParseVersion(rest, out version) ? MonoCoreRuntime.From(version)
                    : null;
            else if (TryStripPrefix(s, "r2r", out rest) && TryParseVersion(rest, out version))
                runtime = R2RRuntime.From(version);
            else if (TryStripPrefix(s, "netcoreapp", out rest) && TryParseVersion(rest, out version))
                runtime = CoreRuntime.From(version);
            else if (TryStripPrefix(s, "net", out rest) && TryParseVersion(rest, out version))
                runtime = version.Major == 4
                    ? ClrRuntime.FromVersion(version)
                    : CoreRuntime.From(version, platform);

            return runtime != null;

            static bool TryStripPrefix(string value, string prefix, out string rest)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    rest = value[prefix.Length..];
                    return true;
                }
                rest = string.Empty;
                return false;
            }

            static bool TryParseVersion(string value, [NotNullWhen(true)] out Version? version)
            {
                version = null!;
                if (value.IsBlank())
                    return false;

                value = value.Replace('_', '.');
                if (value.Contains('.'))
                    return Version.TryParse(value, out version);

                foreach (char c in value)
                    if (!char.IsDigit(c))
                        return false;

                // Compact spellings: "80" -> 8.0, "472" -> 4.7.2, "20" -> 2.0.
                version = value.Length switch
                {
                    1 => new Version(value[0] - '0', 0),
                    2 => new Version(value[0] - '0', value[1] - '0'),
                    3 => new Version(value[0] - '0', value[1] - '0', value[2] - '0'),
                    _ => null,
                };
                // Compact spellings only express single-digit majors; .NET 10+ has no unambiguous compact form
                // ("net10" would parse as 1.0), and no BDN-supported runtime has a major below 2, so reject those
                // and require a separator instead (e.g. "net10.0" or "Net10_0").
                if (version != null && version.Major < 2)
                    version = null;
                return version != null;
            }
        }
    }
}
