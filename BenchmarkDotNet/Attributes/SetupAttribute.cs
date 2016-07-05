using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SetupAttribute : Attribute
    {
        public SetupAttribute()
        {
        }
    }
}
