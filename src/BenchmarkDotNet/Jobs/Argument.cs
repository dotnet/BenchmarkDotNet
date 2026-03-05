using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

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

        public bool Equals(Argument? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TextRepresentation == other.TextRepresentation;
        }

        public override bool Equals(object? obj) => Equals(obj as Argument);

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
        private static readonly Dictionary<char, string> MsBuildEscapes = new ()
        {
            { '%', "%25" },
            { '$', "%24" },
            { '@', "%40" },
            { '\'', "%27" },
            { '(', "%28" },
            { ')', "%29" },
            { ';', "%3B" },
            { '?', "%3F" },
            { '*', "%2A" }
        };

        private static string EscapeMsBuildSpecialChars(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (MsBuildEscapes.TryGetValue(c, out var escaped))
                    sb.Append(escaped);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }
        /// <summary>
        /// Represents an MSBuild command-line argument.
        /// <param name="value">The raw or escaped argument value (e.g., "/p:DefineConstants=TEST1;TEST2").</param>
        /// <param name="escapeSpecialChars">
        /// If true (default), special MSBuild characters like %, ;, *, etc. will be escaped.
        /// If false, the value is used as-is — use this only if you're passing a fully pre-escaped string.
        /// </param>
        /// </summary>
        public MsBuildArgument(string value, bool escapeSpecialChars = true) : base(escapeSpecialChars ? EscapeMsBuildSpecialChars(value) : value) { }
    }
}