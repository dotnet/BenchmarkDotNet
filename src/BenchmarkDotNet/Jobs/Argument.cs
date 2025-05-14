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

        // This lets us check if a %xx is a known escape
        private static readonly HashSet<string> KnownEscapeValues = [.. MsBuildEscapes.Values];

        private static string EscapeMsBuildSpecialChars(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var sb = new StringBuilder(value.Length);
            int i = 0;

            while (i < value.Length)
            {
                char c = value[i];

                // Check for a known %xx escape
                if (c == '%' && i + 2 < value.Length)
                {
                    string candidate = value.Substring(i, 3); // e.g., "%3B"

                    if (KnownEscapeValues.Contains(candidate))
                    {
                        sb.Append(candidate);
                        i += 3;
                        continue;
                    }
                }

                // Escape only if it's a special MSBuild character
                if (MsBuildEscapes.TryGetValue(c, out var escaped))
                {
                    sb.Append(escaped);
                }
                else
                {
                    sb.Append(c);
                }

                i++;
            }

            return sb.ToString();
        }

        public MsBuildArgument(string value) : base(EscapeMsBuildSpecialChars(value)) { }
    }
}