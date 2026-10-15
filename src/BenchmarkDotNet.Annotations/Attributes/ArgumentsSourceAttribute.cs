using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ArgumentsSourceAttribute : PriorityAttribute
    {
        public string Name { get; }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? Type { get; }

        public ArgumentsSourceAttribute(string name)
        {
            Name = name;
            Type = null;
        }

        public ArgumentsSourceAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string name)
        {
            Name = name;
            Type = type;
        }
    }
}