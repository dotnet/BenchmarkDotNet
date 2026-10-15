using BenchmarkDotNet.Configs;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class ConfigAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public ConfigAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type) => Config = (IConfig)Activator.CreateInstance(type)!;
    }
}