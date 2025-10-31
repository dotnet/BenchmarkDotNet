using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Jobs
{
    public abstract class Argument : IEquatable<Argument>
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
        // Specisal chars that need to be wrapped with `\"`.
        // 1. Comma char (It's used for separater char for `-property:{name}={value}` and `-restoreProperty:{name}={ value}`)
        // 2. MSBuild special chars (https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters?view=vs-2022)
        private static readonly char[] MSBuildSpecialChars = [',', '%', '$', '@', '\'', '(', ')', ';', '?', '*'];

        private readonly bool escapeArgument;

        public MsBuildArgument(string value, bool escape = false) : base(value)
        {
            escapeArgument = escape;
        }

        /// <summary>
        /// Gets the MSBuild argument that is used for build script.
        /// </summary>
        internal string GetEscapedTextRepresentation()
        {
            var originalArgument = TextRepresentation;

            if (!escapeArgument)
                return originalArgument;

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

            // If value starts with `\"`.
            // It is expected that the escaped value is specified by the user.
            if (value.StartsWith("\\\""))
                return originalArgument;

            // If value is wrapped with double quote. Trim leading/trailing double quote.
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Trim(['"']);

            // Escape chars that need to escaped when wrapped with escaped double quote (`\"`)
            value = value.Replace(" ", "%20")   // Space
                         .Replace("\"", "%22")  // Double Quote
                         .Replace("\\", "%5C"); // BackSlash

            // If escaped value doesn't contains MSBuild special char, return original argument.
            if (value.IndexOfAny(MSBuildSpecialChars) < 0)
                return originalArgument;

            // Return escaped value that is wrapped with escaped double quote (`\"`)
            return $"""
                    {key}=\"{value}\"
                    """;
        }
    }
}
