using System;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigAttribute : Attribute
    {
        public IConfig Config { get; }

        public ConfigAttribute(Type type)
        {
            Config = (IConfig)Activator.CreateInstance(type);
        }

        public ConfigAttribute(string command)
        {
            Config = new ConfigParser().Parse(command.Split(' '));
        }
    }
}