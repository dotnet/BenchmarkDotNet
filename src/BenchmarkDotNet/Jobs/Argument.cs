using System;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public abstract class Argument: IEquatable<Argument>
    {
        [PublicAPI] public string TextRepresentation { get; }

        protected Argument(string value)
        {
            TextRepresentation = value;
        }

        // CharacteristicPresenters call ToString(), this is why we need this override
        public override string ToString() => TextRepresentation;

        public bool Equals(Argument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TextRepresentation == other.TextRepresentation;
        }

        public override bool Equals(object obj) => Equals(obj as Argument);

        public override int GetHashCode() => HashCode.Combine(TextRepresentation);
    }

    /// <summary>
    /// Argument passed directly to mono when executing benchmarks (mono [options])
    /// example: new MonoArgument("--gc=sgen")
    /// </summary>
    public class MonoArgument : Argument
    {
        public MonoArgument(string value) : base(value)
        {
            if (value == "--llvm" || value == "--nollvm")
                throw new NotSupportedException("Please use job.Env.Jit to specify Jit in explicit way");
        }
    }

    /// <summary>
    /// Argument passed to dotnet cli when restoring and building the project
    /// example: new MsBuildArgument("/p:MyCustomSetting=123")
    /// </summary>
    [PublicAPI]
    public class MsBuildArgument : Argument
    {
        // Characters that need to be escaped.
        // 1. Space
        // 2. Comma (Special char that is used for separater for value of `-property:{name}={value}` and `-restoreProperty:{name}={value}`)
        // 3. Other MSBuild special chars (https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters?view=vs-2022)
        private static readonly char[] MSBuildCharsToEscape = [' ', ',', '%', '$', '@', '\'', '(', ')', ';', '?', '*'];

        public MsBuildArgument(string value) : base(value) { }

        /// <summary>
        /// Gets the MSBuild argument that is used for build script.
        /// </summary>
        internal string GetEscapedTextRepresentation()
        {
            var originalArgument = TextRepresentation;

            // If entire argument surrounded with double quote, returns original argument.
            // In this case. MSBuild special chars must be escaped by user. https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters
            if (originalArgument.StartsWith("\""))
                return originalArgument;

            // Process MSBuildArgument that contains '=' char. (e.g. `--property:{key}={value}` and `-restoreProperty:{key}={value}`)
            // See: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2022
            var values = originalArgument.Split(['='], 2);
            if (values.Length != 2)
                return originalArgument;

            var key = values[0];
            var value = values[1];

            // If value starts with `\` char. It is expected that the escaped value is specified by the user.
            if (value.StartsWith("\\"))
                return originalArgument;

            // If value don't contains special chars. return original value.
            if (value.IndexOfAny(MSBuildCharsToEscape) < 0)
                return originalArgument;

            return $"{key}={GetEscapedValue(value)}";
        }

        private static string GetEscapedValue(string value)
        {
            // If value starts with double quote. Trim leading/trailing double quote
            if (value.StartsWith("\""))
                value = value.Trim(['"']);

            bool isWindows = true;
#if NET
            isWindows = OperatingSystem.IsWindows();
#endif
            if (isWindows)
            {
                // On Windows environment.
                // Returns double-quoted value. (Command line execution and `.bat` file requires escape double quote with `\`)
                return $"""
                        \"{value}\"
                        """;
            }

            // On non-Windows environment.
            // Returns value that surround with `'"` and `"'`. See: https://github.com/dotnet/sdk/issues/8792#issuecomment-393756980
            // It requires escape with `\` when running command with `.sh` file. )
            return $"""
                    \'\"{value}\"\'
                    """;
        }
    }
}
