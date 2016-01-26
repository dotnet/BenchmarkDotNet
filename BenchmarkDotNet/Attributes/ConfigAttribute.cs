using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigAttribute : Attribute
    {
        public Type Type { get; }
        public ConfigUnionRule UnionRule { get; }

        public ConfigAttribute(Type type, ConfigUnionRule unionRule = ConfigUnionRule.Union)
        {
            Type = type;
            UnionRule = unionRule;
        }

        public ConfigAttribute(string command)
        {
        }
    }
}