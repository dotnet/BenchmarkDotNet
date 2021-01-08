using JetBrains.Annotations;

namespace BenchmarkDotNet.Extensions
{
    public static class ParameterExtensions
    {
        [PublicAPI]
        public static NamedParameter WithName(this object parameter, [NotNull] string name) => new NamedParameter(parameter, name);

        [PublicAPI]
        public static NamedParameter WithDefaultName(this object parameter) => new NamedParameter(parameter, null);
    }

    public class NamedParameter
    {
        public object Value { get; }

        public string Name { get; }

        internal NamedParameter(object value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}