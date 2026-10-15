using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsSourceAttribute : PriorityAttribute
    {
        public string Name { get; }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? Type { get; }

        public ParamsSourceAttribute(string name)
        {
            Name = name;
            Type = null;
        }

        public ParamsSourceAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string name)
        {
            Name = name;
            Type = type;
        }
    }
}