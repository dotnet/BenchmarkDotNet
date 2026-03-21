using System;
using System.Text;
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
        private readonly string? displayValue;

        public MsBuildArgument(string value) : base(value) { }

        public MsBuildArgument(string value, bool escapeSpecialCharacters) : base(escapeSpecialCharacters ? EscapeSpecialCharacters(value) : value)
        {
            if (escapeSpecialCharacters)
                displayValue = value;
        }

        public override string ToString() => displayValue ?? base.ToString();

        internal static string EscapeSpecialCharacters(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                switch (character)
                {
                    // See: https://learn.microsoft.com/en-us/visualstudio/msbuild/special-characters-to-escape
                    case '%':
                        builder.Append("%25");
                        break;
                    case '$':
                        builder.Append("%24");
                        break;
                    case '@':
                        builder.Append("%40");
                        break;
                    case '(':
                        builder.Append("%28");
                        break;
                    case ')':
                        builder.Append("%29");
                        break;
                    case ';':
                        builder.Append("%3B");
                        break;
                    case '?':
                        builder.Append("%3F");
                        break;
                    case '*':
                        builder.Append("%2A");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// An MSBuild property argument (<c>/p:name=value</c>).
    /// It escapes MSBuild special characters in the value (for example, semicolons) so that
    /// list-like values can be passed without manual encoding.
    /// </summary>
    [PublicAPI]
    public class MsBuildProperty : MsBuildArgument
    {
        public MsBuildProperty(string name, params string[] values)
            : base(CreateArgumentTextRepresentation(name, values), escapeSpecialCharacters: true)
        {
        }

        private static string CreateArgumentTextRepresentation(string name, string[] values)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Property name must be non-empty.", nameof(name));

            if (values is null)
                throw new ArgumentNullException(nameof(values));

            for (int i = 0; i < values.Length; i++)
                if (values[i] is null)
                    throw new ArgumentException("Property values can not contain null.", nameof(values));

            string value = values.Length switch
            {
                0 => string.Empty,
                1 => values[0],
                _ => string.Join(";", values)
            };

            return $"/p:{name}={value}";
        }
    }
}
